using System;

namespace FacadeFlux
{
    internal static class IconHelper
    {
        /// <summary>
        /// Loads an embedded PNG icon from the assembly. Returns null on non-Windows platforms
        /// because System.Drawing.Common is only supported on Windows in .NET 7.
        /// </summary>
        /// <param name="resourceName">Full resource path (e.g., "FacadeFlux.Icons.MyIcon.png").</param>
        /// <returns>Bitmap when available, otherwise null.</returns>
        public static System.Drawing.Bitmap LoadIcon(string resourceName)
        {
            if (!IsWindows())
                return null;

            var assembly = typeof(IconHelper).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            return stream is null ? null : new System.Drawing.Bitmap(stream);
        }

        private static bool IsWindows()
        {
#if NET7_0_OR_GREATER
            return OperatingSystem.IsWindows();
#else
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif
        }
    }
}
