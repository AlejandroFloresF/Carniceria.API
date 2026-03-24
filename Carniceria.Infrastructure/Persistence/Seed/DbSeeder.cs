using Carniceria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Seed;
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Products.AnyAsync())
        {
            var products = new List<Product>
        {
            Product.Create("Arrachera",      "Res",       280m, "kg", 15m),
            Product.Create("Costilla res",   "Res",       190m, "kg", 20m),
            Product.Create("Milanesa res",   "Res",       210m, "kg", 12m),
            Product.Create("Bistec",         "Res",       240m, "kg", 18m),
            Product.Create("Chuleta cerdo",  "Cerdo",     160m, "kg", 22m),
            Product.Create("Lomo cerdo",     "Cerdo",     185m, "kg", 10m),
            Product.Create("Costilla cerdo", "Cerdo",     150m, "kg", 25m),
            Product.Create("Chorizo",        "Embutidos", 120m, "kg",  8m),
            Product.Create("Salchicha",      "Embutidos",  90m, "kg",  6m),
            Product.Create("Pollo entero",   "Pollo",      75m, "kg", 30m),
            Product.Create("Pechuga",        "Pollo",     110m, "kg", 20m),
            Product.Create("Muslo/pierna",   "Pollo",      65m, "kg", 25m),
        };
            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();
        }

        var hasGeneral = await db.Customers
        .AnyAsync(c => c.Name == "Público General");

        if (!hasGeneral)
        {
            var general = Customer.Create("Público General", null, null, 0);
            await db.Customers.AddAsync(general);
            await db.SaveChangesAsync();
        }
    }
}
