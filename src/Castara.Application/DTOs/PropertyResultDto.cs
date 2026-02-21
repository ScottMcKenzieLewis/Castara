namespace Castara.Application.DTOs;

public sealed record PropertyResultDto(
    double? BrinellHardness,
    double? TensileStrengthMpa,
    string Notes
);