using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class DeconstructFluxConstructionComponent : GH_Component
    {
        public DeconstructFluxConstructionComponent()
          : base("Deconstruct FluxConstruction", "DEC",
                 "Deconstruct an FluxConstruction (Opaque or Fenestration) to a readable text",
                 "FacadeFlux", "4 :: Utilities")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxConstruction", "C", "FluxOpaqueConstruction or FluxFenestrationConstruction", GH_ParamAccess.item);
            pManager[0].Optional = true; // avoid yellow warning when nothing connected
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "Readable construction details", GH_ParamAccess.item);           // index 0
            pManager.AddNumberParameter("Uvalue", "U", "U-value (W/m²K)", GH_ParamAccess.item);                         // index 1
            pManager.AddNumberParameter("ScTotal", "SC", "Solar control total (ScTotal)", GH_ParamAccess.item);         // index 2
            pManager.AddNumberParameter("Sc1", "SC1", "Solar control factor 1", GH_ParamAccess.item);                   // index 3
            pManager.AddNumberParameter("Sc2", "SC2", "Solar control factor 2", GH_ParamAccess.item);                   // index 4
            pManager.AddGenericParameter("Materials", "M", "List of FluxMaterial", GH_ParamAccess.list);                // index 5
            pManager.AddTextParameter("ConstructionName", "CN", "FluxConstruction.Name", GH_ParamAccess.item);          // index 6
            pManager.AddTextParameter("ConstructionId", "CID", "FluxConstruction.Id", GH_ParamAccess.item);             // index 7
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object raw = null;
            DA.GetData(0, ref raw);

            if (raw is IGH_Goo goo)
                raw = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

            string result = string.Empty;
            var mats = new List<FluxMaterial>();
            double uValue = 0.0;
            double scTotal = 0.0;
            bool scIsEmpty = false;
            FluxConstruction construction = null;
            double sc1Value = 0.0;
            double sc2Value = 0.0;
            bool sc1IsEmpty = true;
            bool sc2IsEmpty = true;

            if (raw is FluxConstruction cons && raw.GetType().Assembly == typeof(FluxConstruction).Assembly)
            {
                construction = cons;
                result = FluxConstructionDeconstructor.ToText(cons);
                uValue = cons.Uvalue;
                if (cons.FluxMaterials != null)
                    mats.AddRange(cons.FluxMaterials);

                if (cons is FluxFenestrationConstruction fen)
                {
                    fen.CalculateScTotal(fen.Sc1, fen.Sc2);
                    scTotal = fen.Sc1 * (fen.Sc2 == 0 ? 1.0 : fen.Sc2);
                    sc1Value = fen.Sc1;
                    sc2Value = fen.Sc2;
                    sc1IsEmpty = false;
                    sc2IsEmpty = false;
                }
                else if (cons is FluxOpaqueConstruction)
                {
                    scIsEmpty = true;
                }
                else
                {
                    var pi = cons.GetType().GetProperty("ScTotal", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (pi != null)
                    {
                        var v = pi.GetValue(cons);
                        if (v != null)
                            scTotal = Convert.ToDouble(v);
                    }

                    var sc1Prop = cons.GetType().GetProperty("Sc1", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (sc1Prop != null)
                    {
                        var v = sc1Prop.GetValue(cons);
                        if (v != null)
                        {
                            sc1Value = Convert.ToDouble(v);
                            sc1IsEmpty = false;
                        }
                    }

                    var sc2Prop = cons.GetType().GetProperty("Sc2", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (sc2Prop != null)
                    {
                        var v = sc2Prop.GetValue(cons);
                        if (v != null)
                        {
                            sc2Value = Convert.ToDouble(v);
                            sc2IsEmpty = false;
                        }
                    }
                }

                var formula = BuildUValueFormula(mats, uValue);
                if (!string.IsNullOrWhiteSpace(formula))
                    result = string.IsNullOrWhiteSpace(result) ? formula : $"{result}{Environment.NewLine}{Environment.NewLine}{formula}";
            }
            else if (raw == null)
            {
                result = string.Empty;
            }
            else
            {
                result = "FluxConstruction: invalid input type";
            }

            DA.SetData(0, result);
            DA.SetData(1, uValue);
            if (scIsEmpty) DA.SetData(2, null);
            else DA.SetData(2, scTotal);
            if (sc1IsEmpty) DA.SetData(3, null);
            else DA.SetData(3, sc1Value);
            if (sc2IsEmpty) DA.SetData(4, null);
            else DA.SetData(4, sc2Value);
            DA.SetDataList(5, mats);
            DA.SetData(6, construction?.Name);
            DA.SetData(7, construction?.Id);
        }

        private static string BuildUValueFormula(IReadOnlyList<FluxMaterial> materials, double reportedU)
        {
            if (materials == null || materials.Count == 0)
                return string.Empty;

            const double Rsi = 0.12; // BCA internal surface resistance (m²·K/W)
            const double Rse = 0.04; // BCA external surface resistance (m²·K/W)

            var detailLines = new StringBuilder();
            var sigmaTerms = new List<string>();
            double layerSum = 0d;

            for (int i = 0; i < materials.Count; i++)
            {
                var mat = materials[i];
                if (mat == null)
                    continue;

                double thickness = mat.Thickness;
                double conductivity = mat.ThermalConductivity;
                double resistance = (thickness > 0 && conductivity > 0) ? thickness / conductivity : 0d;

                layerSum += resistance;
                sigmaTerms.Add($"R{i + 1}");
                detailLines.AppendLine($"  R{i + 1} ({mat.Name ?? $"Layer {i + 1}"}) = t/k = {thickness:0.###} / {conductivity:0.###} = {resistance:0.###} m²·K/W");
            }

            if (sigmaTerms.Count == 0)
                return string.Empty;

            double totalR = Rsi + layerSum + Rse;
            double calcU = totalR > 0 ? 1d / totalR : 0d;

            var sb = new StringBuilder();
            sb.AppendLine("U-value derivation (BCA Singapore):");
            sb.AppendLine($"  R_total = R_si + Σ(t_i/k_i) + R_se");
            sb.AppendLine($"          = {Rsi:0.###} + {layerSum:0.###} + {Rse:0.###} = {totalR:0.###} m²·K/W");
            sb.AppendLine($"  U = 1 / R_total = {calcU:0.###} W/m²K (reported: {reportedU:0.###} W/m²K)");
            sb.AppendLine("Layer breakdown:");
            sb.Append(detailLines.ToString().TrimEnd());

            return sb.ToString();
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DeconstructFluxConstruction.png");

        public override Guid ComponentGuid => new Guid("3c0d5b2a-8c7e-4a77-9d8c-2b1a6f4e5d7a");
    }
}
