namespace Castara.Application.DTOs;

public sealed record IronClassificationDto(
    string Type,       // "Gray", "Ductile", "White", "Unknown"
    string Confidence, // "Low/Med/High"
    string Rationale
);