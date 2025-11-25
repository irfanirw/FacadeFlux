using System.Collections.Generic;

namespace BcaEttvCore
{
    public class RetvComputationResult
    {
        public double? RetvValue { get; set; }
        public bool? Pass { get; set; }
        public double? Limit { get; set; }
        public string Climate { get; set; } = string.Empty;
        public Dictionary<string, double> OrientationBreakdown { get; set; } = new();
        public Dictionary<string, double> ComponentBreakdown { get; set; } = new();
        public string ComputationDate { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public RetvComputationResult() { }

        public RetvComputationResult(double? retvValue, bool? pass, double? limit)
        {
            RetvValue = retvValue;
            Pass = pass;
            Limit = limit;
            ComputationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
