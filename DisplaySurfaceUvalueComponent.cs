using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BcaEttvCore;

namespace BcaEttv
{
    public class DisplaySurfaceUvalueComponent : GH_Component
    {
        public DisplaySurfaceUvalueComponent()
            : base("Display Surface U-value", "DSU",
                   "Extract surface meshes and construction U-values from an EttvModel",
                   "BcaEttv", "Post-processing")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvModel", "M", "EttvModel to inspect", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Surfaces", "S", "EttvSurface geometries", GH_ParamAccess.list);
            pManager.AddNumberParameter("Uvalues", "U", "Surface construction U-values (W/m²K)", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawInput = null;
            if (!DA.GetData(0, ref rawInput) || rawInput is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No EttvModel supplied.");
                return;
            }

            var model = UnwrapModel(rawInput);
            if (model is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid EttvModel.");
                return;
            }

            var meshes = new List<Mesh>();
            var uvalues = new List<double>();

            var surfaces = model.Surfaces ?? new List<EttvSurface>();
            foreach (var surface in surfaces)
            {
                if (surface == null || surface.Geometry == null)
                    continue;

                meshes.Add(surface.Geometry);
                uvalues.Add(surface.Construction?.Uvalue ?? 0d);
            }

            if (meshes.Count == 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Model contains no valid EttvSurface geometry.");

            DA.SetDataList(0, meshes);
            DA.SetDataList(1, uvalues);
        }

        private static EttvModel UnwrapModel(object value)
        {
            if (value is EttvModel model)
                return model;

            if (value is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is EttvModel wrapped)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is EttvModel scriptModel)
                    return scriptModel;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("651C0097-AF5D-4C93-B13C-07C53AF61838");
    }
}
