namespace ColaniSale.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
