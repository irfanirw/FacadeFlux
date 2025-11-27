using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types; // added for GH_ObjectWrapper / IGH_Goo
using FacadeFluxCore;
using System.Reflection;

namespace FacadeFlux
{
    public class FluxUValueComponent : GH_Component
    {
        public FluxUValueComponent()
          : base("FluxUValueCalculator", "FU",
                 "Calculate U-value from a list of FluxMaterials",
                 "FacadeFlux", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxMaterials", "M", "List of FacadeFluxCore.FluxMaterial objects", GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Uvalue", "U", "Calculated U-value (W/m²K)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var raw = new List<object>();
            if (!DA.GetDataList(0, raw) || raw.Count == 0)
            {
                DA.SetData(0, 0.0);
                return;
            }

            var mats = new List<FluxMaterial>();
            foreach (var it in raw)
            {
                object v = it;
                if (v is IGH_Goo goo)
                    v = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                // direct core type
                if (v is FluxMaterial m)
                {
                    mats.Add(m);
                    continue;
                }

                // same type name from another load context; clone into core type
                var t = v?.GetType();
                if (t?.FullName == "FacadeFluxCore.FluxMaterial")
                {
                    var m2 = new FluxMaterial
                    {
                        Name = t.GetProperty("Name")?.GetValue(v)?.ToString() ?? "Unnamed",
                        ThermalConductivity = ToDouble(t.GetProperty("ThermalConductivity")?.GetValue(v)),
                        Thickness = ToDouble(t.GetProperty("Thickness")?.GetValue(v))
                    };
                    mats.Add(m2);
                }
            }

            if (mats.Count == 0)
            {
                DA.SetData(0, 0.0);
                return;
            }

            double u = FluxUvalueCalculator.ComputeUValue(mats);
            DA.SetData(0, u);
        }

        private static double ToDouble(object o) => o == null ? 0.0 : Convert.ToDouble(o);

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.FluxUValue.png");

        public override Guid ComponentGuid => new Guid("c5e2d1f8-9a3b-4d7e-b6c5-1f2a3b4c5d6e");
    }
}
