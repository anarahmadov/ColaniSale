namespace ColaniSale.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Weight { get; set; }
    public string? Parameters { get; set; }
}
