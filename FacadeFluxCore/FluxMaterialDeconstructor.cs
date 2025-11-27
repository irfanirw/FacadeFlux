using System;

namespace FacadeFluxCore
{
    public static class FluxMaterialDeconstructor
    {
        public static void Deconstruct(FluxMaterial material,
                                       out string name,
                                       out double thickness,
                                       out double thermalConductivity)
        {
            if (material is null)
                throw new ArgumentNullException(nameof(material));

            name = material.Name;
            thickness = material.Thickness;
            thermalConductivity = material.ThermalConductivity;
        }
    }
}
