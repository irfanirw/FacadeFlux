using System;
using System.Collections.Generic;
using System.Text;

namespace BcaEttvCore
{
    public class EttvOrientationResult
    {
        public EttvOrientation Orientation { get; set; }
        public double WallArea { get; set; }
        public double WindowArea { get; set; }
        public double TotalHeatGain { get; set; }
        public double AverageHeatGain { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<EttvSurface> Surfaces { get; set; } = new();
        public List<EttvConstruction> UniqueConstructions { get; set; } = new();

        public double GrossArea => WallArea + WindowArea;

        public double Wwr => GrossArea > 0 ? WindowArea / GrossArea : 0d;

        public void BuildSummary()
        {
            var orientationName = !string.IsNullOrWhiteSpace(Orientation?.Name)
                ? Orientation.Name
                : "Unknown";
            var orientationId = Orientation?.Id;

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(orientationId) &&
                !string.Equals(orientationName, orientationId, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"Orientation: {orientationName} ({orientationId})");
            }
            else
            {
                sb.AppendLine($"Orientation: {orientationName}");
            }

            sb.AppendLine($"Average ETTV: {AverageHeatGain:0.###} W/m²");
            sb.AppendLine($"WWR: {Wwr:P2}");
            sb.AppendLine($"Window area: {WindowArea:0.###} m²");
            sb.AppendLine($"Wall area: {WallArea:0.###} m²");
            sb.AppendLine($"Gross area: {GrossArea:0.###} m²");
            sb.AppendLine($"Total gross heat gain: {TotalHeatGain:0.###} W");
            sb.AppendLine($"Correction factor (Cf): {(Orientation?.Cf ?? 0d):0.###}");

            Summary = sb.ToString().TrimEnd();
        }
    }
}
