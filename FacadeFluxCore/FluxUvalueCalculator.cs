using System;
using System.Collections.Generic;
using FacadeFluxCore;

namespace FacadeFluxCore
{
    public static class FluxUvalueCalculator
    {
        // Assumes Thickness is in millimetres; converts to meters internally.
        // Uses standard internal/external surface resistances (Rsi=0.13, Rse=0.04 m²K/W).
        public static double ComputeUValue(IEnumerable<FluxMaterial> materials)
        {
            const double Rsi = 0.12; // internal surface film (BCA Singapore)
            const double Rse = 0.04; // external surface film (BCA Singapore)

            if (materials == null)
                return 0.0;

            double layerResistance = 0.0;

            foreach (var mat in materials)
            {
                if (mat == null)
                    continue;

                var k = mat.ThermalConductivity;
                var t = mat.Thickness;

                if (k <= 0 || t <= 0)
                    continue;

                layerResistance += t / k;
            }

            var totalResistance = Rsi + layerResistance + Rse;
            return totalResistance <= 0 ? 0.0 : 1.0 / totalResistance;
        }
    }
}
