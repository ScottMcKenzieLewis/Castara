namespace Castara.Api.Dtos;

public sealed class ValuationLineDto
{
    public DateOnly PaymentDate { get; set; }
    public decimal CashflowAmount { get; set; }
    public decimal DiscountFactor { get; set; }
    public decimal PresentValue { get; set; }
    public string Currency { get; set; } = string.Empty;
}