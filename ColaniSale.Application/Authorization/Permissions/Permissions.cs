using System.Reflection;

namespace ColaniSale.Application.Authorization.Permissions;

public static class Permissions
{
    public static class User
    {
        public const string View = "User.View";
        public const string Create = "User.Create";
        public const string Update = "User.Update";
        public const string Delete = "User.Delete";
    }

    public static class Product
    {
        public const string View = "Product.View";
        public const string Create = "Product.Create";
        public const string Update = "Product.Update";
        public const string Delete = "Product.Delete";
    }

    public static class Customer
    {
        public const string View = "Customer.View";
        public const string Create = "Customer.Create";
        public const string Update = "Customer.Update";
        public const string Delete = "Customer.Delete";
    }

    public static class Sale
    {
        public const string View = "Sale.View";
        public const string Create = "Sale.Create";
        public const string Update = "Sale.Update";
        public const string Delete = "Sale.Delete";
    }

    public static class Payment
    {
        public const string View = "Payment.View";
        public const string Create = "Payment.Create";
        public const string Update = "Payment.Update";
        public const string Delete = "Payment.Delete";
    }

    public static class Report
    {
        public const string View = "Report.View";
    }

    public static IReadOnlyList<string> GetAll()
    {
        return typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(type => type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false }
                    && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!))
            .Distinct()
            .OrderBy(name => name)
            .ToList();
    }
}
