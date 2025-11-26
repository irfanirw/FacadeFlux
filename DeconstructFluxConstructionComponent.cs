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
                 "FacadeFlux", "Utilities")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxConstruction", "C", "FluxOpaqueConstruction or FluxFenestrationConstruction", GH_ParamAccess.item);
            pManager[0].Optional = true; // avoid yellow warning when nothing connected
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "Readable construction details", GH_ParamAccess.item);      // index 0
            pManager.AddNumberParameter("Uvalue", "U", "U-value (W/m²K)", GH_ParamAccess.item);                    // index 1
            pManager.AddNumberParameter("ScTotal", "SC", "Solar control total (ScTotal)", GH_ParamAccess.item);    // index 2
            pManager.AddGenericParameter("Materials", "M", "List of FluxMaterial", GH_ParamAccess.list);           // index 3
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

            if (raw is FluxConstruction cons && raw.GetType().Assembly == typeof(FluxConstruction).Assembly)
            {
                result = FluxConstructionDeconstructor.ToText(cons);
                uValue = cons.Uvalue;
                if (cons.FluxMaterials != null)
                    mats.AddRange(cons.FluxMaterials);

                if (cons is FluxFenestrationConstruction fen)
                {
                    fen.CalculateScTotal(fen.Sc1, fen.Sc2);
                    scTotal = fen.Sc1 * (fen.Sc2 == 0 ? 1.0 : fen.Sc2);
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
            DA.SetDataList(3, mats);
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
