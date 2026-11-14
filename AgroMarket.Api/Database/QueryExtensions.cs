using AgroMarket.Api.Database.Entities;

namespace AgroMarket.Api.Database;

public static class ProductQueryExtensions
{
    public static IQueryable<Product> NotDeleted(this IQueryable<Product> query)
        => query.Where(p => !p.IsDeleted);

    public static IQueryable<Product> ActiveCatalog(this IQueryable<Product> query)
        => query.Where(p => !p.IsDeleted && p.IsActive);
}
