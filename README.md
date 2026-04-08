# Castara

**Cast Iron Property Estimation & Analysis Tool**

A modern WPF desktop application for estimating mechanical properties of gray cast iron based on chemical composition, casting section characteristics, and process-specific profiles. Built with .NET 8 and Material Design, Castara provides foundry engineers and metallurgists with an intuitive interface for rapid property predictions with profile-driven risk assessment and visual feedback.

## Screenshot
<img width="1200" height="780" alt="image" src="https://github.com/user-attachments/assets/8a5da74e-ac7c-4f1f-930a-65bd7f1ae6e5" />

## Demo

https://github.com/user-attachments/assets/a326756b-e2e7-4335-81e8-a604c9eebeab

## Features

### Core Functionality
- **Multiple Casting Profiles**: Support for different casting processes (Green Sand, No-Bake, Shell Mold) with process-specific tuning parameters
- **Real-Time Property Estimation**: Calculate carbon equivalent, graphitization tendency, and hardness ranges from composition inputs
- **Profile-Driven Risk Analysis**: Process-aware risk assessment with profile-specific thresholds for chill risk, shrinkage sensitivity, and machinability concerns
- **Unit System Support**: Seamless switching between Standard (SI) and American Standard units with automatic conversion
- **Input Validation**: Real-time validation with field-level error messages and visual feedback against profile-specific composition ranges

### Visualization
- **Composition Bar Chart**: Visual representation of alloy element percentages with profile-aware valid ranges
- **Gauge Displays**: Graphitization score and hardness range indicators
- **Theme Support**: Dark/Light mode with synchronized chart theming
- **Risk Indicators**: Color-coded severity levels (Low/Medium/High) with detailed explanations

### Technical Features
- **MVVM Architecture**: Clean separation of concerns with comprehensive view model testing
- **Domain-Driven Design**: Rich domain models with profile-based strategy pattern for estimation
- **Logging Infrastructure**: Built-in diagnostic logging with searchable, filterable log viewer and notification badges
- **Material Design UI**: Modern, professional interface using MaterialDesignInXaml with custom chrome
- **Type-Safe Domain Models**: Robust domain layer with validation constraints and immutable value objects
- **AutoMapper Integration**: Seamless DTO-to-domain model transformation
- **Crash Reporting**: Automated crash report generation with privacy-preserving path sanitization

### Crash Reporting & Diagnostics
<img width="887" height="633" alt="image" src="https://github.com/user-attachments/assets/d4289e10-c8dc-4e08-88f1-ee14acdcc00b" />

Castara includes a comprehensive crash reporting system that captures detailed diagnostic information when unexpected errors occur and stores it both locally and in the cloud for analysis:

#### Features
- **Automatic Crash Report Generation**: Unhandled exceptions are automatically captured and converted into structured crash reports
- **Privacy-Preserving Sanitization**: File paths and usernames are automatically redacted from error messages, stack traces, and log entries while preserving filenames for debugging
- **Rich Diagnostic Context**: Reports include:
  - Exception details (type, message, stack trace, inner exceptions)
  - System information (OS, .NET runtime version, application version)
  - Application state snapshot (theme, active view, casting profile, unit system, current composition values)
  - Recent log entries (last 200 entries with timestamps, levels, categories, and messages)
- **JSON Format**: Reports are saved as human-readable, structured JSON files for easy analysis
- **Dual Storage**:
  - **Local Storage**: Reports are stored in `%LocalAppData%\Castara\CrashReports` with timestamped filenames
  - **Cloud Storage**: Reports are uploaded to AWS S3 for centralized analysis and long-term archival
- **User Control**: Interactive dialog allows users to review crash details and choose whether to save locally and/or send to the diagnostic server
- **Optional Upload**: Crash report upload to the diagnostic server can be disabled in `appsettings.json` by setting `CrashReportUpload.Enabled` to `false`

