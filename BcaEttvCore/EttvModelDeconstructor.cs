using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BcaEttvCore
{
    public static class EttvModelDeconstructor
    {
        public static void Deconstruct(EttvModel model, out string summary, out List<EttvSurface> surfaces)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            surfaces = model.Surfaces?.ToList() ?? new List<EttvSurface>();

            double totalGrossArea = surfaces.Sum(s => s?.Area ?? 0d);
            double totalFenestrationArea = surfaces.Sum(s => s?.Area ?? 0d);
            var orientations = surfaces
                .Select(s => s?.Orientation)
                .Where(o => o != null)
                .Select(o => o.ToString())
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Project name: {model.ProjectName ?? "-"}");
            sb.AppendLine($"Version: {model.Version ?? "-"}");
            sb.AppendLine($"Total gross area: {totalGrossArea:0.###}");
            sb.AppendLine($"Total fenestration area: {totalFenestrationArea:0.###}");
            sb.AppendLine($"Orientations: {(orientations.Count == 0 ? "-" : string.Join(", ", orientations))}");
            sb.AppendLine($"Total EttvSurfaces: {surfaces.Count}");

            for (int i = 0; i < surfaces.Count; i++)
            {
                var surface = surfaces[i];
                string constructionInfo = surface?.Construction?.Name ?? "N/A";
                sb.AppendLine($"  [{i + 1}] {surface?.Name ?? "Unnamed"} -> Construction: {constructionInfo}");
            }

            summary = sb.ToString().TrimEnd();
        }
    }
}