using System.Collections.Generic;
using System.Linq;

namespace BcaEttvCore
{
    public class EttvModel
    {
        private static int _projectCounter = 0;
        private static int _versionCounter = 0;

        public string ProjectName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        private List<EttvSurface> _surfaces = new();
        public List<EttvSurface> Surfaces
        {
            get => _surfaces;
            set
            {
                _surfaces = value ?? new List<EttvSurface>();
            }
        }
        public EttvComputationResult ComputationResult { get; set; }

        public EttvModel() { }

        public EttvModel(List<EttvSurface> surfaces)
        {
            Surfaces = surfaces ?? new List<EttvSurface>();
            ProjectName = $"EttvProject_{_projectCounter++}";
            Version = (_versionCounter++).ToString();
        }

        /// <summary>
        /// Cluster surfaces based on EttvOrientation.Name into nested lists of EttvSurface.
        /// Does not mutate the Surfaces list order. Sets Reordered = true.
        /// </summary>



    }
}