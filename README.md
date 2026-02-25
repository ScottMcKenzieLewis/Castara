# Castara

A modern WPF application for cast iron composition analysis and property estimation. Castara provides real-time calculations of carbon equivalent, graphitization potential, and hardness predictions based on chemical composition and section parameters.

## Overview

Castara helps metallurgists and engineers analyze cast iron compositions by:
- Calculating carbon equivalent and key metallurgical factors
- Predicting graphitization tendency and hardness
- Identifying potential risks and quality concerns
- Visualizing composition data and gauge metrics

## Features

### 🧪 Composition Analysis
- Input chemical composition (C, P, Si, S, Mn)
- Section parameters (thickness, cooling rate)
- Real-time carbon equivalent calculation
- Cooling and thickness factor computation

### 📊 Visualization
- Interactive composition bar charts
- Graphitization and hardness gauge displays
- Material Design UI with dark/light theme support

### ⚠️ Risk Assessment
- Automated risk flag detection
- Severity-coded warnings (Critical, Warning, Info)
- Detailed messages for each risk condition
- Validation against metallurgical standards

### 💡 User Experience
- Contextual tooltips for all inputs
- Responsive Material Design interface
- Clear visual feedback
- Easy data entry and clearing

## Technology Stack

### Frontend
- **WPF** (.NET 8) - Modern desktop UI framework
- **Material Design in XAML** - Material Design components and theming
- **OxyPlot** - Advanced charting and data visualization
- **Caliburn.Micro** (likely) - MVVM framework

### Architecture
- **MVVM Pattern** - Clean separation of concerns
- **Domain-Driven Design** - Rich domain models and services
- **Dependency Injection** - Loosely coupled components

## Project Structure

1. **Castara.sln** - Solution file
2. **Castara/** - Main application project
3. **Build the solution**

## Getting Started

### Prerequisites
- .NET 8 SDK or later
- Visual Studio 2022 or Visual Studio Code
- Windows 10/11 (for WPF)

### Installation

1. **Clone the repository**
   git clone https://github.com/ScottMcKenzieLewis/Castara.git cd Castara
2. **Restore NuGet packages**
   dotnet restore
3. **Build the solution**
   dotnet build
4. **Run the application**
   dotnet run --project src/Castara.Wpf

## Usage

### Basic Workflow

1. **Enter Composition**
- Input weight percentages for C, P, Si, S, Mn
- Use tooltips (ℹ️) for guidance on typical ranges

2. **Set Section Parameters**
- Thickness (mm) - Physical section thickness
- Cooling Rate (°C/s) - Expected cooling rate

3. **Calculate**
- Click "Calculate" to run the analysis
- Review KPIs: Carbon Equivalent, Graphitization, Hardness
- Check ModelInputs for derived factors

4. **Review Results**
- Examine composition visualization
- Check gauge readings for graphitization and hardness
- Review any risk flags in the bottom panel

5. **Clear**
- Use "Clear" button to reset all inputs

### Example Values

**Typical Gray Iron:**
- Carbon: 3.4%
- Silicon: 2.1%
- Manganese: 0.7%
- Phosphorus: 0.05%
- Sulfur: 0.08%
- Thickness: 25mm
- Cooling Rate: 1.5°C/s

## Architecture

### Domain Layer
The domain layer contains pure business logic:
- **CastIronEstimator**: Core estimation algorithms
- **SectionProfile**: Input validation and modeling
- **CastIronEstimate**: Result encapsulation
- **SectionGuards**: Businessrule validation

### Presentation Layer
The WPF layer handles UI concerns:
- **CalculationsViewModel**: Presentation logic and state
- **CalculationsView**: XAML-based UI definition
- **Converters**: Data transformation for display
- **Material Design**: Consistent visual language

### Key Patterns
- **MVVM**: Clean separation between UI and logic
- **Data Binding**: Reactive UI updates
- **Commands**: User action handling
- **Value Converters**: Display transformations

## Risk Flags

The application monitors for various conditions:
- Carbon equivalent outside optimal range
- Excessive graphitization potential
- Hardness predictions (high/low)
- Section thickness concerns
- Cooling rate issues
- Composition imbalances

Each flag includes:
- **Code**: Unique identifier
- **Name**: Human-readable description
- **Message**: Detailed explanation
- **Severity**: Critical, Warning, or Info

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Guidelines
- Follow existingcode style and conventions
- Write meaningful commit messages
- Include unit tests for new features
- Update documentation as needed

## License

MIT License

Copyright (c) 2026 Scott McKenzie Lewis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Authors

- Scott McKenzie Lewis ([@ScottMcKenzieLewis](https://github.com/ScottMcKenzieLewis))

## Acknowledgments

- Material Design in XAML Toolkit
- OxyPlot charting library
- .NET community

## Support

For issues, questions, or contributions, please visit:
- **Issues**: https://github.com/ScottMcKenzieLewis/Castara/issues
- **Discussions**: https://github.com/ScottMcKenzieLewis/Castara/discussions

---

**Note**: This application provides estimates based on empirical models and should not be used as the sole basis for critical metallurgical decisions. Always consult with qualified metallurgists and perform appropriate testing.




