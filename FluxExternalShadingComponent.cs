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
                 "FacadeFlux", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurface", "S", "FluxSurface with FluxFenestrationConstruction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Sc2", "Sc2", "External shading coefficient (0.0 - 1.0)", GH_ParamAccess.item, 1.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurface", "S", "Modified FluxSurface with updated Sc2 and ScTotal", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawSurface = null;
            double sc2 = 1.0;

            if (!DA.GetData(0, ref rawSurface))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxSurface provided.");
                return;
            }

            if (!DA.GetData(1, ref sc2))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Sc2 value provided.");
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

            // Validate Sc2 range
            if (sc2 < 0.0 || sc2 > 1.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Sc2 value {sc2:F3} is outside typical range [0.0, 1.0]. Proceeding anyway.");
            }

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
                    Sc2 = sc2, // Apply new Sc2
                    ScTotal = fenConstruction.Sc1 * sc2, // Recompute ScTotal
                    FluxMaterials = fenConstruction.FluxMaterials
                }
            };

            DA.SetData(0, modifiedSurface);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("FacadeFlux.Icons.FluxExternalShading.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("F2E3D4C5-B6A7-8901-CDEF-234567890ABC");
    }
}
