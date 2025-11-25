using System;
using System.Text;
using System.Reflection;

namespace BcaEttvCore
{
    public static class EttvConstructionDeconstructor
    {
        // Returns a human-readable text describing the construction and its materials.
        public static string ToText(EttvConstruction construction)
        {
            if (construction == null) return "EttvConstruction: null";

            var sb = new StringBuilder();

            string type =
                construction is EttvFenestrationConstruction ? "Fenestration" :
                construction is EttvOpaqueConstruction ? "Opaque" :
                "Unknown";

            sb.AppendLine("EttvConstruction");
            sb.AppendLine($"Id: {construction.Id}");
            sb.AppendLine($"Name: {construction.Name}");
            sb.AppendLine($"Type: {type}");
            sb.AppendLine($"Uvalue: {construction.Uvalue:0.###} W/m²K");

            // SC values (use reflection, since property names may vary: Sc/SC/SC1/SC2)
            if (TryReadDouble(construction, "Sc1", out var sc1))
                sb.AppendLine($"Sc1: {sc1:0.###}");
            if (TryReadDouble(construction, "Sc2", out var sc2))
                sb.AppendLine($"Sc2: {sc2:0.###}");
            if (TryReadDouble(construction, "ScTotal", out var scTotal))
                sb.AppendLine($"ScTotal: {scTotal:0.###}");
            else if (TryReadDouble(construction, "Sc", out var sc) || TryReadDouble(construction, "Sc", out sc))
                sb.AppendLine($"Sc: {sc:0.###}");

            // Materials list
            if (construction.EttvMaterials != null && construction.EttvMaterials.Count > 0)
            {
                sb.AppendLine("Materials:");
                for (int i = 0; i < construction.EttvMaterials.Count; i++)
                {
                    var m = construction.EttvMaterials[i];
                    if (m == null) continue;
                    sb.AppendLine($"  {i + 1}. {m.Name} | k={m.ThermalConductivity:0.###} | t={m.Thickness:0.###}");
                }
            }
            else
            {
                sb.AppendLine("Materials: (none)");
            }

            return sb.ToString();
        }

        private static bool TryReadDouble(object obj, string propName, out double value)
        {
            value = 0.0;
            if (obj == null) return false;

            var pi = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (pi == null || !pi.CanRead) return false;

            var v = pi.GetValue(obj);
            if (v == null) return false;

            try
            {
                value = Convert.ToDouble(v);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}