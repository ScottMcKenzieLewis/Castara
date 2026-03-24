using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Wpf.Tests.Common;

/// <summary>
/// Factory helpers for common CastingProfileDefinition test instances.
/// Keeps test setup intention-revealing and consistent.
/// </summary>
public static class CastingProfileTestData
{
    public static CastingProfileDefinition CreateValidProfile(
        string id = "green-sand-gray-iron-general",
        string displayName = "Green Sand Gray Iron - General",
        string processFamily = "Green Sand",
        string ironType = "Gray Iron",
        double defaultSectionThicknessMm = 12.0,
        double carbonMin = 3.0,
        double carbonMax = 4.0,
        double siliconMin = 1.8,
        double siliconMax = 2.8,
        double manganeseMin = 0.4,
        double manganeseMax = 0.9,
        double phosphorusMin = 0.02,
        double phosphorusMax = 0.12,
        double sulfurMin = 0.005,
        double sulfurMax = 0.08,
        double preferredCarbonEquivalentMin = 3.9,
        double preferredCarbonEquivalentMax = 4.3,
        double graphitizationBias = 1.0,
        double coolingSeverityFactor = 1.0,
        double chillRiskCeiling = 3.7,
        double shrinkageRiskFloor = 3.5,
        double hardnessWarningMinBhn = 170,
        double hardnessWarningMaxBhn = 230)
    {
        return new CastingProfileDefinition(
            Id: id,
            DisplayName: displayName,
            ProcessFamily: processFamily,
            IronType: ironType,
            DefaultSectionThicknessMm: defaultSectionThicknessMm,
            CarbonMin: carbonMin,
            CarbonMax: carbonMax,
            SiliconMin: siliconMin,
            SiliconMax: siliconMax,
            ManganeseMin: manganeseMin,
            ManganeseMax: manganeseMax,
            PhosphorusMin: phosphorusMin,
            PhosphorusMax: phosphorusMax,
            SulfurMin: sulfurMin,
            SulfurMax: sulfurMax,
            PreferredCarbonEquivalentMin: preferredCarbonEquivalentMin,
            PreferredCarbonEquivalentMax: preferredCarbonEquivalentMax,
            GraphitizationBias: graphitizationBias,
            CoolingSeverityFactor: coolingSeverityFactor,
            ChillRiskCeiling: chillRiskCeiling,
            ShrinkageRiskFloor: shrinkageRiskFloor,
            HardnessWarningMinBhn: hardnessWarningMinBhn,
            HardnessWarningMaxBhn: hardnessWarningMaxBhn);
    }

    public static CastingProfileDefinition CreateNarrowCarbonProfile(
        double carbonMin = 3.2,
        double carbonMax = 3.6,
        string displayName = "Narrow Carbon Window")
    {
        return CreateValidProfile(
            displayName: displayName,
            carbonMin: carbonMin,
            carbonMax: carbonMax);
    }
}