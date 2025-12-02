using System.Collections.Generic;
using Grasshopper.Kernel.Types;

namespace FacadeFluxCore
{
    /// <summary>
    /// Container for legend metadata passed between Grasshopper components.
    /// </summary>
    public class FluxLegend
    {
        /// <summary>
        /// Colors aligned with the Legend entries. Each item should be a GH_Colour wrapping an RGB value.
        /// </summary>
        public List<GH_Colour> Colors { get; set; } = new();

        /// <summary>
        /// Legend labels or numeric values paired with Colors. Values should be string or double.
        /// </summary>
        public List<object> Legend { get; set; } = new();

        /// <summary>
        /// Display title for the legend.
        /// </summary>
        public string LegendTitle { get; set; } = string.Empty;
    }
}
