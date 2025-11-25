using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BcaEttvCore
{
    public class EttvModelResult : EttvModel
    {
        public IList<EttvOrientationResult> ResultByOrientation { get; set; } = new List<EttvOrientationResult>();
        public string ResultSummary { get; set; } = string.Empty;

        public double WallArea { get; set; }
        public double WindowArea { get; set; }
        public double TotalHeatGain { get; set; }
        public double AverageHeatGain { get; set; }
        public double OverallAverageEttv { get; set; }

        public double GrossArea => WallArea + WindowArea;
        public double Wwr => GrossArea > 0 ? WindowArea / GrossArea : 0d;

        public void BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Average heat gain: {AverageHeatGain:0.###} W/m²");
            sb.AppendLine($"WWR: {Wwr:P2}");
            sb.AppendLine($"Window area: {WindowArea:0.###} m²");
            sb.AppendLine($"Wall area: {WallArea:0.###} m²");
            sb.AppendLine($"Gross area: {GrossArea:0.###} m²");
            sb.AppendLine($"Total gross heat gain: {TotalHeatGain:0.###} W");

            if (ResultByOrientation.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Breakdown by orientation:");

                foreach (var orientationResult in ResultByOrientation)
                {
                    if (orientationResult == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(orientationResult.Summary))
                        orientationResult.BuildSummary();

                    if (string.IsNullOrWhiteSpace(orientationResult.Summary))
                        continue;

                    var lines = orientationResult.Summary.Split(new[] { '\r', '\n' },
                                                               StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                        sb.AppendLine($"- {line}");

                    sb.AppendLine();
                }
            }

            ResultSummary = sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Returns an HTML table containing the same data as ResultSummary/BuildSummary.
        /// </summary>
        public string BuildSummaryTableHtml()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<table>");
            AppendRow(sb, "Average heat gain", $"{AverageHeatGain:0.###} W/m²");
            AppendRow(sb, "WWR", $"{Wwr:P2}");
            AppendRow(sb, "Window area", $"{WindowArea:0.###} m²");
            AppendRow(sb, "Wall area", $"{WallArea:0.###} m²");
            AppendRow(sb, "Gross area", $"{GrossArea:0.###} m²");
            AppendRow(sb, "Total gross heat gain", $"{TotalHeatGain:0.###} W");
            sb.AppendLine("</table>");

            if (ResultByOrientation.Count > 0)
            {
                sb.AppendLine("<br/>");
                sb.AppendLine("<table>");
                sb.AppendLine("<thead><tr><th colspan=\"2\">Breakdown by orientation</th></tr></thead>");
                sb.AppendLine("<tbody>");

                foreach (var orientationResult in ResultByOrientation)
                {
                    if (orientationResult == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(orientationResult.Summary))
                        orientationResult.BuildSummary();

                    if (string.IsNullOrWhiteSpace(orientationResult.Summary))
                        continue;

                    var lines = orientationResult.Summary.Split(new[] { '\r', '\n' },
                                                                StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0)
                        continue;

                    sb.AppendLine($"<tr><th colspan=\"2\">{HtmlEncode(lines[0])}</th></tr>");

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                            AppendRow(sb, parts[0].Trim(), parts[1].Trim());
                        else
                            AppendFullRow(sb, line);
                    }

                    sb.AppendLine("<tr><td colspan=\"2\"><hr/></td></tr>");
                }

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }

            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, string label, string value)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<th style=\"text-align:left;\">{HtmlEncode(label)}</th>");
            sb.AppendLine($"<td>{HtmlEncode(value)}</td>");
            sb.AppendLine("</tr>");
        }

        private static void AppendFullRow(StringBuilder sb, string content)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td colspan=\"2\">{HtmlEncode(content)}</td>");
            sb.AppendLine("</tr>");
        }

        private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
