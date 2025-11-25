using System.Text;
using Rhino.Geometry;

namespace BcaEttvCore
{
    public static class EttvSurfaceDeconstructor
    {
        // Returns a readable text summary of one EttvSurface.
        public static string ToText(EttvSurface surface)
        {
            if (surface == null) return "EttvSurface: null";

            var sb = new StringBuilder();
            sb.AppendLine("EttvSurface");
            sb.AppendLine($"Id: {surface.Id}");
            sb.AppendLine($"Name: {surface.Name}");
            sb.AppendLine($"Type: {surface.Type}");

            // Geometry summary
            if (surface.Geometry is Mesh m && m.IsValid)
            {
                sb.AppendLine($"Mesh: V={m.Vertices.Count}, F={m.Faces.Count}");
                sb.AppendLine($"Area (approx): {ApproxMeshArea(m):0.###} m²");
            }
            else
            {
                sb.AppendLine("Mesh: (none)");
            }

            // Orientation
            if (surface.Orientation != null)
            {
                sb.AppendLine($"Orientation: {surface.Orientation.Name}");
            }
            else
            {
                sb.AppendLine("Orientation: (none)");
            }

            return sb.ToString();
        }

        // Quick area approximation (triangulate faces).
        private static double ApproxMeshArea(Mesh mesh)
        {
            if (mesh == null || !mesh.IsValid) return 0.0;

            // Fast, reliable area from Rhino
            var amp = AreaMassProperties.Compute(mesh);
            if (amp != null)
                return amp.Area;

            // Fallback manual triangulation
            double area = 0.0;
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                var f = mesh.Faces[i];
                var a = mesh.Vertices[f.A];
                var b = mesh.Vertices[f.B];
                var c = mesh.Vertices[f.C];
                area += TriangleArea(a, b, c);
                if (f.IsQuad)
                {
                    var d = mesh.Vertices[f.D];
                    area += TriangleArea(a, c, d);
                }
            }
            return area;
        }

        private static double TriangleArea(Point3f a, Point3f b, Point3f c)
        {
            var v1 = new Vector3d(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var v2 = new Vector3d(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            return 0.5 * Vector3d.CrossProduct(v1, v2).Length;
        }
    }
}