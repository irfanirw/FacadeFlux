using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using BcaEttvCore;

namespace BcaEttv
{
    public class Sc2GeneratorComponent : GH_Component
    {
        public Sc2GeneratorComponent()
          : base("Sc2Generator", "SC2",
                 "Generate SC2 value for ETTV calculation",
                 "BcaEttv", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvSurface", "S", "Surface providing construction metadata", GH_ParamAccess.item);
            pManager.AddNumberParameter("ShadingProjection", "P", "Horizontal shading projection length (m)", GH_ParamAccess.item);
            pManager.AddNumberParameter("GlazingHeight", "H", "Glazing/Opening height (m)", GH_ParamAccess.item);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvSurface", "S", "Surface with updated Construction.Sc2 value", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object rawSurface = null;
            double shadingProjection = double.NaN;
            double glazingHeight = double.NaN;

            DA.GetData(0, ref rawSurface);
            DA.GetData(1, ref shadingProjection);
            DA.GetData(2, ref glazingHeight);

            var surface = UnwrapSurface(rawSurface);
            if (surface is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A valid EttvSurface is required.");
                DA.SetData(0, null);
                return;
            }

            var orientation = surface.Orientation ?? new EttvOrientation { Name = "North" };
            double projectionValue = double.IsNaN(shadingProjection) ? 0.0 : shadingProjection;
            double heightValue = double.IsNaN(glazingHeight) || glazingHeight <= 0 ? 1.0 : glazingHeight;

            double sc2 = HorizontalSc2Calculator.Calculate(projectionValue, heightValue, orientation);
            AssignSc2(surface.Construction, sc2);

            DA.SetData(0, surface);
        }

        private static void AssignSc2(EttvConstruction construction, double sc2)
        {
            if (construction is EttvFenestrationConstruction fen)
            {
                fen.Sc2 = sc2;
                return;
            }

            var prop = construction?.GetType().GetProperty("Sc2");
            if (prop != null && prop.CanWrite)
                prop.SetValue(construction, sc2);
        }

        private static EttvSurface UnwrapSurface(object raw)
        {
            if (raw is EttvSurface surface)
                return surface;

            if (raw is IGH_Goo goo)
            {
                if (goo is GH_ObjectWrapper wrapper && wrapper.Value is EttvSurface wrapped)
                    return wrapped;

                var scriptValue = goo.ScriptVariable();
                if (scriptValue is EttvSurface scriptSurface)
                    return scriptSurface;
            }

            return null;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.Sc2Generator.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("d4e5f6a7-b8c9-4d0e-a1b2-c3d4e5f6a7b8");
    }
}