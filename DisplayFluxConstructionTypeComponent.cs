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
    /// <summary>
    /// Previews FluxModel surfaces in Rhino viewport with colors by construction type.
    /// </summary>
    public class DisplayFluxConstructionTypeComponent : GH_Component
    {
        private readonly List<(Mesh mesh, Color color)> _previewMeshes = new();

        public DisplayFluxConstructionTypeComponent()
            : base("Display FluxConstruction Type", "DFT",
                   "Displays FluxSurface geometry colored by construction type (fenestration vs opaque).",
                   "FacadeFlux", "4 :: Utilities")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxModel", "M", "Model containing FluxSurface objects", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Mesh", "Preview meshes with vertex colors", GH_ParamAccess.list);
            pManager.AddGenericParameter("FluxLegend", "L", "Legend data containing Colors, ConstructionType entries, and LegendTitle", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _previewMeshes.Clear();

            if (!TryCollectSurfaces(DA, out var surfaces))
            {
                DA.SetDataList(0, null);
                DA.SetData(1, BuildLegend(null, new[] { "No FluxModel data provided." }, "Flux Construction Types"));
                return;
            }

            var meshesOut = new List<Mesh>();
            var colorsOut = new List<Color>();
            var legendLines = new List<string>();

            var types = surfaces
                .Select(GetTypeLabel)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var colorByType = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                colorByType[type] = ColorForType(type, i);
            }

            foreach (var surface in surfaces)
            {
                if (surface == null)
                    continue;

                var mesh = DuplicateMesh(surface);
                if (mesh == null)
                    continue;

                var typeLabel = GetTypeLabel(surface);
                var color = colorByType[typeLabel];

                ApplyColor(mesh, color);

                meshesOut.Add(mesh);
                _previewMeshes.Add((mesh, color));
            }

            if (meshesOut.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid geometry could be drawn from the provided surfaces.");
                DA.SetDataList(0, null);
                DA.SetData(1, BuildLegend(null, new[] { "No valid geometry generated." }, "Flux Construction Types"));
                return;
            }

            foreach (var type in types)
            {
                legendLines.Add(type);
                colorsOut.Add(colorByType[type]);
            }

            DA.SetDataList(0, meshesOut);
            DA.SetData(1, BuildLegend(colorsOut, legendLines, "Flux Construction Types"));
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

        private static FluxModel UnwrapModel(object raw)
        {
            if (raw is FluxModel direct)
                return direct;

            if (raw is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is FluxModel wrapped)
                    return wrapped;

                if (goo.ScriptVariable() is FluxModel scriptModel)
                    return scriptModel;
            }

            return null;
        }

        private static string GetTypeLabel(FluxSurface surface)
        {
            return surface?.Construction switch
            {
                FluxFenestrationConstruction => "Fenestration",
                FluxOpaqueConstruction => "Opaque",
                _ => "Unspecified"
            };
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

        private static Color PastelColor(int index)
        {
            Color[] palette = new[]
            {
                Color.FromArgb(186, 225, 255),
                Color.FromArgb(255, 223, 186),
                Color.FromArgb(214, 193, 255),
                Color.FromArgb(186, 255, 201),
                Color.FromArgb(255, 204, 229),
                Color.FromArgb(255, 239, 213)
            };

            if (index < palette.Length)
                return palette[index];

            double hue = (index * 131) % 360;
            return FromHsl(hue, 0.35, 0.85);
        }

        private static Color FromHsl(double hueDegrees, double saturation, double lightness)
        {
            double h = hueDegrees / 360.0;
            double r = lightness;
            double g = lightness;
            double b = lightness;

            if (saturation > 0)
            {
                double q = lightness < 0.5
                    ? lightness * (1 + saturation)
                    : lightness + saturation - lightness * saturation;
                double p = 2 * lightness - q;
                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }

            return Color.FromArgb(
                255,
                (int)Math.Round(r * 255),
                (int)Math.Round(g * 255),
                (int)Math.Round(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        private static Color ColorForType(string type, int paletteIndex)
        {
            if (type.Equals("Fenestration", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(40, 85, 170);   // dark blue for fenestration
            if (type.Equals("Opaque", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(178, 34, 34);   // dark red for opaque

            return PastelColor(paletteIndex);
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
        protected override Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DisplayFluxConstructionType.png");
        public override Guid ComponentGuid => new Guid("7E2B9D99-5A1D-4F9B-92E0-FE72C5C5FC24");

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
