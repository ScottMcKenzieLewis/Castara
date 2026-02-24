using System;
using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Domain.Estimation.Validation;

public static class SectionGuards
{
    public static void Validate(SectionProfile section)
    {
        if (section is null)
            throw new ArgumentNullException(nameof(section));

        if (double.IsNaN(section.ThicknessMm) || double.IsInfinity(section.ThicknessMm))
            throw new ArgumentException("ThicknessMm must be a finite number.", nameof(section));

        if (section.ThicknessMm <= 0)
            throw new ArgumentException("ThicknessMm must be > 0.", nameof(section));

        if (double.IsNaN(section.CoolingRateCPerSec) || double.IsInfinity(section.CoolingRateCPerSec))
            throw new ArgumentException("CoolingRateCPerSec must be a finite number.", nameof(section));

        if (section.CoolingRateCPerSec <= 0)
            throw new ArgumentException("CoolingRateCPerSec must be > 0 (°C/s).", nameof(section));

        // Optional sanity clamp (not required, but helps catch typos like 2000):
        // Typical casting rates are often << 50 °C/s.
        if (section.CoolingRateCPerSec > 200)
            throw new ArgumentException("CoolingRateCPerSec seems unrealistically high. Check units (expected °C/s).", nameof(section));
    }
}