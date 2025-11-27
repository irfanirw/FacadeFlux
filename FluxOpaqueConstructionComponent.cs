using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    // Rename to avoid clashing with core class FacadeFluxCore.FluxOpaqueConstruction
    public class FluxOpaqueConstructionComponent : GH_Component
    {
        public FluxOpaqueConstructionComponent()
          : base("FluxOpaqueConstruction", "EOC",
                 "Create an FluxConstruction (opaque) from Id, Name and Materials",
                 "FacadeFlux", "1 :: Input & Geometry")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Id", "ID", "Construction identifier (string)", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Construction name", GH_ParamAccess.item);
            pManager.AddGenericParameter("Materials", "M", "List of FacadeFluxCore.FluxMaterial", GH_ParamAccess.list);

            // keep inputs optional to avoid yellow warnings
            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Output base class (FluxConstruction). Instance will be FluxOpaqueConstruction.
            pManager.AddGenericParameter("FluxConstruction", "C", "FluxConstruction (opaque) object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string id = null;
            string name = null;
            var rawMaterials = new List<object>();

            DA.GetData(0, ref id);
            DA.GetData(1, ref name);
            DA.GetDataList(2, rawMaterials);

            // Collect only FacadeFluxCore.FluxMaterial
            var materials = new List<FluxMaterial>();
            var coreMatType = typeof(FluxMaterial);

            foreach (var item in rawMaterials)
            {
                object v = item;
                if (v is IGH_Goo goo)
                    v = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                if (v is FluxMaterial m && v.GetType().Assembly == coreMatType.Assembly)
                    materials.Add(m);
            }

            // If nothing provided, return quietly
            bool anyProvided = !string.IsNullOrWhiteSpace(id) || !string.IsNullOrEmpty(name) || materials.Count > 0;
            if (!anyProvided)
            {
                DA.SetData(0, null);
                return;
            }

            // Compute U-value if materials are provided
            double u = materials.Count > 0 ? FluxUvalueCalculator.ComputeUValue(materials) : 0.0;

            // Build opaque construction
            var opaque = new FluxOpaqueConstruction
            {
                Id = id ?? string.Empty,
                Name = name ?? string.Empty,
                FluxMaterials = materials,
                Uvalue = u
            };

            // Output as base class
            DA.SetData(0, (FluxConstruction)opaque);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxOpaqueConstruction.png");

        public override Guid ComponentGuid => new Guid("8b6b8a2f-3f8f-4f7a-bf2c-9c5c2e7a4c11");
    }
}
