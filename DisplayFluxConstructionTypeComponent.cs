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
            : base("Display Flux Construction Type", "DFT",
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
            pManager.AddTextParameter("Legend", "L", "Legend text describing counts", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _previewMeshes.Clear();

            if (!TryGetModel(DA, out var model))
            {
                DA.SetDataList(0, null);
                DA.SetData(1, "No model supplied.");
                return;
            }

            var surfaces = model.Surfaces?.Where(s => s != null).ToList() ?? new List<FluxSurface>();
            if (surfaces.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Model contains no FluxSurface data.");
                DA.SetDataList(0, null);
                DA.SetData(1, "Model contains no FluxSurface data.");
                return;
            }

            var meshesOut = new List<Mesh>();
            int fenCount = 0;
            int opaqueCount = 0;

            foreach (var surface in surfaces)
            {
                var mesh = DuplicateMesh(surface);
                if (mesh == null)
                    continue;

                var color = surface.Construction is FluxFenestrationConstruction
                    ? Color.FromArgb(173, 216, 230) // light blue
                    : Color.Orange;

                ApplyColor(mesh, color);

                meshesOut.Add(mesh);
                _previewMeshes.Add((mesh, color));

                if (surface.Construction is FluxFenestrationConstruction)
                    fenCount++;
                else
                    opaqueCount++;
            }

            if (meshesOut.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid geometry could be drawn from the provided model.");
                DA.SetDataList(0, null);
                DA.SetData(1, "No valid geometry generated.");
                return;
            }

            var legend = $"Fenestration surfaces: {fenCount}, Opaque surfaces: {opaqueCount}";
            DA.SetDataList(0, meshesOut);
            DA.SetData(1, legend);
        }

        private static bool TryGetModel(IGH_DataAccess DA, out FluxModel model)
        {
            model = null;
            object raw = null;
            if (!DA.GetData(0, ref raw) || raw == null)
                return false;

            if (raw is FluxModel direct)
            {
                model = direct;
                return true;
            }

            if (raw is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is FluxModel wrapped)
                {
                    model = wrapped;
                    return true;
                }

                var script = goo.ScriptVariable();
                if (script is FluxModel scriptModel)
                {
                    model = scriptModel;
                    return true;
                }
            }

            return false;
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
            var hasVertexColors = mesh.VertexColors != null && mesh.VertexColors.Count > 0;
            if (!hasVertexColors)
                mesh.VertexColors.CreateMonotoneMesh(color);
            else
            {
                for (int i = 0; i < mesh.VertexColors.Count; i++)
                    mesh.VertexColors[i] = color;
            }
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
    }
}
