using System.Collections.Generic;

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
    }
}
