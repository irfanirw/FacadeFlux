using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class DisplayFluxConstructionUvalueComponent : GH_Component
    {
        private readonly List<(Mesh mesh, Color color)> _previewMeshes = new();

        public DisplayFluxConstructionUvalueComponent()
            : base("Display FluxConstruction U-Value", "DUV",
                   "Displays FluxSurface geometry colored by FluxConstruction.Uvalue.",
                   "FacadeFlux", "4 :: Utilities")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxModel", "M", "FluxModel to inspect", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Mesh", "Preview meshes with vertex colors", GH_ParamAccess.list);
            pManager.AddGenericParameter("FluxLegend", "L", "Legend data containing Colors, Uvalue entries, and LegendTitle", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _previewMeshes.Clear();

            if (!TryCollectSurfaces(DA, out var surfaces))
            {
                DA.SetDataList(0, null);
                DA.SetData(1, BuildLegend(null, new[] { "No FluxModel data provided." }, "U-value (W/m²K)"));
                return;
            }

            var meshesOut = new List<Mesh>();
            var colorsOut = new List<Color>();
            var legendLines = new List<string>();

            var uValues = surfaces
                .Select(s => s?.Construction?.Uvalue)
                .Where(v => v.HasValue && !double.IsNaN(v.Value))
                .Select(v => v.Value)
                .ToList();

            double minU = uValues.Count > 0 ? uValues.Min() : 0d;
            double maxU = uValues.Count > 0 ? uValues.Max() : 1d;

            foreach (var surface in surfaces)
            {
                if (surface == null)
                    continue;

                var mesh = DuplicateMesh(surface);
                if (mesh == null)
                    continue;

                double? uVal = surface.Construction?.Uvalue;
                var color = uVal.HasValue && !double.IsNaN(uVal.Value)
                    ? JetColor(Normalize(uVal.Value, minU, maxU))
                    : Color.LightGray;

                ApplyColor(mesh, color);

                meshesOut.Add(mesh);
                _previewMeshes.Add((mesh, color));
            }

            if (meshesOut.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid geometry could be drawn from the provided surfaces.");
                DA.SetDataList(0, null);
                DA.SetData(1, BuildLegend(null, new[] { "No valid geometry generated." }, "U-value (W/m²K)"));
                return;
            }

            if (uValues.Count == 0)
            {
                legendLines.Add("Unspecified");
                colorsOut.Add(Color.LightGray);
            }
            else
            {
                var legendValues = BuildLegendStops(minU, maxU);
                foreach (var val in legendValues)
                {
                    colorsOut.Add(JetColor(Normalize(val, minU, maxU)));
                    legendLines.Add(FormatUvalue(val));
                }
            }

            DA.SetDataList(0, meshesOut);
            DA.SetData(1, BuildLegend(colorsOut, legendLines, "U-value (W/m²K)"));
        }

        private static bool TryCollectSurfaces(IGH_DataAccess DA, out List<FluxSurface> surfaces)
        {
            surfaces = new List<FluxSurface>();
            object raw = null;
            DA.GetData(0, ref raw);

            var model = UnwrapModel(raw);
            if (model?.Surfaces == null)
                return false;

            surfaces = model.Surfaces.Where(s => s != null && s.Geometry != null).ToList();
            return surfaces.Count > 0;
        }

        private static FluxModel UnwrapModel(object value)
        {
            if (value is FluxModel model)
                return model;

            if (value is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is FluxModel wrapped)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is FluxModel scriptModel)
                    return scriptModel;
            }

            return null;
        }

        private static string FormatUvalue(double? value)
        {
            if (!value.HasValue || double.IsNaN(value.Value))
                return "Unspecified";

            return value.Value.ToString("0.###");
        }

        private static Mesh DuplicateMesh(FluxSurface surface)
        {
            switch (surface?.Geometry as object)
            {
                case Mesh mesh when mesh.IsValid:
                    return mesh.DuplicateMesh();
                case Brep brep:
                    var meshes = Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh);
                    return CombineMeshes(meshes);
                case Surface surf:
                    var brepSurf = surf.ToBrep();
                    var surfMeshes = Mesh.CreateFromBrep(brepSurf, MeshingParameters.FastRenderMesh);
                    return CombineMeshes(surfMeshes);
                default:
                    return null;
            }
        }

        private static Mesh CombineMeshes(IEnumerable<Mesh> meshes)
        {
            if (meshes == null)
                return null;

            var combined = new Mesh();
            foreach (var mesh in meshes)
            {
                if (mesh == null || !mesh.IsValid)
                    continue;

                combined.Append(mesh);
            }

            if (combined.Faces.Count == 0)
                return null;

            combined.Normals.ComputeNormals();
            combined.Compact();
            return combined;
        }

        private static void ApplyColor(Mesh mesh, Color color)
        {
            if (mesh.VertexColors == null || mesh.VertexColors.Count == 0)
            {
                mesh.VertexColors.CreateMonotoneMesh(color);
                return;
            }

            for (int i = 0; i < mesh.VertexColors.Count; i++)
                mesh.VertexColors[i] = color;
        }

        private static List<double> BuildLegendStops(double min, double max, int steps = 7)
        {
            if (Math.Abs(max - min) < 1e-9)
                return new List<double> { min };

            var values = new List<double>();
            for (int i = 0; i < steps; i++)
            {
                double t = i / (double)(steps - 1);
                values.Add(min + (max - min) * t);
            }

            return values;
        }

        private static double Normalize(double value, double min, double max)
        {
            if (Math.Abs(max - min) < 1e-9)
                return 0.5;

            double t = (value - min) / (max - min);
            return Clamp01(t);
        }

        private static double Clamp01(double value)
        {
            if (value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }

        private static Color JetColor(double t)
        {
            t = Clamp01(t);

            double r = Clamp01(1.5 - Math.Abs(4 * t - 3));
            double g = Clamp01(1.5 - Math.Abs(4 * t - 2));
            double b = Clamp01(1.5 - Math.Abs(4 * t - 1));

            return Color.FromArgb(
                255,
                (int)Math.Round(r * 255),
                (int)Math.Round(g * 255),
                (int)Math.Round(b * 255));
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            foreach (var (mesh, color) in _previewMeshes)
            {
                if (mesh == null) continue;
                args.Display.DrawMeshShaded(mesh, new DisplayMaterial(color));
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            foreach (var (mesh, color) in _previewMeshes)
            {
                if (mesh == null) continue;
                args.Display.DrawMeshWires(mesh, color);
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                var bbox = BoundingBox.Empty;
                foreach (var (mesh, _) in _previewMeshes)
                {
                    if (mesh == null) continue;
                    bbox.Union(mesh.GetBoundingBox(true));
                }
                return bbox;
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DisplaySurfaceUvalue.png");

        public override Guid ComponentGuid => new Guid("651C0097-AF5D-4C93-B13C-07C53AF61838");

        private static FluxLegend BuildLegend(IEnumerable<Color> colors, IEnumerable<string> labels, string title)
        {
            var legendColors = new List<GH_Colour>();
            if (colors != null)
            {
                foreach (var c in colors)
                    legendColors.Add(new GH_Colour(c));
            }

            var legendLabels = new List<object>();
            if (labels != null)
            {
                foreach (var l in labels)
                    legendLabels.Add(l);
            }

            return new FluxLegend
            {
                Colors = legendColors,
                Legend = legendLabels,
                LegendTitle = title ?? string.Empty
            };
        }
    }
}
