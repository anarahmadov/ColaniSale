namespace ColaniSale.Application.Authorization.Roles;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Seller = "Seller";
    public const string Warehouse = "Warehouse";
    public const string Accountant = "Accountant";

    public static IReadOnlyList<string> GetAll() =>
    [
        SuperAdmin,
        Admin,
        Manager,
        Seller,
        Warehouse,
        Accountant
    ];
}
