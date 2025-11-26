using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class FluxSurfaceComponent : GH_Component
    {
        public FluxSurfaceComponent()
          : base("FluxSurface", "ES",
                 "Create FluxSurface objects from Mesh or Brep geometry and an FluxConstruction",
                 "FacadeFlux", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Accept both Mesh and Brep by using a generic parameter
            pManager.AddGenericParameter("Geometry", "G", "Meshes or Breps (Brep will be meshed)", GH_ParamAccess.list);
            pManager.AddGenericParameter("FluxConstruction", "C", "FluxConstruction object", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurfaces", "S", "List of FluxSurface objects", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var rawGeometry = new List<object>();
            FluxConstruction construction = null;

            DA.GetDataList(0, rawGeometry);
            DA.GetData(1, ref construction);

            var meshes = new List<Mesh>();

            foreach (var item in rawGeometry)
            {
                object g = item;
                if (g is IGH_Goo goo)
                    g = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                switch (g)
                {
                    case Mesh m when m != null && m.IsValid:
                        meshes.Add(m);
                        break;

                    case Brep b when b != null && b.IsValid:
                        meshes.AddRange(MeshFromBrep(b));
                        break;

                    case GH_Mesh ghm when ghm.Value != null && ghm.Value.IsValid:
                        meshes.Add(ghm.Value);
                        break;

                    case GH_Brep ghb when ghb.Value != null && ghb.Value.IsValid:
                        meshes.AddRange(MeshFromBrep(ghb.Value));
                        break;
                }
            }

            if (meshes.Count == 0 || construction == null)
            {
                DA.SetDataList(0, new List<FluxSurface>());
                return;
            }

            var surfaces = new List<FluxSurface>();
            int idCounter = 1;

            foreach (var mesh in meshes)
            {
                var surface = new FluxSurface
                {
                    Id = idCounter,
                    Name = $"Surf_{idCounter}",
                    Geometry = mesh,
                    Construction = construction
                };

                surface.Orientation = ComputeOrientation(mesh);
                surface.SetArea(mesh);

                surfaces.Add(surface);
                idCounter++;
            }

            DA.SetDataList(0, surfaces);
        }

        private static IEnumerable<Mesh> MeshFromBrep(Brep brep)
        {
            var meshes = Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh);
            if (meshes == null) yield break;

            foreach (var mesh in meshes)
            {
                if (mesh != null && mesh.IsValid)
                    yield return mesh;
            }
        }

        private static FluxOrientation ComputeOrientation(Mesh mesh)
        {
            if (mesh == null || !mesh.IsValid)
                return null;

            if (mesh.Normals.Count == 0)
                mesh.Normals.ComputeNormals();

            var avg = Vector3d.Zero;
            for (int i = 0; i < mesh.Normals.Count; i++)
                avg += new Vector3d(mesh.Normals[i]);

            if (!avg.Unitize())
            {
                mesh.FaceNormals.ComputeFaceNormals();
                for (int i = 0; i < mesh.FaceNormals.Count; i++)
                    avg += new Vector3d(mesh.FaceNormals[i]);
                if (!avg.Unitize())
                    return null;
            }

            var name = ResolveOrientationName(avg);

            return new FluxOrientation
            {
                Normal = avg,
                Name = name,
                Id = name
            };
        }

        private static string ResolveOrientationName(Vector3d normal)
        {
            if (!normal.IsValid || normal.IsTiny())
                return "Undefined";

            if (!normal.Unitize())
                return "Undefined";

            const double verticalThreshold = 0.7;
            if (Math.Abs(normal.Z) > verticalThreshold)
                return normal.Z > 0 ? "Roof" : "Floor";

            double angle = Math.Atan2(normal.Y, normal.X);
            if (angle < 0) angle += 2 * Math.PI;
            double deg = angle * 180.0 / Math.PI;

            return deg switch
            {
                >= 22.5 and < 67.5 => "NorthEast",
                >= 67.5 and < 112.5 => "North",
                >= 112.5 and < 157.5 => "NorthWest",
                >= 157.5 and < 202.5 => "West",
                >= 202.5 and < 247.5 => "SouthWest",
                >= 247.5 and < 292.5 => "South",
                >= 292.5 and < 337.5 => "SouthEast",
                _ => "East"
            };
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("FacadeFlux.Icons.FluxSurface.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("e3b1c9d2-4f6a-4b2e-9d1f-0a1b2c3d4e5f");
    }
}