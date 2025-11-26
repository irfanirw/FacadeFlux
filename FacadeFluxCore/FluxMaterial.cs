namespace FacadeFluxCore
{
    public class FluxMaterial
    {
        public string Name { get; set; }
        public double ThermalConductivity { get; set; }
        public double Thickness { get; set; }

        public FluxMaterial()
        {
            Name = "DefaultFluxMat";
            ThermalConductivity = 1.0;
            Thickness = 10;
        }
    }
}
