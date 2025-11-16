using Rhino.Geometry;

namespace BcaEttvCore
{
    public class EttvSurface
    {
        private EttvConstruction _construction;

        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; private set; }
        public Mesh Geometry { get; set; }
        public double Area { get; private set; }
        public EttvOrientation Orientation { get; set; }

        public EttvConstruction Construction
        {
            get => _construction;
            set
            {
                _construction = value;
                // Auto-set Type based on construction type
                if (value is EttvOpaqueConstruction)
                    Type = "Wall";
                else if (value is EttvFenestrationConstruction)
                    Type = "Fenestration";
                else
                    Type = "Unknown";
            }
        }

        public EttvSurface()
        {
            Name = string.Empty;
            Type = "Unknown";
        }

        /// <summary>
        /// Calculate and assign Orientation based on the average face normal of the Geometry mesh.
        /// </summary>
        public void CalculateOrientation()
        {
            if (Geometry == null || !Geometry.IsValid || Geometry.Faces.Count == 0)
            {
                Orientation = new EttvOrientation
                {
                    Id = "Unknown",
                    Name = "Unknown",
                    Normal = Vector3d.ZAxis
                };
                return;
            }

            // Compute average face normal
            Vector3d avgNormal = Vector3d.Zero;
            int validFaces = 0;

            for (int i = 0; i < Geometry.Faces.Count; i++)
            {
                var face = Geometry.Faces[i];
                if (!face.IsValid(Geometry.Vertices.Count)) continue;

                var a = Geometry.Vertices[face.A];
                var b = Geometry.Vertices[face.B];
                var c = Geometry.Vertices[face.C];

                var v1 = new Vector3d(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
                var v2 = new Vector3d(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
                var normal = Vector3d.CrossProduct(v1, v2);

                if (normal.Length > 0)
                {
                    normal.Unitize();
                    avgNormal += normal;
                    validFaces++;
                }
            }

            if (validFaces > 0)
            {
                avgNormal /= validFaces;
                avgNormal.Unitize();
            }
            else
            {
                avgNormal = Vector3d.ZAxis; // fallback
            }

            // Determine orientation name based on dominant direction
            string orientationName = GetOrientationName(avgNormal);

            Orientation = new EttvOrientation
            {
                Id = orientationName,
                Name = orientationName,
                Normal = avgNormal
            };
        }

        private static string GetOrientationName(Vector3d normal)
        {
            // Determine primary direction based on normal vector
            var absX = System.Math.Abs(normal.X);
            var absY = System.Math.Abs(normal.Y);
            var absZ = System.Math.Abs(normal.Z);

            if (absZ > absX && absZ > absY)
            {
                return normal.Z > 0 ? "Roof" : "Floor";
            }
            else
            {
                // Horizontal surface - determine cardinal direction
                double angle = System.Math.Atan2(normal.Y, normal.X) * 180.0 / System.Math.PI;
                if (angle < 0) angle += 360;

                if (angle >= 337.5 || angle < 22.5) return "East";
                if (angle >= 22.5 && angle < 67.5) return "NorthEast";
                if (angle >= 67.5 && angle < 112.5) return "North";
                if (angle >= 112.5 && angle < 157.5) return "NorthWest";
                if (angle >= 157.5 && angle < 202.5) return "West";
                if (angle >= 202.5 && angle < 247.5) return "SouthWest";
                if (angle >= 247.5 && angle < 292.5) return "South";
                return "SouthEast";
            }
        }

        public void SetArea(GeometryBase geometry)
        {
            if (geometry is null)
            {
                Area = 0d;
                return;
            }

            Area = geometry switch
            {
                Mesh mesh => AreaMassProperties.Compute(mesh)?.Area ?? 0d,
                Brep brep => AreaMassProperties.Compute(brep)?.Area ?? 0d,
                Surface surface => AreaMassProperties.Compute(surface)?.Area ?? 0d,
                _ => 0d
            };
        }
    }
}