When upload is disabled (`Enabled: false`), users will only see the option to save crash reports locally. This is useful for:
- **Privacy-sensitive environments** where external data transmission is restricted
- **Offline installations** without internet connectivity
- **Development/testing** scenarios where server upload is not needed
- **Organizations with internal-only diagnostics** workflows

##### Server Configuration (Castara.Web.Api)

The diagnostic API's S3 storage can be configured in `appsettings.json`:
```
{ "CrashReportUpload": 
    { 
        "Enabled": true, // Set to false to disable crash report upload "BaseUrl": "https://...",     
        "KeyId": "castara",           
        "HmacKey": "...",             
        "TimeoutSeconds": 10
    } 
}
```

#### AWS S3 Storage

The diagnostic API stores crash reports in Amazon S3 for:
- **Centralized Analysis**: All crash reports from all users in one searchable location
- **Long-term Archival**: Durable, scalable storage with configurable lifecycle policies
- **Analytics Integration**: Compatible with AWS Athena, Glue, QuickSight for trend analysis
- **Security**: Server-side encryption (AES256 or KMS) with IAM-controlled access

**S3 object key structure:**

```
{version}/{timestamp:yyyy/MM/dd}/{unique-id}.json
```

- **version**: Application version (e.g., `1.0.0`)
- **timestamp**: UTC timestamp of the crash (e.g., `2023-10-05T14-48-00Z`)
- **unique-id**: Randomly generated identifier for the crash report file

Reports are uploaded in real-time as files are generated, ensuring immediate availability for analysis. The cloud storage integration is transparent to the user and can be disabled in the application settings.

---

## Technology Stack

- **.NET 8.0** - LTS framework
- **WPF** - Windows Presentation Foundation for rich desktop UI
- **C# 12.0** - Modern language features including primary constructors and record types
- **AutoMapper** - Object-to-object mapping for DTOs and domain models
- **OxyPlot** - High-performance chart visualizations
- **MaterialDesignInXaml** - Material Design theming and components
- **Microsoft.Extensions.DependencyInjection** - Built-in dependency injection
- **Microsoft.Extensions.Logging** - Structured logging infrastructure
- **xUnit** - Unit testing framework with Moq and FluentAssertions

---

## Architecture


### Domain Layer (Castara.Domain)

The domain layer contains pure business logic with no external dependencies:

- **CastIronEstimator**: Orchestrates estimation by selecting appropriate strategy based on profile
- **ICastingEstimatorStrategy**: Strategy interface for process-specific estimation algorithms
- **GrayIronCastingEstimatorStrategy**: Unified gray iron model with profile-driven tuning
- **CastingProfileDefinition**: Immutable profile definition with validation and tuning parameters
- **CastIronComposition**: Chemical composition value object with validation
- **SectionProfile**: Section geometry and cooling characteristics
- **CastIronEstimate**: Result encapsulation with risk flags
- **Guards**: Business rule validation (CompositionGuards, SectionGuards)

#### Strategy Pattern for Profiles

The estimation engine uses a strategy pattern where different iron types (Gray Iron, future Ductile Iron) get different strategies, while process variations (Green Sand, No-Bake, Shell Mold) are handled through profile parameters within the same strategy:

- **GraphitizationBias**: Process tendency toward graphitic vs. carbide structures (0.85-1.15)
- **CoolingSeverityFactor**: How aggressively the process cools (0.7-1.3)
- **ChillRiskCeiling**: Process-specific CE threshold for chill risk (3.8-4.3)
- **ShrinkageRiskFloor**: Process-specific CE threshold for feeding sensitivity (4.0-4.5)
- **HardnessWarningMinBhn/MaxBhn**: Acceptable hardness range for the process

### Application Layer (Castara.Application)

Handles DTO transformation and data access:

- **CastingProfileConfig DTOs**: Hierarchical configuration structure loaded from JSON
- **CastingProfileMappingProfile**: AutoMapper configuration for DTO-to-domain transformation
- **JsonCastingProfileRepository**: JSON file-based profile repository
- **ICastingProfileRepository**: Repository abstraction

