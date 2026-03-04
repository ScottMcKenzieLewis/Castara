# Castara

**Cast Iron Property Estimation & Analysis Tool**

A modern WPF desktop application for estimating mechanical properties of gray cast iron based on chemical composition and casting section characteristics. Built with .NET 8 and Material Design, Castara provides foundry engineers and metallurgists with an intuitive interface for rapid property predictions with visual feedback.

## Screenshot
<img width="1484" height="962" alt="image" src="https://github.com/user-attachments/assets/3248b7be-8fd4-4ac3-b256-01769b909433" />

## Demo

https://github.com/user-attachments/assets/6c808a1a-e29e-44f1-a407-fa73e9d0b984

## Features

### Core Functionality
- **Real-Time Property Estimation**: Calculate carbon equivalent, graphitization tendency, and hardness ranges from composition inputs
- **Risk Analysis**: Automatic identification of potential casting defects (chill risk, shrinkage, machinability concerns)
- **Unit System Support**: Seamless switching between Standard (SI) and American Standard units with automatic conversion
- **Input Validation**: Real-time validation with field-level error messages and visual feedback

### Visualization
- **Composition Bar Chart**: Visual representation of alloy element percentages
- **Gauge Displays**: Graphitization score and hardness range indicators
- **Theme Support**: Dark/Light mode with synchronized chart theming

### Technical Features
- **MVVM Architecture**: Clean separation of concerns with comprehensive view model testing
- **Logging Infrastructure**: Built-in diagnostic logging with searchable, filterable log viewer
- **Material Design UI**: Modern, professional interface using MaterialDesignInXaml
- **Type-Safe Domain Models**: Robust domain layer with validation constraints

---

## Technology Stack

- **.NET 8.0** - Latest LTS framework
- **WPF** - Windows Presentation Foundation for rich desktop UI
- **C# 12.0** - Modern language features including primary constructors and record types
- **OxyPlot** - High-performance chart visualizations
- **MaterialDesignInXaml** - Material Design theming
- **Microsoft.Extensions.Logging** - Structured logging infrastructure
- **xUnit** - Unit testing framework with Moq

---

## Architecture

### Project Structure
- **Castara.sln**: Solution file containing all projects
- **README.md**: This documentation file
- **/.github**: GitHub-specific files, including issue templates
- **/assets**: Demo data files and application assets
- **/docs**: Additional documentation (if any)

### Domain Layer
The domain layer contains pure business logic:
- **CastIronEstimator**: Core estimation algorithms
- **SectionProfile**: Input validation and modeling
- **CastIronEstimate**: Result encapsulation
- **SectionGuards**: Business rule validation

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

## TODO / Roadmap

### Planned Features
- [ ] **Stock Inventory Integration** - Constrain composition inputs to feed from stock inventory service, ensuring accuracy and traceability to available materials
- [ ] **Profile Persistence** - Allow saving of section profiles and composition data to database for historical tracking and analysis
- [ ] **Additional Telemetry** - Incorporate domain logging and events
- [ ] **Expand Test Coverage** - Always
- [ ] **Add support for multiple Casting Profiles**.  
  The current implementation assumes a default profile of **Green Sand Gray Iron**.  
  Future profiles may include other casting processes and iron types (e.g., Ductile Iron, Resin Sand Gray Iron, etc.), each with their own parameter ranges and estimation logic.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Guidelines
- Follow existing code style and conventions
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

## Professional Engineering Notice

**Castara is an educational and reference tool.** For production environments, always:

1. **Engage Professional Engineers**: Consult with licensed metallurgical engineers and certified foundry professionals
2. **Follow Standards**: Adhere to relevant industry standards (ASTM A48, A536, ISO 185, etc.)
3. **Laboratory Testing**: Conduct appropriate physical and chemical testing
4. **Quality Control**: Implement proper QC procedures and documentation
5. **Material Certifications**: Obtain and maintain proper material certifications
6. **Safety Protocols**: Follow all applicable safety regulations and guidelines

This software provides **estimates only** and cannot account for all real-world variables including:
- Actual melting practices and equipment variations
- Inoculant effects and fading
- Mold design and gating systems
- Pouring temperature and technique
- Cooling rates and heat treatment
- Microstructure variations
- Environmental conditions

**Always verify critical properties through standardized testing and professional metallurgical analysis.**

---

*Educational Tool - Not for Production Use Without Professional Engineering Validation*




