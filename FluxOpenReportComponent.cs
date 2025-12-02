using System;
using System.Diagnostics;
using System.IO;
using Grasshopper.Kernel;

namespace FacadeFlux
{
    public class FluxOpenReportComponent : GH_Component
    {
        public FluxOpenReportComponent()
          : base("FluxOpenReport", "FOR",
                 "Open an exported ETTV/RETV HTML report in the default browser",
                 "FacadeFlux", "4 :: Utilities")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("HtmlPath", "P", "Path to the exported HTML report", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ViewReport", "V", "Open the report when set to True", GH_ParamAccess.item, false);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string htmlPath = null;
            bool viewReport = false;

            DA.GetData(0, ref htmlPath);
            DA.GetData(1, ref viewReport);

            if (!viewReport)
            {
                DA.SetData(0, "Waiting: ViewReport is False.");
                return;
            }

            if (string.IsNullOrWhiteSpace(htmlPath))
            {
                const string message = "No HTML path provided.";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
                DA.SetData(0, message);
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(htmlPath);
            }
            catch (Exception ex)
            {
                var message = $"Invalid path: {ex.Message}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
                DA.SetData(0, message);
                return;
            }

            if (!File.Exists(fullPath))
            {
                var message = $"File not found: {fullPath}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
                DA.SetData(0, message);
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                var message = $"Opening: {fullPath}";
                DA.SetData(0, message);
            }
            catch (Exception ex)
            {
                var message = $"Failed to open report: {ex.Message}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
                DA.SetData(0, message);
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxOpenReport.png");

        public override Guid ComponentGuid => new Guid("7179E1EE-4B0E-4F1B-A3DA-F4C5BF46B79E");
    }
}
