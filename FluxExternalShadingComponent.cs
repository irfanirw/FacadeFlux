using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class FluxExternalShadingComponent : GH_Component
    {
        public FluxExternalShadingComponent()
          : base("FluxExternalShading", "EES",
                 "Apply external shading coefficient (Sc2) to FluxSurface with fenestration",
                 "FacadeFlux", "1 :: Input & Geometry")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurface", "S", "FluxSurface with FluxFenestrationConstruction", GH_ParamAccess.item);
            pManager.AddNumberParameter("R1", "R1", "Shading projection ratio (projection/height) used to look up Sc2", GH_ParamAccess.item, 1.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurface", "S", "Modified FluxSurface with updated Sc2 and ScTotal", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawSurface = null;
            double r1Ratio = 1.0;

            if (!DA.GetData(0, ref rawSurface))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxSurface provided.");
                return;
            }

            if (!DA.GetData(1, ref r1Ratio))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No R1 (projection/height) value provided.");
                return;
            }

            // Unwrap GH_Goo
            object v = rawSurface;
            if (v is IGH_Goo goo)
                v = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

            if (!(v is FluxSurface surface) || v.GetType().Assembly != typeof(FluxSurface).Assembly)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid FluxSurface object.");
                return;
            }

            // Check if construction is FluxFenestrationConstruction
            if (!(surface.Construction is FluxFenestrationConstruction fenConstruction))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "FluxSurface does not contain FluxFenestrationConstruction. External shading (Sc2) is only applicable to fenestration.");
                DA.SetData(0, surface);
                return;
            }

            var orientation = surface.Orientation ?? new FluxOrientation { Name = "North" };
            double computedSc2 = HorizontalSc2Calculator.Calculate(r1Ratio, 1.0, orientation);

            // Create a modified copy to avoid mutating the original
            var modifiedSurface = new FluxSurface
            {
                Id = surface.Id,
                Name = surface.Name,
                Geometry = surface.Geometry,
                Orientation = surface.Orientation,
                Construction = new FluxFenestrationConstruction
                {
                    Id = fenConstruction.Id,
                    Name = fenConstruction.Name,
                    Uvalue = fenConstruction.Uvalue,
                    Sc1 = fenConstruction.Sc1,
                    Sc2 = computedSc2, // Apply Sc2 from lookup
                    ScTotal = fenConstruction.Sc1 * computedSc2, // Recompute ScTotal
                    FluxMaterials = fenConstruction.FluxMaterials
                }
            };

            DA.SetData(0, modifiedSurface);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxExternalShading.png");

        public override Guid ComponentGuid => new Guid("F2E3D4C5-B6A7-8901-CDEF-234567890ABC");
    }
}
