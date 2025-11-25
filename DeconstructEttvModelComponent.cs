using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using BcaEttvCore;

namespace BcaEttv
{
    public class DeconstructEttvModelComponent : GH_Component
    {
        public DeconstructEttvModelComponent()
            : base("Deconstruct EttvModel", "DEM",
                   "Deconstruct an EttvModel into summary text, metadata, and surfaces",
                   "BcaEttv", "Utilities")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvModel", "M", "EttvModel to deconstruct", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Summary text", GH_ParamAccess.item);
            pManager.AddTextParameter("ProjectName", "PN", "Project name", GH_ParamAccess.item);
            pManager.AddTextParameter("Version", "V", "Version", GH_ParamAccess.item);
            pManager.AddGenericParameter("EttvSurfaces", "S", "EttvSurface list", GH_ParamAccess.list);
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

            EttvModelDeconstructor.Deconstruct(model, out var summary, out List<EttvSurface> surfaces);

            DA.SetData(0, summary);
            DA.SetData(1, model.ProjectName);
            DA.SetData(2, model.Version);
            DA.SetDataList(3, surfaces);
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

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.DeconstructEttvModel.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("60B28D31-8D0D-44B7-A508-FD28F75BCD5A");
    }
}
