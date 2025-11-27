using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class DeconstructFluxMaterialComponent : GH_Component
    {
        public DeconstructFluxMaterialComponent()
            : base("Deconstruct FluxMaterial", "DEM",
                   "Deconstruct an FluxMaterial into summary and properties",
                   "FacadeFlux", "4 :: Utilities")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxMaterial", "M", "Material to deconstruct", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "Material summary text", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Material name", GH_ParamAccess.item);
            pManager.AddNumberParameter("Thickness", "T", "Material thickness (m)", GH_ParamAccess.item);
            pManager.AddNumberParameter("ThermalConductivity", "k", "Thermal conductivity (W/m·K)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawInput = null;
            if (!DA.GetData(0, ref rawInput) || rawInput is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No FluxMaterial supplied.");
                return;
            }

            var material = UnwrapMaterial(rawInput);
            if (material is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not a valid FluxMaterial.");
                return;
            }

            FluxMaterialDeconstructor.Deconstruct(material,
                                                  out var name,
                                                  out var thickness,
                                                  out var thermalConductivity);

            var summary = $"Name: {name ?? "-"}, Thickness: {thickness:0.###}, Thermal Conductivity: {thermalConductivity:0.###}";

            DA.SetData(0, summary);
            DA.SetData(1, name);
            DA.SetData(2, thickness);
            DA.SetData(3, thermalConductivity);
        }

        private static FluxMaterial UnwrapMaterial(object value)
        {
            if (value is FluxMaterial mat)
                return mat;

            if (value is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is FluxMaterial wrapped)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is FluxMaterial scriptMat)
                    return scriptMat;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DeconstructFluxMaterial.png");

        public override Guid ComponentGuid => new Guid("E02E70EF-3E7F-45B1-B917-3EFAF199D0F7");
    }
}
