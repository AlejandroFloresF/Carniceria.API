using Carniceria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<CashierSession> CashierSessions => Set<CashierSession>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InventoryEntry> InventoryEntries => Set<InventoryEntry>();
    public DbSet<WasteRecord> WasteRecords => Set<WasteRecord>();
    public DbSet<StockAlert> StockAlerts => Set<StockAlert>();
    public DbSet<CustomerDebt> CustomerDebts => Set<CustomerDebt>();
    public DbSet<CustomerProductPrice> CustomerProductPrices => Set<CustomerProductPrice>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Product>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100).IsRequired(); e.Property(x => x.PricePerUnit).HasPrecision(18, 2); e.Property(x => x.StockKg).HasPrecision(18, 3); e.HasIndex(x => x.Name); });
        mb.Entity<Order>(e => { e.HasKey(x => x.Id); e.Property(x => x.CustomerName).HasMaxLength(100); e.Property(x => x.DiscountPercent).HasPrecision(5, 2); e.Property(x => x.CashReceived).HasPrecision(18, 2); e.Ignore(x => x.Subtotal); e.Ignore(x => x.DiscountAmount); e.Ignore(x => x.TaxAmount); e.Ignore(x => x.Total); e.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade); });
        mb.Entity<Ticket>(e => { e.HasKey(x => x.Id); e.Property(x => x.Folio).HasMaxLength(20).IsRequired(); e.HasIndex(x => x.Folio).IsUnique(); e.Property(x => x.Subtotal).HasPrecision(18, 2); e.Property(x => x.Total).HasPrecision(18, 2); e.Property(x => x.Change).HasPrecision(18, 2); });
        mb.Entity<CashierSession>(e => { e.HasKey(x => x.Id); e.Property(x => x.CashierName).HasMaxLength(100).IsRequired(); e.Property(x => x.OpeningCash).HasPrecision(18, 2); e.Property(x => x.ClosingCash).HasPrecision(18, 2); });
        mb.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Address).HasMaxLength(100);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.HasIndex(x => x.Name);
            e.Property(x => x.Color).HasMaxLength(7).HasDefaultValue("#6366f1");
            e.Property(x => x.Emoji).HasMaxLength(10);

        });
        mb.Entity<InventoryEntry>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(100);
            e.Property(x => x.QuantityKg).HasPrecision(18, 3);
            e.Property(x => x.CostPerKg).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ProductId, x.EntryDate });
        });

        mb.Entity<WasteRecord>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(100);
            e.Property(x => x.QuantityKg).HasPrecision(18, 3);
            e.HasIndex(x => new { x.ProductId, x.WasteDate });
        });

        mb.Entity<StockAlert>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.MinimumStockKg).HasPrecision(18, 3);
            e.HasIndex(x => x.ProductId).IsUnique();
        });
        mb.Entity<CustomerDebt>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
            e.Property(x => x.OrderFolio).HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.CustomerId);
        });
        mb.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.OrderId);
        });
        mb.Entity<CustomerProductPrice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomPrice).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
        });

        mb.Entity<CustomerDebt>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomerName).HasMaxLength(100);
            e.Property(x => x.OrderFolio).HasMaxLength(20);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.Note).HasMaxLength(300);
        });

    }
}
