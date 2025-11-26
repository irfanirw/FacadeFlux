# FacadeFlux

FacadeFlux is a Grasshopper toolkit for BCA Singapore facade compliance workflows (ETTV and RETV). It wraps the core calculation library and a set of components that build facade models, calculate values, visualize construction types, and export lightweight reports.

## Overview
- Build facade materials and constructions (opaque and fenestration).
- Generate FluxSurface geometry, orientations, and full FluxModel containers.
- Compute ETTV/RETV via FluxModel calculators.
- Visualize construction types directly in the Rhino viewport.
- Export HTML summaries for sharing results.

## Installation (from source)
1. Clone this repository.
2. Build `FacadeFlux.csproj` in Release for your target (e.g., `dotnet build -c Release -f net7.0-windows` on Windows or `-f net7.0` for cross-platform Grasshopper 8).
3. Copy the generated `FacadeFlux.gha` from `bin/Release/<tfm>/` into your Grasshopper Libraries folder:
   - Windows: `%AppData%\\Grasshopper\\Libraries`
   - macOS: `~/Library/Application Support/McNeel/Rhinoceros/MacPlugIns/Grasshopper/Libraries`
4. Restart Rhino/Grasshopper and look for the FacadeFlux tab.

## Quick start (code)
You can also consume the core library directly:

```csharp
using FacadeFluxCore;

var concrete = new FluxMaterial { Name = "Concrete", Thickness = 0.2, ThermalConductivity = 1.4 };
var opaque = new FluxOpaqueConstruction { Name = "Wall", FluxMaterials = new() { concrete } };

var surface = new FluxSurface
{
    Name = "North Facade",
    Construction = opaque,
    Orientation = new FluxOrientation { Name = "North" }
};

var model = new FluxModel(new() { surface });
var etvvResult = FluxCalculator.CalculateEttv(model);
```

## Component highlights
- FluxMaterial / FluxMaterialDatabase: define and pick materials.
- FluxOpaqueConstruction / FluxFenestrationConstruction: assemble constructions and U-values.
- FluxSurface / FluxModel: build facade geometry collections.
- ComputeETTV / ComputeRETV: run compliance calculations.
- Display Flux Construction Type: viewport coloring by construction type.
- Export Flux HTML: one-click HTML summary.

## Project layout
- `FacadeFluxCore/` – calculation library (ETTV/RETV, materials, constructions, surfaces, model utilities).
- `*.cs` in root – Grasshopper component implementations and icons.
- `Icons/` – embedded PNG assets.
- `testFiles/` – sample data inputs.

## License
License terms are not yet declared in this repository.
