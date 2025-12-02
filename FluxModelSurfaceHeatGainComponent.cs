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
    public class FluxModelSurfaceHeatGainComponent : GH_Component
    {
        private readonly List<(Mesh mesh, Color color)> _previewMeshes = new();

        public FluxModelSurfaceHeatGainComponent()
            : base("FluxModelSurfaceHeatGain", "FMSHG",
                   "Preview FluxModelResult surfaces colored by average heat gain (Jet scale).",
                   "FacadeFlux", "3 :: Post-processing")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxModelResult", "R", "Computed EttvModelResult or RetvModelResult", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Mesh", "FluxSurface geometries colored by heat gain", GH_ParamAccess.list);
            pManager.AddColourParameter("Colors", "C", "Legend colors from low to high heat gain (Jet scale)", GH_ParamAccess.list);
            pManager.AddNumberParameter("HeatGain", "H", "Average heat gain legend values aligned with Colors (W/m²)", GH_ParamAccess.list);
            pManager.AddTextParameter("LegendTitle", "T", "Legend title", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _previewMeshes.Clear();

            if (!TryCollectSurfaces(DA, out var surfaces))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxModelResult data provided.");
                DA.SetDataList(0, null);
                DA.SetDataList(1, null);
                DA.SetDataList(2, null);
                DA.SetData(3, "Average HeatGain (W/m²)");
                return;
            }

            var meshData = new List<(FluxSurface surface, Mesh mesh, double avgHeatGain)>();
            foreach (var surface in surfaces)
            {
                if (surface == null)
                    continue;

                EnsureHeatGain(surface);

                var mesh = DuplicateMesh(surface);
                if (mesh == null)
                    continue;

                double avgHeat = ComputeAverageHeatGain(surface);
                meshData.Add((surface, mesh, avgHeat));
            }

            if (meshData.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid geometry could be drawn from the provided surfaces.");
                DA.SetDataList(0, null);
                DA.SetDataList(1, null);
                DA.SetDataList(2, null);
                DA.SetData(3, "Average HeatGain (W/m²)");
                return;
            }

            double minHeat = meshData.Min(m => m.avgHeatGain);
            double maxHeat = meshData.Max(m => m.avgHeatGain);

            var meshesOut = new List<Mesh>();
            foreach (var (_, mesh, avgHeat) in meshData)
            {
                double t = Normalize(avgHeat, minHeat, maxHeat);
                var color = JetColor(t);
                ApplyColor(mesh, color);

                meshesOut.Add(mesh);
                _previewMeshes.Add((mesh, color));
            }

            var legendValues = BuildLegendStops(minHeat, maxHeat);
            var legendColors = legendValues
                .Select(v => JetColor(Normalize(v, minHeat, maxHeat)))
                .ToList();

            DA.SetDataList(0, meshesOut);
            DA.SetDataList(1, legendColors);
            DA.SetDataList(2, legendValues);
            DA.SetData(3, "Average HeatGain (W/m²)");
        }

        private static bool TryCollectSurfaces(IGH_DataAccess DA, out List<FluxSurface> surfaces)
        {
            surfaces = new List<FluxSurface>();
            object rawResult = null;
            DA.GetData(0, ref rawResult);

            var model = UnwrapModel(rawResult);
            if (model?.Surfaces == null)
                return false;

            surfaces = model.Surfaces.Where(s => s?.Geometry != null).ToList();
            return surfaces.Count > 0;
        }

        private static FluxModel UnwrapModel(object value)
        {
            switch (value)
            {
                case EttvModelResult ettv:
                    return ettv;
                case RetvModelResult retv:
                    return retv;
                case FluxModel model:
                    return model;
            }

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

        private static void EnsureHeatGain(FluxSurface surface)
        {
            if (surface.Area <= 0 && surface.Geometry != null)
                surface.SetArea(surface.Geometry);

            surface.ComputeHeatGain();
        }

        private static double ComputeAverageHeatGain(FluxSurface surface)
        {
            if (surface == null || surface.Area <= 0)
                return 0d;

            return surface.HeatGain / surface.Area;
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
            if (double.IsNaN(min) || double.IsNaN(max))
                return new List<double>();

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

        protected override Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxModelSurfaceHeatGain.png");

        public override Guid ComponentGuid => new Guid("CF7C3F3E-784A-4A9B-946A-2182C105D8AC");
    }
}
