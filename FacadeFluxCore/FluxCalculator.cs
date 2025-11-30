using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace FacadeFluxCore
{
    public class FluxCalculator
    {
        public FluxModel Model { get; set; }
        public double EttvLimit { get; set; } = 50.0; // Default ETTV limit (W/m²)
        public string Climate { get; set; } = "Tropical"; // Default climate
        private const double WallConductanceCoefficient = 12.0;
        private const double FenestrationConductanceCoefficient = 3.4;
        private const double SolarGainCoefficient = 211.0;

        public FluxCalculator() { }

        public FluxCalculator(FluxModel model)
        {
            Model = model;
        }

        /// <summary>
        /// Calculate ETTV for the model and populate ComputationResult.
        /// Returns the calculated ETTV value.
        /// </summary>
        public double? CalculateEttv()
        {
            if (Model == null || Model.Surfaces == null || Model.Surfaces.Count == 0)
            {
                Model.ComputationResult = new EttvComputationResult
                {
                    EttvValue = null,
                    Pass = false,
                    Limit = EttvLimit,
                    Climate = Climate,
                    Notes = "No surfaces provided for calculation."
                };
                return null;
            }

            double sumWeightedEttv = 0.0;
            double totalGrossArea = 0.0;

            var orientationBreakdown = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var componentBreakdown = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in Model.Surfaces.Where(s => s != null)
                                                .GroupBy(s => s.Orientation?.Name ?? "Unknown"))
            {
                var surfaces = group.ToList();
                double wallArea = surfaces.Where(IsOpaque).Sum(GetSurfaceArea);
                double windowArea = surfaces.Where(IsFenestration).Sum(GetSurfaceArea);
                double grossArea = wallArea + windowArea;
                if (grossArea <= double.Epsilon)
                    continue;

                double wwr = windowArea / grossArea;

                double wallAreaWeightedU = AreaWeightedAverage(
                    surfaces.Where(IsOpaque),
                    s => s.Construction?.Uvalue ?? 0.0);

                double windowAreaWeightedU = AreaWeightedAverage(
                    surfaces.Where(IsFenestration),
                    s => s.Construction?.Uvalue ?? 0.0);

                double windowAreaWeightedSc = AreaWeightedAverage(
                    surfaces.Where(IsFenestration),
                    s => GetScTotal(s.Construction as FluxFenestrationConstruction));

                string orientationName = group.Key;
                double cf = GetOrientationCf(surfaces);

                // ETTV = 12(1-WWR)Uw + 3.4(WWR)Uf + 211(WWR)SC*CF
                double ettv = (WallConductanceCoefficient * (1.0 - wwr) * wallAreaWeightedU)
                              + (FenestrationConductanceCoefficient * wwr * windowAreaWeightedU)
                              + (SolarGainCoefficient * wwr * windowAreaWeightedSc * cf);

                sumWeightedEttv += ettv * grossArea;
                totalGrossArea += grossArea;

                orientationBreakdown[orientationName] = ettv;

                componentBreakdown[$"{orientationName}-WallArea"] = wallArea;
                componentBreakdown[$"{orientationName}-WindowArea"] = windowArea;
            }

            if (totalGrossArea <= double.Epsilon)
            {
                Model.ComputationResult = new EttvComputationResult
                {
                    EttvValue = null,
                    Pass = false,
                    Limit = EttvLimit,
                    Climate = Climate,
                    Notes = "Surface areas are zero."
                };
                return null;
            }

            double ettvValue = sumWeightedEttv / totalGrossArea;
            bool pass = ettvValue <= EttvLimit;

            Model.ComputationResult = new EttvComputationResult(ettvValue, pass, EttvLimit)
            {
                Climate = Climate,
                OrientationBreakdown = orientationBreakdown,
                ComponentBreakdown = componentBreakdown,
                Notes = pass ? "ETTV calculation passed." : $"ETTV exceeds limit by {ettvValue - EttvLimit:F2} W/m²"
            };

            return ettvValue;
        }

        private static double AreaWeightedAverage(IEnumerable<FluxSurface> surfaces, Func<FluxSurface, double> selector)
        {
            double numerator = 0.0;
            double denominator = 0.0;

            foreach (var surface in surfaces)
            {
                double area = GetSurfaceArea(surface);
                if (area <= double.Epsilon)
                    continue;

                numerator += selector(surface) * area;
                denominator += area;
            }

            return denominator > double.Epsilon ? numerator / denominator : 0.0;
        }

        private static bool IsOpaque(FluxSurface surface) =>
            surface != null && !IsFenestration(surface);

        private static bool IsFenestration(FluxSurface surface) =>
            string.Equals(surface?.Type, "Fenestration", StringComparison.OrdinalIgnoreCase) ||
            surface?.Construction is FluxFenestrationConstruction;

        private static void AddContribution(Dictionary<string, double> map, string key, double value)
        {
            if (value == 0.0) return;

            key ??= "Unknown";
            if (!map.ContainsKey(key))
                map[key] = 0.0;
            map[key] += value;
        }

        private static double GetScTotal(FluxFenestrationConstruction fen)
        {
            if (fen == null) return 1.0;

            if (fen.ScTotal > 0.0)
                return fen.ScTotal;

            double sc1 = fen.Sc1 > 0.0 ? fen.Sc1 : 1.0;
            double sc2 = fen.Sc2 > 0.0 ? fen.Sc2 : 1.0;
            double sc = sc1 * sc2;
            return sc > 0.0 ? sc : 1.0;
        }

        private static double GetOrientationCf(List<FluxSurface> surfaces)
        {
            if (surfaces == null || surfaces.Count == 0)
                return 1.0;

            foreach (var s in surfaces)
            {
                var cf = s?.Orientation?.Cf ?? 0.0;
                if (cf > 0.0)
                    return cf;
            }

            // Derive CF from the orientation name if Cf was not precomputed.
            var firstName = surfaces[0]?.Orientation?.Name;
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                var orientation = new FluxOrientation { Name = firstName };
                orientation.SetCf();
                if (orientation.Cf > 0.0)
                    return orientation.Cf;
            }

            return 1.0;
        }

        private static double GetSurfaceArea(FluxSurface surface)
        {
            if (surface?.Geometry is not Mesh mesh) return 0.0;
            if (!mesh.IsValid || mesh.Faces.Count == 0) return 0.0;

            var amp = AreaMassProperties.Compute(mesh);
            if (amp == null || amp.Area <= double.Epsilon) return 0.0;

            return amp.Area;
        }
    }
}
