using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace FacadeFluxCore
{
    public class EttvModelResult : FluxModel
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

            AppendConstructionSummaryTable(sb);
            AppendConstructionBreakdownTables(sb);

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

                    var breakdownTable = new StringBuilder();
                    var hasBreakdownRows = false;

                    void FlushBreakdownTable()
                    {
                        if (!hasBreakdownRows)
                            return;

                        sb.AppendLine("<tr><td colspan=\"2\">");
                        sb.AppendLine("<table>");
                        sb.AppendLine("<tbody>");
                        sb.Append(breakdownTable.ToString());
                        sb.AppendLine("</tbody>");
                        sb.AppendLine("</table>");
                        sb.AppendLine("</td></tr>");

                        breakdownTable.Clear();
                        hasBreakdownRows = false;
                    }

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            FlushBreakdownTable();
                            AppendRow(sb, parts[0].Trim(), parts[1].Trim());
                        }
                        else if (IsBreakdownHeader(line))
                        {
                            AppendBreakdownHeaderRow(breakdownTable, line);
                            hasBreakdownRows = true;
                        }
                        else if (line.Contains(","))
                        {
                            AppendCommaSeparatedRow(breakdownTable, line);
                            hasBreakdownRows = true;
                        }
                        else
                        {
                            FlushBreakdownTable();
                            AppendFullRow(sb, line);
                        }
                    }

                    FlushBreakdownTable();
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

        private static void AppendCommaSeparatedRow(StringBuilder sb, string content)
        {
            var cells = content.Split(',');
            if (cells.Length == 0)
            {
                AppendFullRow(sb, content);
                return;
            }

            var useHeaderCells = cells[0].Trim().Equals("ID", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine("<tr>");
            foreach (var cell in cells)
            {
                var value = HtmlEncode(cell.Trim());
                if (useHeaderCells)
                    sb.AppendLine($"<th>{value}</th>");
                else
                    sb.AppendLine($"<td>{value}</td>");
            }
            sb.AppendLine("</tr>");
        }

        private static bool IsBreakdownHeader(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            return trimmed.Equals("Opaque Construction", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Fenestration Construction", StringComparison.OrdinalIgnoreCase);
        }

        private static void AppendBreakdownHeaderRow(StringBuilder sb, string title)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<th colspan=\"10\" style=\"text-align:left;\">{HtmlEncode(title)}</th>");
            sb.AppendLine("</tr>");
        }

        private void AppendConstructionSummaryTable(StringBuilder sb)
        {
            var constructions = GetUniqueConstructions().ToList();
            if (constructions.Count == 0)
                return;

            sb.AppendLine("<br/>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr><th colspan=\"4\">Envelope Construction Summary</th></tr>");
            sb.AppendLine("<tr><th>ID</th><th>Description</th><th>U-Value (W/m²K)</th><th>SC value</th></tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            foreach (var construction in constructions)
            {
                var id = HtmlEncode(construction.Id ?? string.Empty);
                var description = HtmlEncode(string.IsNullOrWhiteSpace(construction.Name) ? "Unnamed" : construction.Name);
                var uValue = HtmlEncode($"{construction.Uvalue:0.###} W/m²K");
                var scRaw = construction is FluxFenestrationConstruction
                    ? GetScValue(construction)
                    : double.NaN;
                var scValue = double.IsNaN(scRaw)
                    ? "N/A"
                    : HtmlEncode($"{scRaw:0.###}");

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{id}</td>");
                sb.AppendLine($"<td>{description}</td>");
                sb.AppendLine($"<td>{uValue}</td>");
                sb.AppendLine($"<td>{scValue}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }

        private void AppendConstructionBreakdownTables(StringBuilder sb)
        {
            var constructions = GetUniqueConstructions().ToList();
            if (constructions.Count == 0)
                return;

            foreach (var construction in constructions)
            {
                sb.AppendLine("<br/>");
                sb.AppendLine("<table>");
                sb.AppendLine("<thead>");

                var title = HtmlEncode(string.IsNullOrWhiteSpace(construction.Name) ? "Unnamed Construction" : construction.Name);
                sb.AppendLine($"<tr><th colspan=\"4\">{title}</th></tr>");
                sb.AppendLine("<tr><th>Material Description</th><th>Thickness (m)</th><th>Thermal Conductivity (W/mK)</th><th>Thermal Resistance (m²K/W)</th></tr>");
                sb.AppendLine("</thead>");
                sb.AppendLine("<tbody>");

                const double outsideFilmR = 0.044;
                const double insideFilmR = 0.12;
                double totalResistance = outsideFilmR + insideFilmR;

                if (construction.FluxMaterials != null && construction.FluxMaterials.Count > 0)
                {
                    foreach (var material in construction.FluxMaterials)
                    {
                        var name = HtmlEncode(material?.Name ?? string.Empty);
                        var thickness = HtmlEncode(material?.Thickness > 0 ? $"{material.Thickness:0.###}" : "N/A");
                        var conductivity = HtmlEncode(material?.ThermalConductivity > 0 ? $"{material.ThermalConductivity:0.###}" : "N/A");
                        double rLayer = material?.Thickness > 0 && material?.ThermalConductivity > 0
                            ? material.Thickness / material.ThermalConductivity
                            : double.NaN;
                        var resistance = double.IsNaN(rLayer) ? "N/A" : HtmlEncode($"{rLayer:0.###}");
                        if (!double.IsNaN(rLayer))
                            totalResistance += rLayer;

                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td>{name}</td>");
                        sb.AppendLine($"<td>{thickness}</td>");
                        sb.AppendLine($"<td>{conductivity}</td>");
                        sb.AppendLine($"<td>{resistance}</td>");
                        sb.AppendLine("</tr>");
                    }
                }
                else
                {
                    sb.AppendLine("<tr><td colspan=\"4\">No materials</td></tr>");
                }

                var outsideText = HtmlEncode($"{outsideFilmR:0.###}");
                var insideText = HtmlEncode($"{insideFilmR:0.###}");
                sb.AppendLine($"<tr><td>Outside air film</td><td>-</td><td>-</td><td>{outsideText}</td></tr>");
                sb.AppendLine($"<tr><td>Inside air film</td><td>-</td><td>-</td><td>{insideText}</td></tr>");

                var rSumText = totalResistance > 0 ? HtmlEncode($"{totalResistance:0.###}") : "N/A";
                var uValueText = totalResistance > 0 ? HtmlEncode($"{1.0 / totalResistance:0.###}") : "N/A";

                sb.AppendLine($"<tr><td colspan=\"3\" style=\"text-align:right;\">R Total</td><td>{rSumText}</td></tr>");
                sb.AppendLine($"<tr><td colspan=\"3\" style=\"text-align:right;\">U-Value (1/R Total)</td><td>{uValueText}</td></tr>");

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }
        }

        private IEnumerable<FluxConstruction> GetUniqueConstructions()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<FluxConstruction>();

            foreach (var construction in (Surfaces ?? new List<FluxSurface>()).Select(s => s?.Construction))
            {
                if (construction == null)
                    continue;

                var key = BuildConstructionKey(construction);
                if (seen.Add(key))
                    ordered.Add(construction);
            }

            return ordered
                .OrderBy(c => c is FluxOpaqueConstruction ? 0 : 1)
                .ThenBy(c => c.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        private static string BuildConstructionKey(FluxConstruction construction)
        {
            var idPart = (construction.Id ?? string.Empty).Trim();
            var namePart = (construction.Name ?? string.Empty).Trim();
            var key = $"{idPart}::{namePart}";

            if (string.IsNullOrWhiteSpace(idPart) && string.IsNullOrWhiteSpace(namePart))
                key = construction.GetHashCode().ToString();

            return key;
        }

        private static double GetScValue(FluxConstruction construction)
        {
            if (construction is FluxFenestrationConstruction fen)
            {
                if (fen.ScTotal > 0d)
                    return fen.ScTotal;

                double sc1 = fen.Sc1 > 0d ? fen.Sc1 : 1d;
                double sc2 = fen.Sc2 > 0d ? fen.Sc2 : 1d;
                double sc = sc1 * sc2;
                return sc > 0d ? sc : 1d;
            }

            return double.NaN;
        }

        private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
