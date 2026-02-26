namespace Castara.Domain.Estimation.Validation;

public static class CastIronInputConstraints
{
    // Composition (wt%)
    public const double CarbonMin = 2.5;
    public const double CarbonMax = 4.5;

    public const double SiliconMin = 0.5;
    public const double SiliconMax = 3.5;

    public const double ManganeseMin = 0.0;
    public const double ManganeseMax = 2.0;

    public const double PhosphorusMin = 0.0;
    public const double PhosphorusMax = 1.0;

    public const double SulfurMin = 0.0;
    public const double SulfurMax = 1.0;

    // Section
    public const double ThicknessMinMm = 0.0001;
    public const double CoolingRateMinCPerSec = 0.0001;

    // Optional “typical guidance” (not validation)
    public const double CoolingRateTypicalMin = 0.05;
    public const double CoolingRateTypicalMax = 20.0;
}