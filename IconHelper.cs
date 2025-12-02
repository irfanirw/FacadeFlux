using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FacadeFlux
{
    internal static class IconHelper
    {
        /// <summary>
        /// Loads an embedded PNG icon from the assembly. Returns null if it cannot be loaded.
        /// </summary>
        /// <param name="resourceName">Full resource path (e.g., "FacadeFlux.Icons.MyIcon.png").</param>
        /// <returns>Bitmap when available, otherwise null.</returns>
        public static Bitmap LoadIcon(string resourceName)
        {
#if NET7_0_OR_GREATER
            // Allow System.Drawing on non-Windows (requires libgdiplus on Unix).
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
#endif

            var assembly = typeof(IconHelper).Assembly;
            Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                // Fallback: try to find an alternative resource name ending with the same suffix.
                var alt = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase)
                                      || n.EndsWith(resourceName.Replace("FacadeFlux.", string.Empty), StringComparison.OrdinalIgnoreCase));
                if (alt != null)
                    stream = assembly.GetManifestResourceStream(alt);
            }

            if (stream == null)
                return null;

            try
            {
                using (stream)
                {
                    return new Bitmap(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
