namespace ColaniSale.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime SaleDate { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
