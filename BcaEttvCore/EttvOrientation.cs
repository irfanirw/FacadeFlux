using Rhino.Geometry;
using System;

namespace BcaEttvCore
{
    public class EttvOrientation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Vector3d Normal { get; set; }
        public double AngleToNorth { get; set; } = double.NaN;
        public double Cf { get; set; }

        public EttvOrientation()
        {
            Id = string.Empty;
            Name = string.Empty;
            Normal = Vector3d.ZAxis;
        }

        public void AssignOrientation()
        {
            if (!Normal.IsValid || Normal.Length == 0)
            {
                Id = "Unknown";
                Name = "Unknown";
                return;
            }

            var absX = Math.Abs(Normal.X);
            var absY = Math.Abs(Normal.Y);
            var absZ = Math.Abs(Normal.Z);

            if (absZ > absX && absZ > absY)
            {
                if (Normal.Z > 0)
                {
                    Id = "R";
                    Name = "Roof";
                }
                else
                {
                    Id = "F";
                    Name = "Floor";
                }

                SetCf();
                return;
            }

            double computedAngle = Math.Atan2(Normal.X, Normal.Y) * 180.0 / Math.PI;
            double angle = NormalizeAngle(double.IsNaN(AngleToNorth) ? computedAngle : AngleToNorth);

            if (angle >= 337.5 || angle < 22.5)
            {
                Id = "N";
                Name = "North";
            }
            else if (angle >= 22.5 && angle < 67.5)
            {
                Id = "NE";
                Name = "NorthEast";
            }
            else if (angle >= 67.5 && angle < 112.5)
            {
                Id = "E";
                Name = "East";
            }
            else if (angle >= 112.5 && angle < 157.5)
            {
                Id = "SE";
                Name = "SouthEast";
            }
            else if (angle >= 157.5 && angle < 202.5)
            {
                Id = "S";
                Name = "South";
            }
            else if (angle >= 202.5 && angle < 247.5)
            {
                Id = "SW";
                Name = "SouthWest";
            }
            else if (angle >= 247.5 && angle < 292.5)
            {
                Id = "W";
                Name = "West";
            }
            else
            {
                Id = "NW";
                Name = "NorthWest";
            }

            SetCf();
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % 360.0;
            if (normalized < 0)
                normalized += 360.0;
            return normalized;
        }

        public void SetCf()
        {
            Cf = Id switch
            {
                "N" => 0.80,
                "NE" => 0.97,
                "E" => 1.13,
                "SE" => 0.98,
                "S" => 0.83,
                "SW" => 1.06,
                "W" => 1.23,
                "NW" => 1.03,
                _ => 1.00
            };
        }
    }
}