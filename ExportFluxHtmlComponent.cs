using System;
using System.IO;
using System.Net;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class ExportFluxHtmlComponent : GH_Component
    {
        public ExportFluxHtmlComponent()
          : base("ExportFluxHtml", "EFH",
                 "Export ETTV/RETV results as an HTML table",
                 "FacadeFlux", "3 :: Post-processing")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "R", "EttvModelResult or RetvModelResult to export", GH_ParamAccess.item);
            pManager.AddTextParameter("FileName", "F", "Optional file name (defaults to <GHfile>_EttvReport.html or <GHfile>_RetvReport.html)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("RunExport", "E", "Trigger HTML export", GH_ParamAccess.item, false);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("HtmlPath", "P", "Path to generated HTML file", GH_ParamAccess.item);
            pManager.AddTextParameter("HtmlDocument", "H", "Full HTML document content", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawResult = null;
            string customFileName = null;
            bool runExport = false;

            DA.GetData(0, ref rawResult);
            DA.GetData(1, ref customFileName);
            DA.GetData(2, ref runExport);

            var result = UnwrapResult(rawResult);
            if (result == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No ETTV/RETV result provided.");
                DA.SetData(0, null);
                DA.SetData(1, null);
                return;
            }

            if (!runExport)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Enable RunExport to write the HTML file.");
                DA.SetData(0, null);
                DA.SetData(1, null);
                return;
            }

            var document = BuildHtmlDocument(result);
            if (string.IsNullOrWhiteSpace(document))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to build HTML document for the provided result.");
                DA.SetData(0, null);
                DA.SetData(1, null);
                return;
            }

            var path = WriteHtmlDocument(result, document, customFileName);

            DA.SetData(0, path);
            DA.SetData(1, document);
        }

        private static object UnwrapResult(object value)
        {
            if (value is EttvModelResult direct)
                return direct;

            if (value is RetvModelResult directRetv)
                return directRetv;

            if (value is GH_ObjectWrapper wrapper)
                return wrapper.Value;

            if (value is IGH_Goo goo)
            {
                var scriptValue = goo.ScriptVariable();
                if (scriptValue is EttvModelResult scriptEttv)
                    return scriptEttv;
                if (scriptValue is RetvModelResult scriptRetv)
                    return scriptRetv;
            }

            return null;
        }

        private string WriteHtmlDocument(object result, string document, string customFileName)
        {
            var doc = OnPingDocument();
            var ghFilePath = doc?.FilePath;

            if (string.IsNullOrWhiteSpace(ghFilePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grasshopper file is not saved. Save the file to enable HTML export.");
                return null;
            }

            var directory = Path.GetDirectoryName(ghFilePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to locate Grasshopper file directory for HTML export.");
                return null;
            }

            var baseName = Path.GetFileNameWithoutExtension(ghFilePath);
            var fileName = !string.IsNullOrWhiteSpace(customFileName)
                ? EnsureHtmlExtension(customFileName.Trim())
                : (string.IsNullOrWhiteSpace(baseName) ? DefaultFileName(result) : $"{baseName}_{DefaultFileName(result)}");

            var fullPath = Path.Combine(directory, fileName);

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

        private static string EnsureHtmlExtension(string fileName)
        {
            return fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : $"{fileName}.html";
        }

        private static string DefaultFileName(object result)
        {
            if (result is RetvModelResult)
                return "RetvReport.html";
            return "EttvReport.html";
        }

        private static string BuildHtmlDocument(object result)
        {
            string htmlContent;
            string titleSuffix;
            string reportTitle;

            if (result is EttvModelResult ettv)
            {
                htmlContent = ettv.BuildSummaryTableHtml();
                titleSuffix = "ETTV Report";
                reportTitle = "Envelope Thermal Transfer Value (ETTV) Report";
            }
            else if (result is RetvModelResult retv)
            {
                htmlContent = retv.BuildSummaryTableHtml();
                titleSuffix = "RETV Report";
                reportTitle = "Residential Envelope Thermal Transfer Value (RETV) Report";
            }
            else
            {
                return null;
            }

            var projectName = result switch
            {
                EttvModelResult e => e.ProjectName,
                RetvModelResult r => r.ProjectName,
                _ => string.Empty
            };

            var encodedProjectName = WebUtility.HtmlEncode(projectName ?? string.Empty);
            var version = "1.0";
            var author = "Irfan Irwanuddin";
            var dateText = DateTime.Now.ToString("dd MMM yyyy");

            var headerBlock = $@"<h1>{WebUtility.HtmlEncode(reportTitle)}</h1>
<p>Produced with FacadeFlux Software<br/>
Version: {WebUtility.HtmlEncode(version)}<br/>
Author: {WebUtility.HtmlEncode(author)}<br/>
Project Name: {encodedProjectName}<br/>
Date: {WebUtility.HtmlEncode(dateText)}</p>
<hr/>";

            const string footerBlock = @"<hr/>
<p>
All calculations in this report are based on the BCA ETTV standard.<br/>
For more information on the BCA ETTV guidelines, visit: <a href=""https://www1.bca.gov.sg/buildsg/sustainability/green-mark-incentive-schemes/ettv-requirements"">https://www1.bca.gov.sg/buildsg/sustainability/green-mark-incentive-schemes/ettv-requirements</a><br/>
Validation test case report can be found here: [Insert URL]
</p>";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <title>{projectName} - {titleSuffix}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 1.5rem; }}
        table {{ border-collapse: collapse; min-width: 320px; }}
        th, td {{ border: 1px solid #ccc; padding: 6px 10px; }}
        th {{ background: #f3f3f3; text-align: left; }}
        hr {{ border: 0; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
{headerBlock}
{htmlContent}
{footerBlock}
</body>
</html>";
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.ComputeEttv.png");

        public override Guid ComponentGuid => new Guid("D63F35B9-0F6A-4B19-9E78-8B4D1C8F7B4D");
    }
}
