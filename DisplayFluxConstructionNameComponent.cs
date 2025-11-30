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
    /// Visualizes FluxSurface geometry colored by FluxConstruction.Name.
    /// </summary>
    public class DisplayFluxConstructionNameComponent : GH_Component
    {
        private readonly List<(Mesh mesh, Color color)> _previewMeshes = new();

        public DisplayFluxConstructionNameComponent()
            : base("Display FluxConstruction Names", "DCN",
                   "Displays FluxSurface geometry colored by FluxConstruction.Name.",
                   "FacadeFlux", "4 :: Utilities")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxModel", "M", "FluxModel containing surfaces", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Mesh", "Preview meshes with vertex colors", GH_ParamAccess.list);
            pManager.AddColourParameter("Colors", "C", "Colors mapped to FluxConstruction.Name in legend order", GH_ParamAccess.list);
            pManager.AddTextParameter("Legend", "L", "Legend showing FluxConstruction.Name entries in the same order as Colors", GH_ParamAccess.list);
            pManager.AddTextParameter("LegendTitle", "T", "Legend title matching the Colors/Legend outputs", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _previewMeshes.Clear();

            if (!TryCollectSurfaces(DA, out var surfaces))
            {
                DA.SetDataList(0, null);
                DA.SetDataList(1, null);
                DA.SetDataList(2, new[] { "No FluxModel data provided." });
                DA.SetData(3, "Flux Construction Names");
                return;
            }

            var meshesOut = new List<Mesh>();
            var colorsOut = new List<Color>();
            var legendLines = new List<string>();

            var names = surfaces
                .Select(s => s?.Construction?.Name ?? "Unspecified")
                .Distinct()
                .ToList();

            var colorByName = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < names.Count; i++)
            {
                var name = names[i] ?? "Unspecified";
                colorByName[name] = PastelColor(i);
            }

            foreach (var surface in surfaces)
            {
                if (surface == null)
                    continue;

                var mesh = DuplicateMesh(surface);
                if (mesh == null)
                    continue;

                var name = surface.Construction?.Name ?? "Unspecified";
                var color = colorByName[name];

                ApplyColor(mesh, color);

                meshesOut.Add(mesh);
                _previewMeshes.Add((mesh, color));
            }

            if (meshesOut.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid geometry could be drawn from the provided surfaces.");
                DA.SetDataList(0, null);
                DA.SetDataList(1, null);
                DA.SetDataList(2, new[] { "No valid geometry generated." });
                DA.SetData(3, "Flux Construction Names");
                return;
            }

            foreach (var name in names)
            {
                var color = colorByName[name];
                legendLines.Add(name);
                colorsOut.Add(color);
            }

            DA.SetDataList(0, meshesOut);
            DA.SetDataList(1, colorsOut);
            DA.SetDataList(2, legendLines);
            DA.SetData(3, "Flux Construction Names");
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
            // Cycle through a palette of contrasty pastel colors; fall back to hashed hues if exceeded.
            Color[] palette = new[]
            {
                Color.FromArgb(255, 179, 186),
                Color.FromArgb(255, 223, 186),
                Color.FromArgb(255, 255, 186),
                Color.FromArgb(186, 255, 201),
                Color.FromArgb(186, 225, 255),
                Color.FromArgb(214, 193, 255),
                Color.FromArgb(255, 204, 229),
                Color.FromArgb(204, 229, 255),
                Color.FromArgb(222, 243, 255),
                Color.FromArgb(255, 239, 213)
            };

            if (index < palette.Length)
                return palette[index];

            // Generate additional pastels by spinning hue.
            double hue = (index * 137) % 360; // golden angle for distribution
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

        protected override Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DisplayFluxConstructionName.png");

        public override Guid ComponentGuid => new Guid("0D38C6C2-2E52-4BD5-9F0F-4E53A2E4F312");
    }
}
