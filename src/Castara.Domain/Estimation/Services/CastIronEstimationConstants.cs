namespace Castara.Domain.Estimation.Services;

internal static class CastIronEstimationConstants
{
    public const double ThicknessPivotMm = 20.0;
    public const double ThicknessScale = 80.0;

    public const double BaseGraphScore = 0.45;
    public const double CePivot = 3.6;
    public const double CeWeight = 0.12;
    public const double CoolingWeight = 0.25;
    public const double ThicknessWeight = 0.20;

    public const int BaseHardnessHB = 220;
    public const double HardnessGraphWeight = 60.0;
    public const double HardnessCoolingWeight = 35.0;
    public const double HardnessThicknessWeight = 25.0;

    public const int HardnessSpreadHB = 15;
    public const int MinHardnessHB = 140;
    public const int MaxHardnessHB = 320;
}