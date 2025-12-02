using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using FacadeFluxCore;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace FacadeFlux
{
    /// <summary>
    /// Draws a viewport legend that pairs labels with colors.
    /// </summary>
    public class FluxLegendComponent : GH_Component
    {
        private readonly List<string> _legend = new();
        private readonly List<Color> _colors = new();
        private string _title = "Legend";

        public FluxLegendComponent()
            : base("FluxLegend", "Legend",
                   "Displays a legend bar in the active viewport matching labels and colors.",
                   "FacadeFlux", "4 :: Utilities")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxLegend", "L", "FluxLegend containing Colors, Legend entries, and LegendTitle", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Intentionally no outputs; this component only renders to the viewport.
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _legend.Clear();
            _colors.Clear();

            if (!TryGetLegend(DA, out var legend))
            {
                return;
            }

            _legend.AddRange(legend.labels);
            _colors.AddRange(legend.colors);
            _title = legend.title;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            DrawLegend(args);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            // No shaded geometry to render for this component.
        }

        private void DrawLegend(IGH_PreviewArgs args)
        {
            if (_legend.Count == 0 || _colors.Count == 0)
                return;

            var size = args.Viewport.Size;
            if (size.Width <= 0 || size.Height <= 0)
                return;

            const int fontHeight = 24;
            const int swatchSize = 28;
            const int padding = 16;
            const int gap = 8;

            int count = Math.Min(_legend.Count, _colors.Count);
            int maxLabelWidth = Math.Max(EstimateMaxTextWidth(_legend, fontHeight), EstimateTextWidth(_title, fontHeight));
            int rowHeight = Math.Max(swatchSize, fontHeight) + gap;
            int titleHeight = fontHeight + gap;
            int boxWidth = padding * 3 + swatchSize + maxLabelWidth;
            int boxHeight = padding * 2 + titleHeight + count * rowHeight;

            int originX = size.Width - boxWidth - padding;
            int originY = size.Height - boxHeight - padding;

            var background = new Rectangle(originX, originY, boxWidth, boxHeight);
            args.Display.Draw2dRectangle(background, Color.FromArgb(120, 40, 40, 40), 1, Color.FromArgb(220, 250, 250, 250));

            int currentY = originY + padding;

            if (titleHeight > 0)
            {
                var titlePoint = new Point2d(originX + padding, currentY);
                args.Display.Draw2dText(_title, Color.Black, titlePoint, false, fontHeight);
                currentY += titleHeight;
            }

            for (int i = 0; i < count; i++)
            {
                int lineY = currentY + i * rowHeight;
                var swatchRect = new Rectangle(originX + padding, lineY, swatchSize, swatchSize);
                args.Display.Draw2dRectangle(swatchRect, Color.FromArgb(200, 30, 30, 30), 1, _colors[i]);

                // Align text vertically with the swatch using its top-left corner as the anchor for Draw2dText.
                // Align text vertically with the swatch using its top-left corner as the anchor for Draw2dText.
                double textY = swatchRect.Y + (swatchSize - fontHeight) * 0.5;
                var textPoint = new Point2d(swatchRect.Right + padding, textY);
                args.Display.Draw2dText(_legend[i], Color.Black, textPoint, false, fontHeight);
            }
        }

        private static int EstimateMaxTextWidth(IEnumerable<string> lines, int fontHeight)
        {
            int max = 0;
            foreach (var line in lines)
                max = Math.Max(max, EstimateTextWidth(line, fontHeight));
            return max;
        }

        private static int EstimateTextWidth(string text, int fontHeight)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Approximate width assuming a 0.6 aspect ratio for most fonts.
            return (int)Math.Ceiling(text.Length * fontHeight * 0.6);
        }

        public override BoundingBox ClippingBox => BoundingBox.Empty;
        public override bool IsPreviewCapable => true;
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxLegend.png");
        public override Guid ComponentGuid => new Guid("265106EA-9C18-4BC0-8B5E-A02FD78D3DC6");

        private bool TryGetLegend(IGH_DataAccess DA, out (List<string> labels, List<Color> colors, string title) legend)
        {
            legend = (new List<string>(), new List<Color>(), "Legend");

            object raw = null;
            if (!DA.GetData(0, ref raw))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provide a FluxLegend object.");
                return false;
            }

            var fluxLegend = UnwrapLegend(raw);
            if (fluxLegend == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid FacadeFluxCore.FluxLegend.");
                return false;
            }

            var colors = new List<Color>();
            foreach (var entry in fluxLegend.Colors?.Cast<object>() ?? Enumerable.Empty<object>())
            {
                switch (entry)
                {
                    case Color color:
                        colors.Add(color);
                        break;
                    case GH_Colour ghColor when ghColor != null:
                        colors.Add(ghColor.Value);
                        break;
                    case GH_Integer ghInt:
                        colors.Add(Color.FromArgb(ghInt.Value));
                        break;
                    case GH_String ghString when TryParseColor(ghString.Value, out var parsed):
                        colors.Add(parsed);
                        break;
                }
            }

            var labels = new List<string>();
            foreach (var item in fluxLegend.Legend ?? new List<object>())
            {
                if (TryNormalizeLabel(item, out var label))
                    labels.Add(label);
            }

            int count = Math.Min(labels.Count, colors.Count);
            if (count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "FluxLegend must contain both Colors and Legend entries.");
                return false;
            }

            if (labels.Count != colors.Count)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "FluxLegend Legend and Colors lengths differ; truncating to the shorter list.");

            legend = (labels.Take(count).ToList(), colors.Take(count).ToList(), NormalizeTitle(fluxLegend.LegendTitle));
            return true;
        }

        private static FluxLegend UnwrapLegend(object raw)
        {
            if (raw is FluxLegend direct)
                return direct;

            if (raw is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper)
                {
                    if (wrapper.Value is FluxLegend wrapped && wrapper.Value.GetType().Assembly == typeof(FluxLegend).Assembly)
                        return wrapped;
                }

                if (goo.ScriptVariable() is FluxLegend scriptLegend)
                    return scriptLegend;
            }

            return null;
        }

        private static bool TryNormalizeLabel(object value, out string label)
        {
            label = null;

            switch (value)
            {
                case null:
                    return false;
                case string s when !string.IsNullOrWhiteSpace(s):
                    label = s.Trim();
                    return true;
                case GH_String ghs when !string.IsNullOrWhiteSpace(ghs.Value):
                    label = ghs.Value.Trim();
                    return true;
                case GH_Number ghn when !double.IsNaN(ghn.Value):
                    label = FormatNumber(ghn.Value);
                    return true;
                case GH_Integer ghi:
                    label = ghi.Value.ToString(CultureInfo.InvariantCulture);
                    return true;
                case double d when !double.IsNaN(d):
                    label = FormatNumber(d);
                    return true;
                case float f when !float.IsNaN(f):
                    label = FormatNumber(f);
                    return true;
                case int i:
                    label = i.ToString(CultureInfo.InvariantCulture);
                    return true;
                case long l:
                    label = l.ToString(CultureInfo.InvariantCulture);
                    return true;
            }

            return false;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("G6", CultureInfo.InvariantCulture);
        }

        private static string NormalizeTitle(string title)
        {
            return string.IsNullOrWhiteSpace(title) ? "Legend" : title.Trim();
        }

        private static bool TryParseColor(string text, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                color = ColorTranslator.FromHtml(text);
                return color.ToArgb() != Color.Empty.ToArgb();
            }
            catch
            {
                return false;
            }
        }
    }
}
