using System.Collections.Generic;

namespace BcaEttvCore
{
    public class EttvComputationResult
    {
        public double? EttvValue { get; set; }
        public bool? Pass { get; set; }
        public double? Limit { get; set; }
        public string Climate { get; set; } = string.Empty;
        public Dictionary<string, double> OrientationBreakdown { get; set; } = new();
        public Dictionary<string, double> ComponentBreakdown { get; set; } = new();
        public string ComputationDate { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public EttvComputationResult() { }

        public EttvComputationResult(double? ettvValue, bool? pass, double? limit)
        {
            EttvValue = ettvValue;
            Pass = pass;
            Limit = limit;
            ComputationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
