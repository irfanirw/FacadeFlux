using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class ComputeFluxModelComponent : GH_Component
    {
        private const double DefaultRetvLimit = 25.0;

        public ComputeFluxModelComponent()
          : base("ComputeFluxModel", "CFM",
                 "Compute ETTV or RETV values for the model",
                 "FacadeFlux", "2 :: Analysis")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxModel", "M", "FluxModel object", GH_ParamAccess.item);
            pManager.AddBooleanParameter("RunComputation", "R", "Run computation", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("ComputationType", "C", "Computation type: 0 = ETTV, 1 = RETV", GH_ParamAccess.item, 0);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "Human readable ETTV/RETV report", GH_ParamAccess.item);
            pManager.AddNumberParameter("ThermalTransferValue", "V", "Overall average ETTV/RETV value (W/m²)", GH_ParamAccess.item);
            pManager.AddGenericParameter("FluxModelResult", "R", "Computed EttvModelResult or RetvModelResult", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawModel = null;
            bool runComputation = false;
            int computationType = 0;

            DA.GetData(0, ref rawModel);
            DA.GetData(1, ref runComputation);
            DA.GetData(2, ref computationType);

            var model = UnwrapModel(rawModel);
            if (model == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxModel provided.");
                DA.SetData(0, "No FluxModel provided.");
                DA.SetData(1, double.NaN);
                DA.SetData(2, null);
                return;
            }

            if (!runComputation)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Enable RunComputation to evaluate ETTV/RETV.");
                DA.SetData(0, "Computation skipped.");
                DA.SetData(1, double.NaN);
                DA.SetData(2, null);
                return;
            }

            switch (computationType)
            {
                case 0:
                    ComputeEttv(model, DA);
                    break;
                case 1:
                    ComputeRetv(model, DA);
                    break;
                default:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid ComputationType. Use 0 for ETTV or 1 for RETV.");
                    DA.SetData(0, "Invalid ComputationType supplied.");
                    DA.SetData(1, double.NaN);
                    DA.SetData(2, null);
                    break;
            }
        }

        private void ComputeEttv(FluxModel model, IGH_DataAccess DA)
        {
            if (model.Surfaces != null)
            {
                foreach (var surface in model.Surfaces)
                    surface?.ComputeHeatGain();
            }

            var result = FluxModelCalculator.Calculate(model);
            DA.SetData(0, result.ResultSummary);
            DA.SetData(1, result.OverallAverageEttv);
            DA.SetData(2, result);
        }

        private void ComputeRetv(FluxModel model, IGH_DataAccess DA)
        {
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

        private static FluxModel UnwrapModel(object raw)
        {
            if (raw is FluxModel model)
                return model;

            if (raw is GH_ObjectWrapper wrapper)
                return wrapper.Value as FluxModel;

            if (raw is IGH_Goo goo)
            {
                var scriptValue = goo.ScriptVariable();
                if (scriptValue is FluxModel scriptModel)
                    return scriptModel;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.ComputeFluxModel.png");

        public override Guid ComponentGuid => new Guid("E139698A-094B-4B7E-B7C2-3C5D4B9BE8CB");
    }
}
