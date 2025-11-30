using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.DocObjects;

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
            pManager.AddColourParameter("Colors", "C", "Colors aligned by index with Legend", GH_ParamAccess.list);
            pManager.AddTextParameter("Legend", "L", "Legend labels", GH_ParamAccess.list);
            pManager.AddTextParameter("Legend Title", "T", "Optional legend title", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Intentionally no outputs; this component only renders to the viewport.
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _legend.Clear();
            _colors.Clear();

            var colors = new List<Color>();
            var labels = new List<string>();
            string title = null;

            DA.GetDataList(0, colors);
            DA.GetDataList(1, labels);
            DA.GetData(2, ref title);

            int count = Math.Min(labels.Count, colors.Count);
            if (count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provide both Legend labels and Colors.");
                return;
            }

            if (labels.Count != colors.Count)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Legend and Colors lengths differ; truncating to the shorter list.");

            _legend.AddRange(labels.Take(count));
            _colors.AddRange(colors.Take(count));
            _title = string.IsNullOrWhiteSpace(title) ? "Legend" : title.Trim();
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
    }
}
