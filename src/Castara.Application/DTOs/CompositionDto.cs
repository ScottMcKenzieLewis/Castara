namespace Castara.Application.DTOs;

public sealed record CompositionDto(
    double Carbon,     // C %
    double Silicon,    // Si %
    double Manganese,  // Mn %
    double Phosphorus, // P %
    double Sulfur,     // S %
    double Chromium,   // Cr %
    double Nickel,     // Ni %
    double Molybdenum  // Mo %
);