using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace BcaEttvCore
{
    /// <summary>
    /// Computes RETV (Residential Envelope Transmittance Value) using the BCA ETTV/RETV Singapore formula.
    /// The coefficients are exposed as properties so they can be tuned if the authority updates the standard.
    /// </summary>
    public class RetvCalculator
    {
        public EttvModel Model { get; set; }
        public RetvComputationResult Result { get; private set; }

        /// <summary>
        /// Default RETV limit per BCA requirement (W/m²).
        /// </summary>
        public double RetvLimit { get; set; } = 25.0;

        public string Climate { get; set; } = "Tropical";

        /// <summary>
        /// Coefficient for opaque wall heat transmission (W/m²K) factor in the RETV equation.
        /// </summary>
        public double WallConductanceCoefficient { get; set; } = 3.4;

        /// <summary>
        /// Coefficient for fenestration heat transmission (W/m²K) factor in the RETV equation.
        /// </summary>
        public double FenestrationConductanceCoefficient { get; set; } = 1.3;

        /// <summary>
        /// Coefficient for solar gain term in the RETV equation.
        /// </summary>
        public double SolarGainCoefficient { get; set; } = 58.6;

        private static readonly Dictionary<string, double> CoolingFactors = new(StringComparer.OrdinalIgnoreCase)
        {
            { "North", 211.0 },
            { "NorthEast", 218.0 },
            { "East", 224.0 },
            { "SouthEast", 236.0 },
            { "South", 240.0 },
            { "SouthWest", 236.0 },
            { "West", 224.0 },
            { "NorthWest", 218.0 }
        };

        public RetvCalculator() { }

        public RetvCalculator(EttvModel model)
        {
            Model = model;
        }

        /// <summary>
        /// Calculate RETV for the model and populate Model.RetvResult/Result.
        /// Returns the calculated RETV value.
        /// </summary>
        public double? CalculateRetv()
        {
            if (Model == null || Model.Surfaces == null || Model.Surfaces.Count == 0)
            {
                return SetEmptyResult("No surfaces provided for RETV calculation.");
            }

            double sumWeightedRetv = 0.0;
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

                double wallUw = AreaWeightedAverage(
                    surfaces.Where(IsOpaque),
                    s => s.Construction?.Uvalue ?? 0.0);

                double windowUw = AreaWeightedAverage(
                    surfaces.Where(IsFenestration),
                    s => s.Construction?.Uvalue ?? 0.0);

                double windowSc = AreaWeightedAverage(
                    surfaces.Where(IsFenestration),
                    s => GetScTotal(s.Construction as EttvFenestrationConstruction));

                string orientationName = group.Key;
                double cf = GetCoolingFactor(orientationName);

                double wallTerm = WallConductanceCoefficient * wallUw * (wallArea / grossArea);
                double windowConductionTerm = FenestrationConductanceCoefficient * windowUw * (windowArea / grossArea);
                double solarTerm = SolarGainCoefficient * cf * windowSc * (windowArea / grossArea);

                double retv = wallTerm + windowConductionTerm + solarTerm;

                sumWeightedRetv += retv * grossArea;
                totalGrossArea += grossArea;

                orientationBreakdown[orientationName] = retv;

                componentBreakdown[$"{orientationName}-WallArea"] = wallArea;
                componentBreakdown[$"{orientationName}-WindowArea"] = windowArea;
            }

            if (totalGrossArea <= double.Epsilon)
            {
                return SetEmptyResult("Surface areas are zero for RETV calculation.");
            }

            double retvValue = sumWeightedRetv / totalGrossArea;
            bool pass = retvValue <= RetvLimit;

            Result = new RetvComputationResult(retvValue, pass, RetvLimit)
            {
                Climate = Climate,
                OrientationBreakdown = orientationBreakdown,
                ComponentBreakdown = componentBreakdown,
                Notes = pass ? "RETV calculation passed." : $"RETV exceeds limit by {retvValue - RetvLimit:F2} W/m²"
            };

            if (Model != null)
                Model.RetvResult = Result;

            return retvValue;
        }

        private double? SetEmptyResult(string notes)
        {
            Result = new RetvComputationResult
            {
                RetvValue = null,
                Pass = false,
                Limit = RetvLimit,
                Climate = Climate,
                Notes = notes
            };

            if (Model != null)
                Model.RetvResult = Result;

            return null;
        }

        private static double AreaWeightedAverage(IEnumerable<EttvSurface> surfaces, Func<EttvSurface, double> selector)
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

        private static bool IsOpaque(EttvSurface surface) =>
            surface != null && !IsFenestration(surface);

        private static bool IsFenestration(EttvSurface surface) =>
            string.Equals(surface?.Type, "Fenestration", StringComparison.OrdinalIgnoreCase) ||
            surface?.Construction is EttvFenestrationConstruction;

        private static double GetScTotal(EttvFenestrationConstruction fen)
        {
            if (fen == null) return 1.0;

            if (fen.ScTotal > 0.0)
                return fen.ScTotal;

            double sc1 = fen.Sc1 > 0.0 ? fen.Sc1 : 1.0;
            double sc2 = fen.Sc2 > 0.0 ? fen.Sc2 : 1.0;
            double sc = sc1 * sc2;
            return sc > 0.0 ? sc : 1.0;
        }

        private static double GetCoolingFactor(string orientation)
        {
            if (string.IsNullOrEmpty(orientation))
                return 211.0;

            if (CoolingFactors.TryGetValue(orientation, out var value))
                return value;

            return 211.0;
        }

        private static double GetSurfaceArea(EttvSurface surface)
        {
            if (surface?.Geometry is not Mesh mesh) return 0.0;
            if (!mesh.IsValid || mesh.Faces.Count == 0) return 0.0;

            var amp = AreaMassProperties.Compute(mesh);
            if (amp == null || amp.Area <= double.Epsilon) return 0.0;

            return amp.Area;
        }
    }
}
