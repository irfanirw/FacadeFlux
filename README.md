<p align="center">
  <img src="" alt="FacadeFlux Logo" width="128" height="128">
</p>


# FacadeFlux

A Grasshopper toolkit for BCA Singapore facade compliance workflows (ETTV and RETV).

[![build]](#) [![license]](#) [![release]](#)

## Overview

FacadeFlux is a Grasshopper component collection designed to help architects, façade engineers, and energy consultants evaluate façade thermal performance against Singapore's Building and Construction Authority (BCA) ETTV and RETV methodologies. It supports early-design exploration through documentation-stage compliance checks, integrating parametric modelling, material libraries, and exportable reports.

## Key capabilities

- ETTV and RETV calculation engines following BCA methodology.
- Parametric Grasshopper components for façade geometry, glazing, shading, and material properties.
- Batch-processing of multiple orientations, façade panels, or design variants.
- Glass and material libraries with editable thermal and optical properties (U-value, SHGC, VLT).
- Exportable reports (html) with summary and detailed breakdowns.
- Visualization outputs: plots, color-coded façades, and heatmaps in Rhino/Grasshopper.
- U-value and solar-gain helper tools for component-level analysis.
- Extensible architecture with scripting/API hooks for advanced workflows and integration.

## Main components (examples)

- ETTV_Calculator
  - Purpose: Computes ETTV for a single façade panel or grouped surfaces according to BCA methodology.
  - Inputs: Geometry, Orientation, Area, Wall U-value, Window properties (U-value, SHGC, VLT), Shading info, Internal gains.
  - Outputs: ETTV (W/m²), breakdown (conduction, solar, internal), component-level report.

- RETV_Calculator
  - Purpose: Implements the Revised ETTV (RETV) methodology and outputs RETV-compliant metrics.
  - Inputs/Outputs: Similar to ETTV_Calculator with RETV-specific weighting and fields.

- GlazingLibrary
  - Purpose: Lookup and edit glazing thermal and optical properties for reuse across projects.
  - Inputs: Glazing ID / name.
  - Outputs: U-value, SC, description.

- UValue_Helper
  - Purpose: Calculates composite U-values for multi-layered constructions (walls, roofs, windows).
  - Inputs: Layer stack (thickness, conductivity), material library references.
  - Outputs: U-value, layer-by-layer heat transfer details.

- ReportExporter
  - Purpose: Generates HTML reports and human-readable summaries suitable for compliance submissions.
  - Inputs: Component outputs or grouped result trees.
  - Outputs: HTML.

## Installation

Requirements
- Rhino 7 or newer with Grasshopper.
- .NET runtime as required by the build (Rhino commonly provides the required runtime).
- Optional: Git, Visual Studio 2022+ or .NET SDK for building from source.

Install prebuilt package
1. If a .gha/.dll is provided in releases, copy it into your Grasshopper Components folder:
   - Windows: %APPDATA%/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper/Components
2. Restart Rhino. Components should appear in the Grasshopper tabs.


Build from source
1. Clone the repository:
   - git clone https://github.com/irfanirw/FacadeFlux.git
2. Open the solution (.sln) in Visual Studio or build using:
   - dotnet build
3. Copy the compiled .gha/.dll into your Grasshopper Components folder (see above).

## Quick start (ETTV example)

1. Open Rhino and launch Grasshopper.
2. Install or load FacadeFlux components (they appear in the toolbar once .gha is loaded).
3. Create a simple façade surface in Rhino (e.g., a planar rectangle or a box face).
4. In Grasshopper:
   - Drop an ETTV_Calculator component.
   - Connect your façade geometry to the Geometry input.
   - Set Orientation (bearing), Area, Wall/Window U-values, and glazing inputs (use GlazingLibrary).
   - Connect shading geometry to ShadingTool or provide a shading factor.
5. Run the ETTV_Calculator and inspect:
   - ETTV output (W/m²).
   - Breakdown outputs (conduction, solar gain, internal heat).
6. Use ReportExporter to save results as html for submission or further analysis.

(Placeholder: add screenshots of Grasshopper canvas and exported CSV in the examples folder.)

## Usage tips

- Units: Maintain consistent units across inputs (meters, W/m²·K, °C). Rhino units affect geometry-area calculations.
- Batch runs: Use data trees to run analyses on multiple façades/orientations in one pass.
- Caching: Cache intermediate results (shading, solar maps) when running repeated simulations on large models.
- Performance: Reduce mesh resolution for visualization-only runs; use higher resolution for final exports.
- Climate data: For Singapore, use typical weather files (TMY) — store them in a /data/weather folder and reference them with the climate input.
- Troubleshooting: Check Grasshopper’s runtime console for component exceptions; verify geometry is planar and normals are consistent for façade panels.

## Examples and demos

Check the /examples folder for sample Grasshopper (.gh) files demonstrating typical ETTV and RETV workflows, including:
- simple-ettv.gh — minimal ETTV workflow for a single façade panel
- batch-ettv.gh — batch-processing example for multiple orientations

If example files are missing, add sample Grasshopper files showing:
- connecting geometry to ETTV_Calculator
- using GlazingLibrary entries
- producing CSV exports via ReportExporter

## Contributing

Contributions are welcome.
- Report issues at: https://github.com/irfanirw/FacadeFlux/issues
- Submit pull requests against the default branch with a clear description of changes.
- Coding style: follow existing C# conventions in the project, include XML comments for public APIs, and add unit tests for new calculation logic where applicable.
- Testing: include simple example .gh files for new features and unit tests for calculation modules.

## License

Specify the repository license here (e.g., MIT, Apache-2.0). Update this section with the chosen license and include a LICENSE file at the repository root.

## Acknowledgements

- BCA Singapore for ETTV/RETV methodology references.
- Rhino & Grasshopper for the plugin platform.
- Contributors and users who provide feedback and test cases.

---
