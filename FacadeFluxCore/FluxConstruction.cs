using System.Collections.Generic;

namespace FacadeFluxCore
{
    public class FluxConstruction
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<FluxMaterial> FluxMaterials { get; set; }
        public double Uvalue { get; set; }

        public FluxConstruction()
        {
            Id = string.Empty;
            Name = string.Empty;
            FluxMaterials = new List<FluxMaterial>();
            Uvalue = 0.0;
        }

        public void CalculateUvalue(List<FluxMaterial> materials)
        {
            FluxMaterials = materials ?? new List<FluxMaterial>();
            Uvalue = FluxUvalueCalculator.ComputeUValue(FluxMaterials);
        }
    }
}
