namespace ColaniSale.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? Notes { get; set; } 
    public DateTime CreatedAt { get; set; }
}
