using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using BcaEttvCore;

namespace BcaEttv
{
    public class EttvExternalShadingComponent : GH_Component
    {
        public EttvExternalShadingComponent()
          : base("EttvExternalShading", "EES",
                 "Apply external shading coefficient (Sc2) to EttvSurface with fenestration",
                 "BcaEttv", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvSurface", "S", "EttvSurface with EttvFenestrationConstruction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Sc2", "Sc2", "External shading coefficient (0.0 - 1.0)", GH_ParamAccess.item, 1.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvSurface", "S", "Modified EttvSurface with updated Sc2 and ScTotal", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawSurface = null;
            double sc2 = 1.0;

            if (!DA.GetData(0, ref rawSurface))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No EttvSurface provided.");
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

            if (!(v is EttvSurface surface) || v.GetType().Assembly != typeof(EttvSurface).Assembly)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid EttvSurface object.");
                return;
            }

            // Check if construction is EttvFenestrationConstruction
            if (!(surface.Construction is EttvFenestrationConstruction fenConstruction))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "EttvSurface does not contain EttvFenestrationConstruction. External shading (Sc2) is only applicable to fenestration.");
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
            var modifiedSurface = new EttvSurface
            {
                Id = surface.Id,
                Name = surface.Name,
                Geometry = surface.Geometry,
                Orientation = surface.Orientation,
                Construction = new EttvFenestrationConstruction
                {
                    Id = fenConstruction.Id,
                    Name = fenConstruction.Name,
                    Uvalue = fenConstruction.Uvalue,
                    Sc1 = fenConstruction.Sc1,
                    Sc2 = sc2, // Apply new Sc2
                    ScTotal = fenConstruction.Sc1 * sc2, // Recompute ScTotal
                    EttvMaterials = fenConstruction.EttvMaterials
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
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.EttvExternalShading.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("F2E3D4C5-B6A7-8901-CDEF-234567890ABC");
    }
}
