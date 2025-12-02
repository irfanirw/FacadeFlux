using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class DeconstructFluxModelComponent : GH_Component
    {
        public DeconstructFluxModelComponent()
            : base("Deconstruct FluxModel", "DEM",
                   "Deconstruct an FluxModel into summary text, metadata, and surfaces",
                   "FacadeFlux", "4 :: Utilities")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxModel", "M", "FluxModel to deconstruct", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Summary text", GH_ParamAccess.item);
            pManager.AddTextParameter("ProjectName", "PN", "Project name", GH_ParamAccess.item);
            pManager.AddTextParameter("Version", "V", "Version", GH_ParamAccess.item);
            pManager.AddGenericParameter("FluxSurfaces", "S", "FluxSurface list", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawInput = null;
            if (!DA.GetData(0, ref rawInput) || rawInput is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxModel supplied.");
                return;
            }

            var model = UnwrapModel(rawInput);
            if (model is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid FluxModel.");
                return;
            }

            FluxModelDeconstructor.Deconstruct(model, out var summary, out List<FluxSurface> surfaces);

            DA.SetData(0, summary);
            DA.SetData(1, model.ProjectName);
            DA.SetData(2, model.Version);
            DA.SetDataList(3, surfaces);
        }

        private static FluxModel UnwrapModel(object value)
        {
            if (value is FluxModel model)
                return model;

            if (value is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is FluxModel wrapped)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is FluxModel scriptModel)
                    return scriptModel;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DeconstructFluxModel.png");

        public override Guid ComponentGuid => new Guid("60B28D31-8D0D-44B7-A508-FD28F75BCD5A");
    }
}
