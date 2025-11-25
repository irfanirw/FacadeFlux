using System;

namespace BcaEttvCore
{
    public static class EttvMaterialDeconstructor
    {
        public static void Deconstruct(EttvMaterial material,
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
