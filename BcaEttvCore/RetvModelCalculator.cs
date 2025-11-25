using System;
using System.Collections.Generic;
using System.Linq;

namespace BcaEttvCore
{
    public static class RetvModelCalculator
    {
        public static RetvModelResult Calculate(EttvModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var surfaceList = model.Surfaces?
                                   .Where(s => s != null)
                                   .ToList() ?? new List<EttvSurface>();

            var result = new RetvModelResult
            {
                ProjectName = model.ProjectName,
                Version = model.Version,
                Surfaces = surfaceList
            };

            if (surfaceList.Count == 0)
            {
                result.ResultSummary = "No EttvSurface objects supplied.";
                return result;
            }

            foreach (var group in surfaceList.GroupBy(s => s.Orientation?.Id ?? "UNDEFINED"))
            {
                var orientation = group.First().Orientation ?? new EttvOrientation
                {
                    Id = "UNDEFINED",
                    Name = "Undefined"
                };
                orientation.SetCf();

                var list = group.ToList();

                var orientationResult = new RetvOrientationResult
                {
                    Orientation = orientation,
                    Surfaces = list,
                    UniqueConstructions = list.Select(s => s.Construction)
                                              .Where(c => c != null)
                                              .Distinct()
                                              .ToList()
                };

                orientationResult.WallArea = list.Where(IsOpaque).Sum(s => s.Area);
                orientationResult.WindowArea = list.Where(IsFenestration).Sum(s => s.Area);

                var gross = orientationResult.GrossArea;
                var wwr = orientationResult.Wwr;

                double wallU = AreaWeightedAverage(list.Where(IsOpaque),
                                                   s => s.Construction?.Uvalue ?? 0d);
                wallU = wallU > 0 ? wallU : 0.0;

                double fenestrationU = AreaWeightedAverage(list.Where(IsFenestration),
                                                           s => s.Construction?.Uvalue ?? 0d);
                fenestrationU = fenestrationU > 0 ? fenestrationU : 0.0;

                double sc = AreaWeightedAverage(list.Where(IsFenestration),
                                                s => GetScTotal(s.Construction as EttvFenestrationConstruction));

                double cf = orientation.Cf <= 0 ? 1.0 : orientation.Cf;

                double retvValue = gross > 0
                    ? (12.0 * wallU * (orientationResult.WallArea / gross))
                      + (3.4 * fenestrationU * (orientationResult.WindowArea / gross))
                      + (0.305 * cf * sc * (orientationResult.WindowArea / gross))
                    : 0.0;

                orientationResult.AverageRetv = retvValue;
                orientationResult.TotalEnvelopeGain = retvValue * gross;
                orientationResult.BuildSummary();

                result.WallArea += orientationResult.WallArea;
                result.WindowArea += orientationResult.WindowArea;
                result.TotalEnvelopeGain += orientationResult.TotalEnvelopeGain;
                result.ResultByOrientation.Add(orientationResult);
            }

            result.AverageRetv = result.GrossArea > 0
                ? result.TotalEnvelopeGain / result.GrossArea
                : 0d;

            result.OverallAverageRetv = result.ResultByOrientation.Count > 0 && result.GrossArea > 0
                ? result.ResultByOrientation.Sum(o => o.AverageRetv * o.GrossArea) / result.GrossArea
                : 0d;

            result.BuildSummary();
            return result;
        }

        private static bool IsOpaque(EttvSurface surface) =>
            surface?.Construction is EttvOpaqueConstruction;

        private static bool IsFenestration(EttvSurface surface) =>
            surface?.Construction is EttvFenestrationConstruction;

        private static double AreaWeightedAverage(IEnumerable<EttvSurface> surfaces, Func<EttvSurface, double> selector)
        {
            double numerator = 0d;
            double areaSum = 0d;

            foreach (var surface in surfaces)
            {
                var area = surface?.Area ?? 0d;
                if (area <= 0)
                    continue;

                numerator += selector(surface) * area;
                areaSum += area;
            }

            return areaSum > 0 ? numerator / areaSum : 0d;
        }

        private static double GetScTotal(EttvFenestrationConstruction construction)
        {
            if (construction == null)
                return 1d;

            if (construction.ScTotal > 0d)
                return construction.ScTotal;

            double sc1 = construction.Sc1 > 0d ? construction.Sc1 : 1d;
            double sc2 = construction.Sc2 > 0d ? construction.Sc2 : 1d;
            double sc = sc1 * sc2;
            return sc > 0d ? sc : 1d;
        }
    }
}
