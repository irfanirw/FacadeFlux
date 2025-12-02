# FacadeFlux

A Grasshopper toolkit for BCA Singapore facade compliance workflows (ETTV and RETV).

![Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![License](https://img.shields.io/badge/license-TBD-lightgrey) ![Release](https://img.shields.io/badge/release-v0.1.0-blue)

## Overview

FacadeFlux provides a set of parametric Grasshopper components and utilities to evaluate facade thermal performance for Singapore's Building and Construction Authority (BCA) compliance checks. It is aimed at architects, façade engineers, and energy consultants performing ETTV (Envelope Thermal Transfer Value) and RETV (Revised ETTV) analyses across early design through documentation stages.

## Key capabilities

- ETTV and RETV calculation engines following BCA methodology.
- Parametric Grasshopper components for facade geometry, glazing, shading and material properties.
- Batch-processing of multiple facade orientations or design variants.
- Climate data integration (Singapore weather files support) and timezone handling.
- Glass and material libraries with editable properties.
- Exportable reports (CSV, JSON) with summary and detailed breakdowns.
- Visualization outputs: plots, heatmaps and color-coded facade previews in Rhino/Grasshopper.
- Support for U-value and solar gain helper tools for component-level analysis.
- Extensible architecture with scripting/API hooks for advanced workflows.

## Main components (examples)

- **ETTV_Calculator**: Computes ETTV for a facade panel or grouped surfaces.
  - Inputs: Geometry, Orientation, Area, Wall U-value, Roof U-value, Window properties, Shading info.
  - Outputs: ETTV value, breakdown (conduction, solar, internal gains).

- **RETV_Calculator**: Implements the revised ETTV methodology.
  - Inputs/Outputs: Similar to ETTV_Calculator but with RETV-specific fields.

- **GlazingLibrary**: Lookup and edit glazing thermal and optical properties (SHGC, U-value, VLT).

- **ShadingTool**: Computes shading factors and effective solar reductions for given sun path.

- **WWR_Tool**: Calculates Window-to-Wall Ratio and suggests compliant configurations.

- **ReportExporter**: Generates CSV/JSON reports and a human-readable summary for compliance submission.

## Installation

### Requirements

- Rhino 7 or newer with Grasshopper.
- .NET runtime (as required by the build) — typically provided by Rhino.

### Options

1. **Install prebuilt package**
   - Copy the provided `.gha`/`.dll` into your Grasshopper Components folder or use Rhino's Package Manager if a package is published.
   - Grasshopper Libraries folder locations:
     - Windows: `%AppData%\Grasshopper\Libraries`
     - macOS: `~/Library/Application Support/McNeel/Rhinoceros/MacPlugIns/Grasshopper/Libraries`

2. **Build from source**
   - Clone the repo and open the solution in Visual Studio or use `dotnet build`.
   - Example build command: `dotnet build -c Release -f net7.0-windows` (Windows) or `-f net7.0` (cross-platform Grasshopper 8).
   - Copy compiled assemblies (`.gha`) from `bin/Release/<tfm>/` into the Grasshopper Components folder.

## Quick start

1. Open Rhino and Grasshopper.
2. Load the FacadeFlux components (they appear in the toolbar after installing `.gha`).
3. Open an example file in `/examples` (if available) or create a simple box surface in Rhino.
4. Connect geometry to ETTV_Calculator and set glazing/material properties.
5. Run the simulation and inspect the outputs and exported report.

<!-- Placeholder for Grasshopper screenshot -->
<!-- ![Grasshopper Example](./docs/screenshot-example.png) -->

### Code example

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
var ettvResult = FluxCalculator.CalculateEttv(model);
```

## Usage tips

- Keep units consistent (meters, watts, degrees Celsius).
- Use grouped components and data trees for batch runs across multiple facades.
- Cache intermediate results for large models to improve performance.
- Use the GlazingLibrary to maintain consistent glass specifications across projects.
- For climate data, use typical Singapore weather files (EPW format) compatible with BCA requirements.
- Troubleshooting: Ensure all required inputs are connected; check Rhino console for error messages.

## Examples and demos

Check `/examples` for sample Grasshopper (`.gh`) files demonstrating typical ETTV and RETV workflows. If examples are missing, contributions are welcome—please add sample files or request them via issues.

The `/testFiles` folder contains sample data inputs that can be used for testing and validation.

## Contributing

Contributions are welcome! Please:
- Open issues for bugs or feature requests.
- Submit pull requests with clear descriptions and tests where appropriate.
- Follow the existing coding style and conventions.
- Ensure your changes build successfully before submitting.

## License

License terms are not yet declared in this repository.

## Acknowledgements

Thanks to all contributors and the BCA Singapore for providing the ETTV/RETV methodology guidelines.
