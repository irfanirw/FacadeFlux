using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace FacadeFlux
{
    public class FluxMaterialDatabaseComponent : GH_Component
    {
        private static readonly Dictionary<string, double> MaterialDatabase = new Dictionary<string, double>
        {
            { "Asphalt, roofing", 1.226 },
            { "Bitumen", 1.298 },
            { "Brick (dry, covered by plaster/tiles)", 0.807 },
            { "Brick (common brickwall directly exposed)", 1.154 },
            { "Concrete", 1.442 },
            { "Concrete, lightweight (density 960 kg/m³)", 0.303 },
            { "Concrete, lightweight (density 1120 kg/m³)", 0.346 },
            { "Concrete, lightweight (density 1280 kg/m³)", 0.476 },
            { "Cork board", 0.042 },
            { "Fibre board", 0.052 },
            { "Glass, sheet", 1.053 },
            { "Glass wool, mat or quilt (dry)", 0.035 },
            { "Gypsum plaster board", 0.170 },
            { "Hard board, standard", 0.216 },
            { "Hard board, medium", 0.123 },
            { "Aluminium alloy, typical", 211.0 },
            { "Copper, commercial", 385.0 },
            { "Steel", 47.6 },
            { "Mineral wool, felt (32 kg/m³)", 0.035 },
            { "Mineral wool, felt (104 kg/m³)", 0.032 },
            { "Plaster: gypsum", 0.370 },
            { "Plaster: perlite", 0.115 },
            { "Plaster: sand/cement", 0.533 },
            { "Plaster: vermiculite (640 kg/m³)", 0.202 },
            { "Plaster: vermiculite (960 kg/m³)", 0.303 },
            { "Polystyrene, expanded", 0.035 },
            { "Polyurethane, foam", 0.024 },
            { "PVC flooring", 0.713 },
            { "Soil, loosely packed", 0.375 },
            { "Stone, tile: sandstone", 1.298 },
            { "Stone, tile: granite", 2.927 },
            { "Stone, tile: marble/terrazzo/ceramic/mosaic", 1.298 },
            { "Tile, roof", 0.836 },
            { "Timber across grain, softwood", 0.125 },
            { "Timber hardwood", 0.138 },
            { "Timber plywood", 0.138 },
            { "Vermiculite, loose granules (80 kg/m³)", 0.065 },
            { "Oxygen gas", 0.0263 },
            { "Argon gas", 0.0177 }
        };

        private string _selectedMaterial = MaterialDatabase.Keys.First();

        public FluxMaterialDatabaseComponent()
          : base("FluxMaterialDatabase", "EMD",
                 "Select building material and get its thermal conductivity (k-value)",
                 "FacadeFlux", "Geometry & Inputs")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // No inputs - dropdown only
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("MaterialName", "N", "Selected material name", GH_ParamAccess.item);
            pManager.AddNumberParameter("kValue", "k", "Thermal conductivity (W/m·K)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (MaterialDatabase.TryGetValue(_selectedMaterial, out double kValue))
            {
                DA.SetData(0, _selectedMaterial);
                DA.SetData(1, kValue);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Material not found in database.");
                DA.SetData(0, string.Empty);
                DA.SetData(1, 0.0);
            }
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);

            Menu_AppendSeparator(menu);

            foreach (var material in MaterialDatabase.Keys.OrderBy(k => k))
            {
                Menu_AppendItem(menu, material, OnMaterialSelected, true, material == _selectedMaterial)
                    .Tag = material;
            }
        }

        private void OnMaterialSelected(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is string materialName)
            {
                _selectedMaterial = materialName;
                ExpireSolution(true);
            }
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetString("SelectedMaterial", _selectedMaterial);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if (reader.ItemExists("SelectedMaterial"))
                _selectedMaterial = reader.GetString("SelectedMaterial");
            return base.Read(reader);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("FacadeFlux.Icons.FluxMaterialDatabase.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    }
}
