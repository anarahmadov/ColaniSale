namespace ColaniSale.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? SaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}
