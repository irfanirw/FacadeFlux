
# BcaEttv

**Building Construction Authority Environmental Thermal Transfer Value (ETTV) Calculator**

## 📦 Overview

**BcaEttv** is a C# library designed to support ETTV calculations for building envelope components in compliance with Singapore’s BCA Green Mark requirements. It provides a structured object-oriented model for simulating heat gain through opaque and fenestration surfaces, enabling designers to evaluate compliance and optimize building performance.

## 🧱 Features

- Modular class architecture for:
  - Opaque and fenestration constructions
  - Surface geometry and orientation
  - Material thermal properties
- ETTV computation logic based on BCA formulas
- Rhino.Geometry integration for mesh-based surface modeling
- Extensible model for simulation and reporting

## 🛠️ Installation

Clone the repository:

```bash
git clone https://github.com/irfanirw/BcaEttv.git
```

Open the solution in Visual Studio and restore NuGet packages.

## 🚀 Usage

Example usage:

```csharp
var mat = new EttvMat { Name = "Concrete", Thickness = 0.2, ThermalConductivity = 1.4 };
var construction = new FluxOpaqueConstruction { Layers = new List<EttvMat> { mat } };
double uValue = construction.CalculateU(new List<EttvMat> { mat });
```

## 📁 Project Structure

- `EttvMat`: Defines material properties
- `FluxConstruction`: Abstract base for construction types
- `FluxOpaqueConstruction` / `FluxFenestrationConstruction`: Specialized implementations
- `EttvSrf`: Represents building surfaces with geometry and heat gain logic
- `FluxOrientation`: Handles orientation vectors and correction factors
- `FluxModel`: Aggregates all components for full ETTV simulation

## 👥 Contributors

- **Irfan Irwanuddin** – https://github.com/irfanirw
- **Galuh Kresnadian Tedjawinata** – https://github.com/tedjawinata

## 📄 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 🙌 Acknowledgements

Inspired by BCA Green Mark guidelines and Rhino.Geometry for spatial modeling.

---