### Presentation Layer (Castara.Wpf)

The WPF layer handles UI concerns with MVVM pattern:

- **ShellViewModel**: Main window view model with profile selection and theme management
- **CalculationsViewModel**: Calculation form with composition inputs and results display
- **LogViewerViewModel**: Diagnostic log viewer with filtering and search
- **CastingProfileOption**: Presentation model for profile dropdown
- **StatusService**: Centralized status message and indicator management
- **Material Design Styles**: Custom theme integration with dark/light mode support

### Key Patterns

- **MVVM**: Clean separation between UI and logic with data binding
- **Strategy Pattern**: Profile-based estimation strategy selection
- **Repository Pattern**: Abstracted data access for profiles
- **Domain-Driven Design**: Rich domain models with business logic encapsulation
- **Dependency Injection**: Built-in DI container for service resolution
- **Value Objects**: Immutable composition and section models
- **Guard Clauses**: Input validation at domain boundaries

---

## Casting Profiles

### Current Profiles

Castara ships with several gray iron profiles representing different casting processes:

1. **Green Sand Gray Iron** (Default)
   - Medium cooling severity (1.0)
   - Balanced graphitization bias (1.0)
   - Most flexible feeding characteristics

2. **No-Bake Gray Iron**
   - Increased cooling severity (1.15)
   - Slightly reduced graphitization (0.95)
   - More restrictive feeding requirements

3. **Shell Mold Gray Iron**
   - Highest cooling severity (1.25)
   - Reduced graphitization tendency (0.90)
   - Most challenging feeding conditions

4. **Heavy Section Gray Iron**
   - Reduced cooling severity (0.85)
   - Enhanced graphitization (1.10)
   - Lower hardness expectations

Each profile defines:
- Valid composition ranges (C, Si, Mn, P, S)
- Default section thickness
- Target carbon equivalent range
- Process-specific tuning factors
- Risk assessment thresholds

### Profile File Format

Profiles are stored as JSON in `assets/profiles/`:

```json
{
  "id": "green-sand-gray",
  "displayName": "Green Sand Gray Iron",
  "processFamily": "GreenSand",
  "ironType": "GrayIron",
  "defaults": {
    "sectionThicknessMm": 25.0
  },
  "ranges": {
    "carbonMin": 2.5,
    "carbonMax": 4.0,
    "siliconMin": 1.0,
    "siliconMax": 3.5,
    // ...
  },
  "targets": {
    "preferredCarbonEquivalentMin": 4.0,
    "preferredCarbonEquivalentMax": 4.5,
    "graphitizationBias": 1.0,
    "coolingSeverityFactor": 1.0
  },
  "riskThresholds": {
    "chillRiskCeiling": 4.0,
    "shrinkageRiskFloor": 4.2,
    "hardnessWarningMinBhn": 170,
    "hardnessWarningMaxBhn": 250
  }
}
```

---

## Risk Assessment

The application provides profile-aware risk monitoring for various casting conditions:

### Risk Categories

1. **Chill Risk (CHILL_RISK)**
   - Evaluates tendency toward white iron (carbide) structure
   - Considers CE deficit below profile's chill ceiling
   - Factors in graphitization score, cooling rate, and section thickness
   - Profile-specific thresholds account for process cooling characteristics

2. **Shrinkage/Porosity Risk (SHRINK_RISK)**
   - Assesses feeding sensitivity and solidification shrinkage potential
   - Evaluates CE against profile's shrinkage floor
   - Accounts for section thickness and manganese content
   - Process-specific thresholds reflect mold rigidity and feeding capability

3. **Machinability Concern (MACHINABILITY)**
   - Identifies potential machining difficulties
   - Compares predicted hardness to profile's acceptable range
   - Considers graphitization tendency and carbide formation
   - Application-specific thresholds for machined vs. wear components

### Severity Levels

