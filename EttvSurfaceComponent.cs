using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BcaEttvCore;

namespace BcaEttv
{
    public class EttvSurfaceComponent : GH_Component
    {
        public EttvSurfaceComponent()
          : base("EttvSurface", "ES",
                 "Create EttvSurface objects from Mesh or Brep geometry and an EttvConstruction",
                 "BcaEttv", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Accept both Mesh and Brep by using a generic parameter
            pManager.AddGenericParameter("Geometry", "G", "Meshes or Breps (Brep will be meshed)", GH_ParamAccess.list);
            pManager.AddGenericParameter("EttvConstruction", "C", "EttvConstruction object", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("EttvSurfaces", "S", "List of EttvSurface objects", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var rawGeometry = new List<object>();
            EttvConstruction construction = null;

            DA.GetDataList(0, rawGeometry);
            DA.GetData(1, ref construction);

            var meshes = new List<Mesh>();

            foreach (var item in rawGeometry)
            {
                object g = item;
                if (g is IGH_Goo goo)
                    g = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                switch (g)
                {
                    case Mesh m:
                        if (m != null && m.IsValid) meshes.Add(m);
                        break;
                    case Brep b:
                        if (b != null && b.IsValid)
                        {
                            var brepMeshes = Mesh.CreateFromBrep(b, MeshingParameters.FastRenderMesh);
                            if (brepMeshes != null)
                            {
                                foreach (var bm in brepMeshes)
                                {
                                    if (bm != null && bm.IsValid)
                                        meshes.Add(bm);
                                }
                            }
                        }
                        break;
                    // Allow Rhino geometry goo types
                    case GH_Mesh ghm when ghm.Value != null && ghm.Value.IsValid:
                        meshes.Add(ghm.Value);
                        break;
                    case GH_Brep ghb when ghb.Value != null && ghb.Value.IsValid:
                        var brepMeshes2 = Mesh.CreateFromBrep(ghb.Value, MeshingParameters.FastRenderMesh);
                        if (brepMeshes2 != null)
                        {
                            foreach (var bm in brepMeshes2)
                            {
                                if (bm != null && bm.IsValid)
                                    meshes.Add(bm);
                            }
                        }
                        break;
                }
            }

            // If nothing provided, output empty list quietly
            if (meshes.Count == 0 || construction == null)
            {
                DA.SetDataList(0, new List<EttvSurface>());
                return;
            }

            var surfaces = new List<EttvSurface>();
            int idCounter = 1;
            foreach (var mesh in meshes)
            {
                var s = new EttvSurface
                {
                    Id = idCounter++,
                    Name = $"Surf_{idCounter - 1}",
                    Geometry = mesh,
                    Construction = construction // setter in EttvSurface sets Type based on construction subclass
                };
                surfaces.Add(s);
            }

            DA.SetDataList(0, surfaces);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.EttvSurface.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("e3b1c9d2-4f6a-4b2e-9d1f-0a1b2c3d4e5f");
    }
}