using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FacadeFluxCore
{
    public class FluxConstruction
    {
        private string _name;

        public string Id { get; set; }
        public string Name
        {
            get => _name;
            set => _name = ToTitleCase(value);
        }
        public List<FluxMaterial> FluxMaterials { get; set; }
        public double Uvalue { get; set; }

        public FluxConstruction()
        {
            Id = string.Empty;
            _name = string.Empty;
            FluxMaterials = new List<FluxMaterial>();
            Uvalue = 0.0;
        }

        public void CalculateUvalue(List<FluxMaterial> materials)
        {
            FluxMaterials = materials ?? new List<FluxMaterial>();
            Uvalue = FluxUvalueCalculator.ComputeUValue(FluxMaterials);
        }

        private static string ToTitleCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var culture = CultureInfo.CurrentCulture;
            var textInfo = culture.TextInfo;
            var trimmed = value.Trim();

            var sb = new StringBuilder(trimmed.Length);
            int i = 0;
            while (i < trimmed.Length)
            {
                if (char.IsWhiteSpace(trimmed[i]))
                {
                    sb.Append(trimmed[i]);
                    i++;
                    continue;
                }

                int start = i;
                while (i < trimmed.Length && !char.IsWhiteSpace(trimmed[i]))
                    i++;

                var word = trimmed.Substring(start, i - start);
                if (word.Length > 0 && char.IsDigit(word[0]))
                {
                    sb.Append(word);
                }
                else
                {
                    var lower = word.ToLower(culture);
                    sb.Append(textInfo.ToTitleCase(lower));
                }
            }

            return sb.ToString();
        }
    }
}
