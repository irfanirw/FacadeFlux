using System.Collections.Generic;
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
            sb.AppendLine($"Average ETTV: {OverallAverageEttv:0.###}");
            sb.AppendLine($"WWR: {Wwr:P2}");
            sb.AppendLine($"Window area: {WindowArea:0.###} m²");
            sb.AppendLine($"Wall area: {WallArea:0.###} m²");
            sb.AppendLine($"Gross area: {GrossArea:0.###} m²");
            sb.AppendLine($"Total gross heat gain: {TotalHeatGain:0.###} W");
            sb.AppendLine($"Average heat gain: {AverageHeatGain:0.###} W/m²");
            ResultSummary = sb.ToString().TrimEnd();
        }
    }
}
