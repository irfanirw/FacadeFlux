using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FacadeFluxCore
{
    public class RetvOrientationResult
    {
        public FluxOrientation Orientation { get; set; }
        public double WallArea { get; set; }
        public double WindowArea { get; set; }
        public double TotalEnvelopeGain { get; set; }
        public double AverageRetv { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<FluxSurface> Surfaces { get; set; } = new();
        public List<FluxConstruction> UniqueConstructions { get; set; } = new();

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

            sb.AppendLine($"Average RETV: {AverageRetv:0.###} W/m²");
            sb.AppendLine($"WWR: {Wwr:P2}");
            sb.AppendLine($"Window area: {WindowArea:0.###} m²");
            sb.AppendLine($"Wall area: {WallArea:0.###} m²");
            sb.AppendLine($"Gross area: {GrossArea:0.###} m²");
            sb.AppendLine($"Total envelope gain: {TotalEnvelopeGain:0.###} W");
            sb.AppendLine($"Correction factor (Cf): {(Orientation?.Cf ?? 0d):0.###}");

            AppendConstructionBreakdown(sb);

            Summary = sb.ToString().TrimEnd();
        }

        private void AppendConstructionBreakdown(StringBuilder sb)
        {
            if (Surfaces == null || Surfaces.Count == 0)
                return;

            var constructions = UniqueConstructions ?? new List<FluxConstruction>();
            if (constructions.Count == 0)
                constructions = Surfaces.Select(s => s?.Construction)
                                        .Where(c => c != null)
                                        .Distinct()
                                        .ToList();

            var sorted = constructions
                .Select(c => new
                {
                    Construction = c,
                    TypeOrder = c is FluxOpaqueConstruction ? 0 : 1,
                    NameKey = string.IsNullOrWhiteSpace(c?.Name) ? string.Empty : c.Name
                })
                .OrderBy(x => x.TypeOrder)
                .ThenBy(x => x.NameKey, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var addedOpaqueHeader = false;
            var addedFenestrationHeader = false;

            foreach (var entry in sorted)
            {
                var construction = entry.Construction;
                if (construction == null)
                    continue;

                double area = Surfaces.Where(s => s?.Construction?.Name == construction.Name)
                                      .Sum(s => s?.Area ?? 0d);

                if (area <= 0)
                    continue;

                var id = construction.Id ?? string.Empty;
                var name = construction.Name ?? "Unnamed";

                if (construction is FluxOpaqueConstruction)
                {
                    if (!addedOpaqueHeader)
                    {
                        sb.AppendLine("Opaque Construction");
                        sb.AppendLine("ID, Description, Area, U-Value (W/m²K), 3.4 x Area x U-Value");
                        addedOpaqueHeader = true;
                    }

                    var opaqueContribution = 3.4 * area * construction.Uvalue;
                    sb.AppendLine($"{id}, {name}, {area:0.###} m², {construction.Uvalue:0.###}, {opaqueContribution:0.###}");
                }
                else if (construction is FluxFenestrationConstruction fen)
                {
                    if (!addedFenestrationHeader)
                    {
                        sb.AppendLine("Fenestration Construction");
                        sb.AppendLine("ID, Description, Area, U-Value (W/m²K), SC, 1.3 x Area x U-Value, 58.6 x Area x CF x SC");
                        addedFenestrationHeader = true;
                    }

                    double scTotal = fen.ScTotal > 0 ? fen.ScTotal : (fen.Sc1 > 0 ? fen.Sc1 : 1d) * (fen.Sc2 > 0 ? fen.Sc2 : 1d);
                    double cf = Orientation?.Cf ?? 0d;
                    var conductionContribution = 1.3 * area * construction.Uvalue;
                    var solarContribution = 58.6 * area * cf * scTotal;
                    sb.AppendLine($"{id}, {name}, {area:0.###} m², {construction.Uvalue:0.###}, {scTotal:0.###}, {conductionContribution:0.###}, {solarContribution:0.###}");
                }
            }
        }
    }
}
