using Microsoft.AspNetCore.Identity;

namespace ColaniSale.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}
