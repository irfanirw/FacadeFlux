using System;

namespace FacadeFluxCore
{
    public static class HorizontalSc2Calculator
    {
        // R1 values span 0.1 to 3.0 in 0.1 increments.
        private const double MinR1 = 0.1;
        private const double MaxR1 = 3.0;

        private static readonly double[] TableEastWest =
        {
            0.9363, 0.8752, 0.8228, 0.7703, 0.7248, 0.6911, 0.6574, 0.6237, 0.5998, 0.5827,
            0.5656, 0.5485, 0.5314, 0.5156, 0.5051, 0.4995, 0.4939, 0.4882, 0.4826, 0.4770,
            0.4713, 0.4657, 0.4601, 0.4544, 0.4488, 0.4432, 0.4400, 0.4369, 0.4339, 0.4333
        };

        private static readonly double[] TableNorthSouth =
        {
            0.9380, 0.8773, 0.8167, 0.7560, 0.7210, 0.7041, 0.6923, 0.6871, 0.6819, 0.6767,
            0.6731, 0.6713, 0.6705, 0.6698, 0.6690, 0.6683, 0.6675, 0.6667, 0.6660, 0.6652,
            0.6645, 0.6637, 0.6630, 0.6622, 0.6614, 0.6607, 0.6604, 0.6601, 0.6599, 0.6596
        };

        private static readonly double[] TableNorthEastWest =
        {
            0.9273, 0.8630, 0.8054, 0.7563, 0.7171, 0.6787, 0.6549, 0.6327, 0.6105, 0.5922,
            0.5809, 0.5722, 0.5634, 0.5547, 0.5466, 0.5413, 0.5359, 0.5306, 0.5253, 0.5200,
            0.5162, 0.5141, 0.5119, 0.5097, 0.5075, 0.5053, 0.5047, 0.5042, 0.5036, 0.5031
        };

        private static readonly double[] TableSouthEastWest =
        {
            0.9253, 0.8574, 0.7964, 0.7413, 0.6981, 0.6578, 0.6289, 0.6059, 0.5828, 0.5619,
            0.5502, 0.5413, 0.5323, 0.5234, 0.5150, 0.5096, 0.5042, 0.4988, 0.4933, 0.4879,
            0.4841, 0.4820, 0.4798, 0.4777, 0.4755, 0.4734, 0.4712, 0.4699, 0.4694, 0.4688
        };

        public static double Calculate(double projection, double height, FluxOrientation orientation)
        {
            if (height <= 0 || projection < 0)
                return 1.0;

            double r1 = projection / height;
            var orientationKey = !string.IsNullOrWhiteSpace(orientation?.Name)
                ? orientation.Name
                : orientation?.Id;
            var table = SelectTable(orientationKey);

            return SampleSc2(table, r1);
        }

        private static double[] SelectTable(string orientationName)
        {
            if (string.IsNullOrWhiteSpace(orientationName))
                return TableNorthSouth;

            var key = NormalizeOrientationName(orientationName);

            switch (key)
            {
                case "north":
                case "south":
                case "n":
                case "s":
                    return TableNorthSouth;

                case "east":
                case "west":
                case "e":
                case "w":
                    return TableEastWest;

                case "northeast":
                case "northwest":
                case "ne":
                case "nw":
                    return TableNorthEastWest;

                case "southeast":
                case "southwest":
                case "se":
                case "sw":
                    return TableSouthEastWest;

                default:
                    return TableNorthSouth;
            }
        }

        private static string NormalizeOrientationName(string name)
        {
            var trimmed = name.Trim().ToLowerInvariant();
            // Remove separators like spaces or hyphens, e.g., "north east" => "northeast".
            trimmed = trimmed.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
            return trimmed;
        }

        private static double SampleSc2(double[] table, double ratio)
        {
            if (table == null || table.Length == 0)
                return 1.0;

            double clamped = Math.Max(MinR1, Math.Min(MaxR1, ratio));
            double scaledIndex = (clamped - MinR1) / (MaxR1 - MinR1) * (table.Length - 1);

            int lower = (int)Math.Floor(scaledIndex);
            int upper = Math.Min(table.Length - 1, lower + 1);
            double t = scaledIndex - lower;

            return table[lower] * (1 - t) + table[upper] * t;
        }
    }
}
