using System;
using System.Collections.Generic;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using BcaEttvCore;

namespace BcaEttv
{
    public class EttvFenestrationConstructionComponent : GH_Component
    {
        public EttvFenestrationConstructionComponent()
          : base("EttvFenestrationConstruction", "EFC",
                 "Create an ETTV Fenestration Construction",
                 "BcaEttv", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Id", "ID", "Construction identifier (string)", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Construction name", GH_ParamAccess.item);
            pManager.AddGenericParameter("Materials", "M", "List of BcaEttvCore.EttvMaterial", GH_ParamAccess.list);
            pManager.AddNumberParameter("Sc", "SC", "Solar coefficient", GH_ParamAccess.item);

            // keep inputs optional to avoid yellow warnings
            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Output base class (EttvConstruction) but the instance is EttvFenestrationConstruction
            pManager.AddGenericParameter("EttvConstruction", "C", "EttvConstruction (fenestration) object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string id = null;
            string name = null;
            var rawMaterials = new List<object>();
            double sc = double.NaN;

            DA.GetData(0, ref id);
            DA.GetData(1, ref name);
            DA.GetDataList(2, rawMaterials);
            DA.GetData(3, ref sc);

            // Collect only BcaEttvCore.EttvMaterial
            var materials = new List<EttvMaterial>();
            var coreMatType = typeof(EttvMaterial);

            foreach (var item in rawMaterials)
            {
                object v = item;
                if (v is IGH_Goo goo)
                    v = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                if (v is EttvMaterial m && v.GetType().Assembly == coreMatType.Assembly)
                    materials.Add(m);
            }

            // If nothing provided, return quietly
            bool anyProvided = !string.IsNullOrWhiteSpace(id) || !string.IsNullOrEmpty(name) || materials.Count > 0 || !double.IsNaN(sc);
            if (!anyProvided)
            {
                DA.SetData(0, null);
                return;
            }

            // Build a fenestration construction and fill base properties
            var fen = new EttvFenestrationConstruction
            {
                Id = id ?? string.Empty,
                Name = name ?? string.Empty,
                EttvMaterials = materials,
                Sc2 = 1.0 // default unless explicitly set elsewhere
            };

            // Compute U-value from materials if any
            if (materials.Count > 0)
                fen.Uvalue = UvalueCalculator.ComputeUValue(materials);

            // Assign SC input to Sc1 property
            if (!double.IsNaN(sc))
                fen.Sc1 = sc;

            // Ensure ScTotal is set (Sc1 * Sc2, with Sc2 defaulting to 1.0)
            fen.ScTotal = fen.Sc1 * (fen.Sc2 == 0 ? 1.0 : fen.Sc2);

            // Output as base class (EttvConstruction)
            DA.SetData(0, (EttvConstruction)fen);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.EttvFenestrationConstruction.png");
                return stream is null ? null : new System.Drawing.Bitmap(stream);
            }
        }

        public override Guid ComponentGuid => new Guid("f7e8d9c0-b1a2-4c3d-9e4f-5a6b7c8d9e0f");
    }
}