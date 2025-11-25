using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.IO;
using BcaEttvCore;

namespace BcaEttv
{
    public class ComputeEttvComponent : GH_Component
    {
        public ComputeEttvComponent()
          : base("ComputeEttv", "CE",
                 "Compute ETTV values for the model",
                 "BcaEttv", "Calculations")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvModel", "M", "EttvModel object", GH_ParamAccess.item);
            pManager.AddBooleanParameter("RunComputation", "R", "Run ETTV computation", GH_ParamAccess.item, false);

            // Make all inputs optional to avoid yellow warnings
            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "Human readable ETTV report", GH_ParamAccess.item);
            pManager.AddNumberParameter("EttvValue", "V", "Overall average ETTV value", GH_ParamAccess.item);
            pManager.AddGenericParameter("EttvResult", "R", "Computed EttvModelResult", GH_ParamAccess.item);
            pManager.AddTextParameter("HtmlReport", "H", "Path to generated HTML report", GH_ParamAccess.item);
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
                DA.SetData(3, null);
                return;
            }

            if (!runComputation)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Enable RunComputation to evaluate ETTV.");
                DA.SetData(0, "Computation skipped.");
                DA.SetData(1, double.NaN);
                DA.SetData(2, null);
                DA.SetData(3, null);
                return;
            }

            // Compute per-surface heat gain before aggregating model results
            if (model.Surfaces != null)
            {
                foreach (var surface in model.Surfaces)
                    surface?.ComputeHeatGain();
            }

            var result = EttvModelCalculator.Calculate(model);
            DA.SetData(0, result.ResultSummary);
            DA.SetData(1, result.OverallAverageEttv);
            DA.SetData(2, result);

            var htmlPath = WriteHtmlReport(result);
            DA.SetData(3, htmlPath);
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

        private string WriteHtmlReport(EttvModelResult result)
        {
            if (result == null)
                return null;

            var doc = OnPingDocument();
            var ghFilePath = doc?.FilePath;

            if (string.IsNullOrWhiteSpace(ghFilePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grasshopper file is not saved. Save the file to enable HTML report export.");
                return null;
            }

            var directory = Path.GetDirectoryName(ghFilePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to locate Grasshopper file directory for HTML export.");
                return null;
            }

            var baseName = Path.GetFileNameWithoutExtension(ghFilePath);
            var fileName = string.IsNullOrWhiteSpace(baseName) ? "EttvReport.html" : $"{baseName}_EttvReport.html";
            var fullPath = Path.Combine(directory, fileName);

            var htmlContent = result.BuildSummaryTableHtml();
            var document = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <title>{result.ProjectName} - ETTV Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 1.5rem; }}
        table {{ border-collapse: collapse; min-width: 320px; }}
        th, td {{ border: 1px solid #ccc; padding: 6px 10px; }}
        th {{ background: #f3f3f3; text-align: left; }}
        hr {{ border: 0; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
{htmlContent}
</body>
</html>";

            try
            {
                File.WriteAllText(fullPath, document);
                return fullPath;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to write HTML report: {ex.Message}");
                return null;
            }
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

        public override Guid ComponentGuid => new Guid("C5F8A2D1-3E4B-4A5C-9D6E-7F8A9B0C1D2E");
    }
}
