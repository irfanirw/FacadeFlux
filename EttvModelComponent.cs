using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using BcaEttvCore;

namespace BcaEttv
{
    public class EttvModelComponent : GH_Component
    {
        private static readonly HashSet<string> MainOrientationKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "N", "NORTH",
            "NE", "NORTHEAST",
            "E", "EAST",
            "SE", "SOUTHEAST",
            "S", "SOUTH",
            "SW", "SOUTHWEST",
            "W", "WEST",
            "NW", "NORTHWEST"
        };

        public EttvModelComponent()
          : base("EttvModel", "EM",
                 "Create an ETTV Model from surfaces (export deferred)",
                 "BcaEttv", "Model Setup")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("ProjectName", "PN", "Override EttvModel.ProjectName", GH_ParamAccess.item);
            pManager.AddTextParameter("Version", "V", "Override EttvModel.Version", GH_ParamAccess.item);
            pManager.AddGenericParameter("EttvSurfaces", "S", "List of EttvSurface objects", GH_ParamAccess.list);
            pManager.AddNumberParameter("AngleToNorth", "A", "Assign to EttvOrientation.AngleToNorth for all surfaces", GH_ParamAccess.item);
            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Operation status and messages", GH_ParamAccess.item);
            pManager.AddGenericParameter("EttvModel", "M", "EttvModel object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string projectName = null;
            string version = null;
            var rawSurfaces = new List<object>();
            double angleToNorth = double.NaN;

            DA.GetData(0, ref projectName);
            DA.GetData(1, ref version);
            DA.GetDataList(2, rawSurfaces);
            DA.GetData(3, ref angleToNorth);

            var surfaces = new List<EttvSurface>();
            foreach (var item in rawSurfaces)
            {
                object v = item;
                if (v is IGH_Goo goo)
                    v = (goo as GH_ObjectWrapper)?.Value ?? goo.ScriptVariable();

                if (v is EttvSurface s && v.GetType().Assembly == typeof(EttvSurface).Assembly)
                    surfaces.Add(s);
            }

            if (surfaces.Count == 0)
            {
                DA.SetData(0, "No valid EttvSurface objects provided.");
                DA.SetData(1, null);
                return;
            }

            var filteredSurfaces = surfaces.Where(IsMainOrientationSurface).ToList();
            int filteredOut = surfaces.Count - filteredSurfaces.Count;

            if (filteredSurfaces.Count == 0)
            {
                DA.SetData(0, "No valid EttvSurface objects provided after filtering invalid orientations.");
                DA.SetData(1, null);
                return;
            }

            var model = new EttvModel(filteredSurfaces);

            if (!double.IsNaN(angleToNorth))
            {
                foreach (var surface in model.Surfaces)
                {
                    surface.Orientation ??= new EttvOrientation();
                    surface.Orientation.AngleToNorth = angleToNorth;
                    surface.Orientation.AssignOrientation();
                }
            }

            if (!string.IsNullOrWhiteSpace(projectName))
                model.ProjectName = projectName;
            if (!string.IsNullOrWhiteSpace(version))
                model.Version = version;

            string status = $"EttvModel created: {model.ProjectName} v{model.Version}\nSurfaces: {model.Surfaces.Count}";
            if (filteredOut > 0)
                status += $"\nFiltered out {filteredOut} surface(s) with unsupported orientation.";

            DA.SetData(0, status);
            DA.SetData(1, model);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("BcaEttv.Icons.EttvModel.png");
#pragma warning disable CA1416
                return stream is null ? null : new System.Drawing.Bitmap(stream);
#pragma warning restore CA1416
            }
        }

        public override Guid ComponentGuid => new Guid("BBA8E894-9193-4563-8A4F-E4D197778691");

        private static bool IsMainOrientationSurface(EttvSurface surface)
        {
            if (surface?.Orientation?.Name is null)
                return false;

            var key = NormalizeOrientationKey(surface.Orientation.Name);
            return key != null && MainOrientationKeys.Contains(key);
        }

        private static string NormalizeOrientationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Replace(" ", string.Empty)
                        .Replace("-", string.Empty)
                        .ToUpperInvariant();
        }
    }
}
