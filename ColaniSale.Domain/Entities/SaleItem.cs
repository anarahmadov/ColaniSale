namespace ColaniSale.Domain.Entities;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; } 
    public decimal? Weight { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Parameters { get; set; }
}
