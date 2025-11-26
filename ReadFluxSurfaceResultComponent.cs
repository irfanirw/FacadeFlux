using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class ReadFluxSurfaceResultComponent : GH_Component
    {
        public ReadFluxSurfaceResultComponent()
            : base("Read Ettv Surface Result", "RESR",
                   "Extract surfaces and heat gain values from an EttvModelResult",
                   "FacadeFlux", "Post-processing")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvModelResult", "R", "Computed EttvModelResult", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Surfaces", "S", "FluxSurface geometries", GH_ParamAccess.list);
            pManager.AddNumberParameter("HeatGain", "H", "Heat gain per surface (W)", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawResult = null;
            if (!DA.GetData(0, ref rawResult) || rawResult is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No EttvModelResult supplied.");
                return;
            }

            var result = UnwrapResult(rawResult);
            if (result is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid EttvModelResult.");
                return;
            }

            var meshes = new List<Mesh>();
            var heatGains = new List<double>();

            var surfaces = result.Surfaces ?? new List<FluxSurface>();
            foreach (var surface in surfaces)
            {
                if (surface == null)
                    continue;

                surface.ComputeHeatGain();
                meshes.Add(surface.Geometry);
                heatGains.Add(surface.HeatGain);
            }

            DA.SetDataList(0, meshes);
            DA.SetDataList(1, heatGains);
        }

        private static EttvModelResult UnwrapResult(object value)
        {
            if (value is EttvModelResult modelResult)
                return modelResult;

            if (value is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is EttvModelResult wrapped)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is EttvModelResult scriptResult)
                    return scriptResult;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid => new Guid("CF7C3F3E-784A-4A9B-946A-2182C105D8AC");
    }
}
