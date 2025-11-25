namespace BcaEttvCore
{
    public class EttvFenestrationConstruction : EttvConstruction
    {
        public double ScTotal { get; set; }
        public double Sc1 { get; set; }
        public double Sc2 { get; set; }

        // Ensure defaults and ScTotal set on construction
        public EttvFenestrationConstruction()
        {
            Sc1 = 1.0;
            Sc2 = 1.0;
            ScTotal = Sc1 * Sc2;
        }

        public void CalculateScTotal(double sc1, double sc2)
        {
            Sc1 = sc1;
            Sc2 = sc2;
            ScTotal = sc1 * sc2;
        }
    }
}
