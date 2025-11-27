using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using FacadeFluxCore;

namespace FacadeFlux
{
    public class DeconstructFluxSurfaceComponent : GH_Component
    {
        public DeconstructFluxSurfaceComponent()
          : base("Deconstruct FluxSurface", "DES",
                 "Deconstruct a list of FluxSurface objects",
                 "FacadeFlux", "4 :: Utilities")
        {
            // Disable preview by default
            this.Hidden = true;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FluxSurfaces", "S", "List of FacadeFluxCore.FluxSurface", GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Per-surface summary text (one item per FluxSurface)", GH_ParamAccess.list); // 0
            pManager.AddGenericParameter("Geometries", "G", "Surface meshes", GH_ParamAccess.list);      // 1
            pManager.AddNumberParameter("Areas", "A", "Areas per geometry (m²)", GH_ParamAccess.list);   // 2
            pManager.AddTextParameter("Orientations", "O", "List of FluxOrientation.Name", GH_ParamAccess.list); // 3
            pManager.AddGenericParameter("FluxConstructions", "C", "List of FluxConstruction", GH_ParamAccess.list); // 4
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var rawItems = new List<object>();
            DA.GetDataList(0, rawItems);

            if (rawItems.Count == 0)
            {
                DA.SetDataList(0, new List<string>());
                DA.SetDataList(1, new List<Mesh>());
                DA.SetDataList(2, new List<double>());
                DA.SetDataList(3, new List<string>());
                DA.SetDataList(4, new List<FluxConstruction>());
                return;
            }

            var surfaces = new List<FluxSurface>();
            foreach (var item in rawItems)
            {
                object v = item;
                if (v is IGH_Goo goo)
                    v = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                if (v is FluxSurface s && v.GetType().Assembly == typeof(FluxSurface).Assembly)
                    surfaces.Add(s);
            }

            if (surfaces.Count == 0)
            {
                DA.SetDataList(0, new List<string>());
                DA.SetDataList(1, new List<Mesh>());
                DA.SetDataList(2, new List<double>());
                DA.SetDataList(3, new List<string>());
                DA.SetDataList(4, new List<FluxConstruction>());
                return;
            }

            var meshes = new List<Mesh>();
            var areas = new List<double>();
            var orientationNames = new List<string>();
            var constructions = new List<FluxConstruction>();
            var texts = new List<string>();

            foreach (var s in surfaces)
            {
                // Calculate orientation if not already set
                if (s.Orientation == null && s.Geometry != null)
                {
                    s.CalculateOrientation();
                }

                // Text block per surface
                texts.Add(FluxSurfaceDeconstructor.ToText(s).TrimEnd());

                // Geometry
                if (s.Geometry is Mesh m && m.IsValid)
                {
                    meshes.Add(m);
                    double area = 0.0;
                    var amp = AreaMassProperties.Compute(m);
                    if (amp != null)
                    {
                        // Convert to m² from document units
                        var doc = Rhino.RhinoDoc.ActiveDoc;
                        if (doc != null)
                        {
                            double scale = Rhino.RhinoMath.UnitScale(doc.ModelUnitSystem, Rhino.UnitSystem.Meters);
                            area = amp.Area * scale * scale; // area scales by square of linear scale
                        }
                        else
                        {
                            area = amp.Area; // fallback: assume meters
                        }
                    }
                    areas.Add(area);
                }
                else
                {
                    areas.Add(0.0);
                }

                // Orientation
                orientationNames.Add((s.Orientation ?? new FluxOrientation()).Name ?? string.Empty);

                // Construction
                if (s.Construction != null)
                    constructions.Add(s.Construction);
            }

            DA.SetDataList(0, texts);
            DA.SetDataList(1, meshes);
            DA.SetDataList(2, areas);
            DA.SetDataList(3, orientationNames);
            DA.SetDataList(4, constructions);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => IconHelper.LoadIcon("FacadeFlux.Icons.DeconstructFluxSurface.png");

        public override Guid ComponentGuid => new Guid("d2a7f1b4-6c3e-4a09-9b11-7e3d5c2f1a8b");
    }
}
