using System;
using System.Collections.Generic;
using System.Text;

namespace BcaEttvCore
{
    public class RetvModelResult : EttvModel
    {
        public IList<RetvOrientationResult> ResultByOrientation { get; set; } = new List<RetvOrientationResult>();
        public string ResultSummary { get; set; } = string.Empty;

        public double WallArea { get; set; }
        public double WindowArea { get; set; }
        public double TotalEnvelopeGain { get; set; }
        public double AverageRetv { get; set; }
        public double OverallAverageRetv { get; set; }

        public double GrossArea => WallArea + WindowArea;
        public double Wwr => GrossArea > 0 ? WindowArea / GrossArea : 0d;

        public void BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Average RETV contribution: {AverageRetv:0.###} W/m²");
            sb.AppendLine($"WWR: {Wwr:P2}");
            sb.AppendLine($"Window area: {WindowArea:0.###} m²");
            sb.AppendLine($"Wall area: {WallArea:0.###} m²");
            sb.AppendLine($"Gross area: {GrossArea:0.###} m²");
            sb.AppendLine($"Total envelope gain: {TotalEnvelopeGain:0.###} W");

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
    }
}
