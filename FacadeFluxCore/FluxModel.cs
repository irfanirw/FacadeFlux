using System.Collections.Generic;
using System.Linq;

namespace FacadeFluxCore
{
    public class FluxModel
    {
        private static int _projectCounter = 0;
        private static int _versionCounter = 0;

        public string ProjectName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        private List<FluxSurface> _surfaces = new();
        public List<FluxSurface> Surfaces
        {
            get => _surfaces;
            set
            {
                _surfaces = value ?? new List<FluxSurface>();
            }
        }
        public EttvComputationResult ComputationResult { get; set; }
        public RetvComputationResult RetvResult { get; set; }

        public FluxModel() { }

        public FluxModel(List<FluxSurface> surfaces)
        {
            Surfaces = surfaces ?? new List<FluxSurface>();
            ProjectName = $"FluxProject_{_projectCounter++}";
            Version = (_versionCounter++).ToString();
        }

        /// <summary>
        /// Cluster surfaces based on FluxOrientation.Name into nested lists of FluxSurface.
        /// Does not mutate the Surfaces list order. Sets Reordered = true.
        /// </summary>



    }
}