Each risk flag includes:
- **Code**: Unique identifier (e.g., "CHILL_RISK")
- **Name**: Human-readable description
- **Severity**: 
  - **Low** (Score < 0.33): Minimal concern under profile assumptions
  - **Medium** (0.33 ≤ Score < 0.66): Moderate risk requiring review
  - **High** (Score ≥ 0.66): Significant risk under profile assumptions
- **Message**: Context-specific guidance and recommendations

### Multi-Factor Scoring

Risk scores combine multiple metallurgical factors:
- Chemical composition (CE, individual elements)
- Process characteristics (cooling severity, graphitization bias)
- Section geometry (thickness, cooling rate)
- Profile-specific thresholds and acceptable ranges

---

## Getting Started

### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8.0 SDK or Runtime**
- **Visual Studio 2022** or **JetBrains Rider** (for development)

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/ScottMcKenzieLewis/Castara.git
   cd Castara
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the solution**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run --project src/Castara.Wpf
   ```

### Quick Start Usage

1. **Select a Casting Profile** from the dropdown (e.g., "Green Sand Gray Iron")
2. **Enter Chemical Composition**:
   - Carbon (C): 2.5-4.0%
   - Silicon (Si): 1.0-3.5%
   - Manganese (Mn): 0.3-1.2%
   - Phosphorus (P): 0.02-0.3%
   - Sulfur (S): 0.05-0.15%
3. **Specify Section Parameters**:
   - Thickness: mm or inches
   - Cooling rate: °C/s or °F/s
4. **Review Results**:
   - Carbon Equivalent (CE)
   - Graphitization Score
   - Estimated Hardness Range (HB)
   - Risk Flags and Recommendations
5. **Switch Units** as needed (Standard ↔ American)
6. **Toggle Theme** (Dark ↔ Light) for visual preference

---

## TODO / Roadmap

### Planned Features

- [ ] **Ductile Iron Support** - Add estimation strategy and profiles for ductile (nodular) iron with different CE formula and microstructure model
- [ ] **Profile Editor** - UI for creating and editing custom casting profiles with validation
- [ ] **Stock Inventory Integration** - Constrain composition inputs to feed from stock inventory service, ensuring accuracy and traceability to available materials
- [ ] **Profile Persistence** - Allow saving of calculation sessions with composition data and results to database for historical tracking and analysis
- [ ] **Batch Analysis** - Process multiple composition scenarios and compare results side-by-side
- [ ] **Export/Reporting** - Generate PDF reports with charts, risk summaries, and recommendations
- [ ] **Additional Telemetry** - Incorporate domain events and enhanced logging for troubleshooting
- [ ] **Performance Optimization** - Profile-based caching and calculation optimization for large batch operations
- [ ] **Expand Test Coverage** - Additional unit tests for edge cases and integration tests for end-to-end workflows
- [ ] **API/Service Layer** - REST API for headless operation and integration with other systems
- [ ] **Database Persistence** - Replace JSON file storage with proper database for profiles and calculation history

### Future Iron Types & Processes

- **Ductile Iron** (Nodular/SG Iron)
  - Different CE formula: CE = C + 0.31·Si + 0.33·P
  - Nodularity and nodule count estimation
  - Inoculant and magnesium treatment modeling

- **Compacted Graphite Iron** (CGI)
  - Intermediate between gray and ductile
  - Vermicularity assessment

- **Malleable Iron**
  - Heat treatment considerations
  - Temper carbon structure prediction

- **Additional Process Variants**
  - Investment casting
  - Permanent mold
  - Centrifugal casting
  - Lost foam

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Guidelines

1. **Code Style**
   - Follow existing C# conventions and patterns
   - Use meaningful names for variables, methods, and classes
   - Keep methods focused and single-purpose
   - Add XML documentation for public APIs

2. **Architecture**
   - Respect layer boundaries (Domain → Application → Presentation)
   - Domain layer should have no external dependencies
   - Use dependency injection for service resolution
   - Follow MVVM pattern in presentation layer

3. **Testing**
   - Write unit tests for new features
   - Maintain or improve code coverage
   - Use FluentAssertions for readable test assertions
   - Mock external dependencies with Moq

4. **Commits**
   - Write clear, descriptive commit messages
   - Use conventional commit format when applicable
   - Keep commits focused and atomic

5. **Pull Requests**
   - Update documentation as needed
   - Ensure all tests pass
   - Follow the PR template
   - Link related issues

### Adding New Casting Profiles

To add a new casting profile:

1. Create a JSON file in `assets/profiles/` following the schema
2. Define composition ranges, targets, and risk thresholds
3. Set appropriate tuning parameters:
   - `graphitizationBias`: 0.85-1.15 (process graphitization tendency)
   - `coolingSeverityFactor`: 0.7-1.3 (process cooling aggressiveness)
   - `chillRiskCeiling`: 3.8-4.3 (CE threshold for chill risk)
   - `shrinkageRiskFloor`: 4.0-4.5 (CE threshold for shrinkage risk)
4. Test the profile across expected composition ranges
5. Document the profile's intended use case and process characteristics

### Adding New Estimation Strategies

For fundamentally different metallurgical regimes (e.g., Ductile Iron):

1. Implement `ICastingEstimatorStrategy` interface
2. Add strategy class to `Castara.Domain.Estimation.Services.Strategies`
3. Implement `CanHandle()` to match appropriate iron types
4. Implement `Estimate()` with domain-specific calculations
5. Register strategy in DI container
6. Add corresponding unit tests
7. Update documentation with new capabilities

---

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

---

## Professional Engineering Notice

**Castara is an educational and reference tool.** For production environments, always:

1. **Engage Professional Engineers**: Consult with licensed metallurgical engineers and certified foundry professionals
2. **Follow Standards**: Adhere to relevant industry standards (ASTM A48, A159, A536, ISO 185, SAE J431, etc.)
3. **Laboratory Testing**: Conduct appropriate physical and chemical testing using calibrated equipment
4. **Quality Control**: Implement proper QC procedures, statistical process control, and comprehensive documentation
5. **Material Certifications**: Obtain and maintain proper material certifications and test reports
6. **Safety Protocols**: Follow all applicable OSHA, EPA, and local safety regulations and guidelines
7. **Process Validation**: Validate estimation results against actual production data and adjust profiles accordingly

### Important Limitations

This software provides **estimates only** and cannot account for all real-world variables including:

**Metallurgical Factors:**
- Actual melting practices and equipment variations
- Inoculant effects, fading kinetics, and treatment effectiveness
- Nucleation site density and graphite morphology variations
- Residual element effects (Cu, Ni, Cr, Mo, etc.)
- Microstructure heterogeneity and local variations
- Heat treatment effects and thermal history

**Process Factors:**
- Mold design, gating systems, and feeding adequacy
- Pouring temperature, technique, and stream integrity
- Cooling rate variations within complex castings
- Sand properties, binder systems, and mold rigidity
- Metal cleanliness and inclusion content
- Atmospheric conditions and oxidation

**Application Context:**
- Stress states and loading conditions
- Service environment (temperature, corrosion, wear)
- Required mechanical properties and acceptance criteria
- Safety factors and design margins
- Industry-specific requirements and specifications

### Profile Limitations

Casting profiles represent **typical process characteristics** but should be:
- Calibrated to your specific foundry equipment and practices
- Validated against actual production data and test results
- Updated based on process changes and continuous improvement
- Used as starting points, not absolute specifications

**Always verify critical properties through standardized testing (ASTM, ISO) and professional metallurgical analysis.**

### Disclaimer

THE SOFTWARE AND ALL ESTIMATION RESULTS ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND. 
The authors and contributors assume no liability for decisions made based on this software's output.
Professional engineering judgment and proper testing remain essential for all production applications.

---

*Educational and Reference Tool - Not for Production Use Without Professional Engineering Validation*