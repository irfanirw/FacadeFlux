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
            pManager.AddGenericParameter("FluxSurface", "S", "FluxSurface(s) with FluxFenestrationConstruction", GH_ParamAccess.list);
            pManager.AddNumberParameter("R1", "R1", "Shading projection ratio (projection/height) used to look up Sc2", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurface", "S", "Modified FluxSurface list with updated Sc2 and ScTotal", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var rawSurfaces = new List<object>();
            var r1Values = new List<double>();
            DA.GetDataList(0, rawSurfaces);
            DA.GetDataList(1, r1Values);

            if (rawSurfaces.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxSurface provided.");
                return;
            }

            var outputs = new List<FluxSurface>();
            bool anyNonFen = false;

            for (int i = 0; i < rawSurfaces.Count; i++)
            {
                var surface = UnwrapSurface(rawSurfaces[i]);
                if (surface == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Item {i + 1}: Input is not a valid FluxSurface object.");
                    continue;
                }

                if (surface.Construction is not FluxFenestrationConstruction fenConstruction)
                {
                    anyNonFen = true;
                    outputs.Add(surface);
                    continue;
                }

                double r1 = GetR1ForIndex(r1Values, i);
                var orientation = surface.Orientation ?? new FluxOrientation { Name = "North" };
                double computedSc2 = HorizontalSc2Calculator.Calculate(r1, 1.0, orientation);

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
                        Sc2 = computedSc2,
                        ScTotal = fenConstruction.Sc1 * computedSc2,
                        FluxMaterials = fenConstruction.FluxMaterials
                    }
                };

                outputs.Add(modifiedSurface);
            }

            if (anyNonFen)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some surfaces are not fenestration; their Sc2 was left unchanged.");
            }

            DA.SetDataList(0, outputs);
        }

        private static FluxSurface UnwrapSurface(object raw)
        {
            if (raw is FluxSurface surface && raw.GetType().Assembly == typeof(FluxSurface).Assembly)
                return surface;

            if (raw is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is FluxSurface wrapped && wrapper.Value.GetType().Assembly == typeof(FluxSurface).Assembly)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is FluxSurface scriptSurface && scriptSurface.GetType().Assembly == typeof(FluxSurface).Assembly)
                    return scriptSurface;
            }

            return null;
        }

        private static double GetR1ForIndex(IReadOnlyList<double> r1Values, int index)
        {
            if (r1Values == null || r1Values.Count == 0)
                return 1.0;

            if (index < r1Values.Count)
                return r1Values[index];

            // For older frameworks without Index (^) support, use the last element explicitly.
            return r1Values[r1Values.Count - 1];
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxExternalShading.png");

        public override Guid ComponentGuid => new Guid("F2E3D4C5-B6A7-8901-CDEF-234567890ABC");
    }
}
