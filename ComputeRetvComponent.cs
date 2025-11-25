using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using BcaEttvCore;

namespace BcaEttv
{
    public class ComputeRetvComponent : GH_Component
    {
        private const double DefaultRetvLimit = 25.0;

        public ComputeRetvComponent()
          : base("ComputeRetv", "CR",
                 "Compute RETV values for the model",
                 "BcaEttv", "Calculations")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvModel", "M", "EttvModel object", GH_ParamAccess.item);
            pManager.AddBooleanParameter("RunComputation", "R", "Run RETV computation", GH_ParamAccess.item, false);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "Human readable RETV report", GH_ParamAccess.item);
            pManager.AddNumberParameter("RetvValue", "V", "Overall average RETV value", GH_ParamAccess.item);
            pManager.AddGenericParameter("RetvResult", "R", "Computed RetvModelResult", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawModel = null;
            bool runComputation = false;

            DA.GetData(0, ref rawModel);
            DA.GetData(1, ref runComputation);

            var model = UnwrapModel(rawModel);
            if (model == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No EttvModel provided.");
                DA.SetData(0, "No EttvModel provided.");
                DA.SetData(1, double.NaN);
                DA.SetData(2, null);
                return;
            }

            if (!runComputation)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Enable RunComputation to evaluate RETV.");
                DA.SetData(0, "Computation skipped.");
                DA.SetData(1, double.NaN);
                DA.SetData(2, null);
                return;
            }

            var result = RetvModelCalculator.Calculate(model);
            var pass = result.GrossArea > 0 ? result.OverallAverageRetv <= DefaultRetvLimit : (bool?)null;
            model.RetvResult = new RetvComputationResult(result.OverallAverageRetv, pass, DefaultRetvLimit)
            {
                OrientationBreakdown = result.ResultByOrientation.ToDictionary(
                    r => r.Orientation?.Name ?? "Unknown",
                    r => r.AverageRetv),
                Notes = "Computed via RetvModelCalculator"
            };

            DA.SetData(0, result.ResultSummary);
            DA.SetData(1, result.OverallAverageRetv);
            DA.SetData(2, result);
        }

        private static EttvModel UnwrapModel(object raw)
        {
            if (raw is EttvModel model)
                return model;

            if (raw is GH_ObjectWrapper wrapper)
                return wrapper.Value as EttvModel;

            if (raw is IGH_Goo goo)
            {
                var scriptValue = goo.ScriptVariable();
                if (scriptValue is EttvModel scriptModel)
                    return scriptModel;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.ComputeEttv.png");
                return stream is null ? null : new System.Drawing.Bitmap(stream);
            }
        }

        public override Guid ComponentGuid => new Guid("0F9C2D45-85B2-4771-B376-EF5E26F8F6A2");
    }
}
