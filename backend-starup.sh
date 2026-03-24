#!/bin/bash
set -e

# ── Solución ──────────────────────────────────────────────
dotnet new sln -n Carniceria
dotnet new webapi -n Carniceria.API --no-openapi
dotnet new classlib -n Carniceria.Domain
dotnet new classlib -n Carniceria.Application
dotnet new classlib -n Carniceria.Infrastructure

dotnet sln add Carniceria.API
dotnet sln add Carniceria.Domain
dotnet sln add Carniceria.Application
dotnet sln add Carniceria.Infrastructure

dotnet add Carniceria.Application reference Carniceria.Domain
dotnet add Carniceria.Infrastructure reference Carniceria.Application
dotnet add Carniceria.API reference Carniceria.Application
dotnet add Carniceria.API reference Carniceria.Infrastructure

dotnet add Carniceria.API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add Carniceria.API package Swashbuckle.AspNetCore
dotnet add Carniceria.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add Carniceria.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add Carniceria.Application package MediatR
dotnet add Carniceria.Application package FluentValidation
dotnet add Carniceria.Application package FluentValidation.DependencyInjectionExtensions
dotnet add Carniceria.API package Microsoft.EntityFrameworkCore.Design

# ── Domain ────────────────────────────────────────────────
mkdir -p Carniceria.Domain/{Common,Entities,Interfaces}

cat > Carniceria.Domain/Common/BaseEntity.cs << 'EOF'
namespace Carniceria.Domain.Common;
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}
EOF

cat > Carniceria.Domain/Common/Result.cs << 'EOF'
namespace Carniceria.Domain.Common;
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    private Result() { }
    public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Fail(string error) => new() { IsSuccess = false, Error = error };
}
public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Fail<T>(string error) => Result<T>.Fail(error);
}
EOF

cat > Carniceria.Domain/Common/DomainException.cs << 'EOF'
namespace Carniceria.Domain.Common;
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
EOF

cat > Carniceria.Domain/Entities/Product.cs << 'EOF'
using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal PricePerUnit { get; private set; }
    public string Unit { get; private set; } = "kg";
    public decimal StockKg { get; private set; }
    public bool IsActive { get; private set; } = true;
    private Product() { }
    public static Product Create(string name, string category, decimal price, string unit, decimal stock)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (price <= 0) throw new DomainException("Price must be greater than zero.");
        if (stock < 0) throw new DomainException("Stock cannot be negative.");
        return new Product { Name = name, Category = category, PricePerUnit = price, Unit = unit, StockKg = stock };
    }
    public void DeductStock(decimal qty)
    {
        if (qty <= 0) throw new DomainException("Quantity must be positive.");
        if (StockKg < qty) throw new DomainException($"Insufficient stock for {Name}.");
        StockKg -= qty;
        SetUpdated();
    }
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new DomainException("Price must be greater than zero.");
        PricePerUnit = newPrice;
        SetUpdated();
    }
    public void Deactivate() { IsActive = false; SetUpdated(); }
}
EOF

cat > Carniceria.Domain/Entities/Order.cs << 'EOF'
using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public enum OrderStatus { Pending, Completed, Cancelled }
public enum PaymentMethod { Cash, Card, Transfer }
public class Order : BaseEntity
{
    public Guid CashierSessionId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal CashReceived { get; private set; }
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Subtotal => _items.Sum(i => i.UnitPrice * i.Quantity);
    public decimal DiscountAmount => Math.Round(Subtotal * (DiscountPercent / 100), 2);
    public decimal TaxAmount => Math.Round((Subtotal - DiscountAmount) * 0.16m, 2);
    public decimal Total => Subtotal - DiscountAmount + TaxAmount;
    private Order() { }
    public static Order Create(Guid cashierSessionId) => new() { CashierSessionId = cashierSessionId };
    public void AddItem(Product product, decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null) existing.AddQuantity(quantity);
        else _items.Add(OrderItem.Create(Id, product.Id, product.Name, product.PricePerUnit, quantity));
    }
    public void ApplyDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100) throw new DomainException("Discount must be between 0 and 100.");
        DiscountPercent = percent;
    }
    public void Confirm(PaymentMethod method, decimal cashReceived)
    {
        if (!_items.Any()) throw new DomainException("Cannot confirm an empty order.");
        if (method == PaymentMethod.Cash && cashReceived < Total) throw new DomainException("Cash received is less than total.");
        PaymentMethod = method;
        CashReceived = cashReceived;
        Status = OrderStatus.Completed;
        SetUpdated();
    }
}
EOF

cat > Carniceria.Domain/Entities/OrderItem.cs << 'EOF'
using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = "kg";
    public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2);
    private OrderItem() { }
    public static OrderItem Create(Guid orderId, Guid productId, string name, decimal price, decimal qty) =>
        new() { OrderId = orderId, ProductId = productId, ProductName = name, UnitPrice = price, Quantity = qty };
    public void AddQuantity(decimal qty) => Quantity += qty;
}
EOF

cat > Carniceria.Domain/Entities/Ticket.cs << 'EOF'
using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public class Ticket : BaseEntity
{
    public string Folio { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public Guid CashierSessionId { get; private set; }
    public string CashierName { get; private set; } = string.Empty;
    public string ShopName { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public decimal CashReceived { get; private set; }
    public decimal Change { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    private Ticket() { }
    public static Ticket Generate(Order order, string cashierName, string shopName, string folio) =>
        new()
        {
            Folio = folio, OrderId = order.Id, CashierSessionId = order.CashierSessionId,
            CashierName = cashierName, ShopName = shopName, Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount, TaxAmount = order.TaxAmount, Total = order.Total,
            CashReceived = order.CashReceived,
            Change = order.PaymentMethod == PaymentMethod.Cash ? Math.Round(order.CashReceived - order.Total, 2) : 0,
            PaymentMethod = order.PaymentMethod,
        };
}
EOF

cat > Carniceria.Domain/Entities/CashierSession.cs << 'EOF'
using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public enum SessionStatus { Open, Closed }
public class CashierSession : BaseEntity
{
    public string CashierName { get; private set; } = string.Empty;
    public DateTime OpenedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }
    public decimal OpeningCash { get; private set; }
    public decimal ClosingCash { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Open;
    private CashierSession() { }
    public static CashierSession Open(string cashierName, decimal openingCash)
    {
        if (string.IsNullOrWhiteSpace(cashierName)) throw new DomainException("Cashier name is required.");
        if (openingCash < 0) throw new DomainException("Opening cash cannot be negative.");
        return new CashierSession { CashierName = cashierName, OpeningCash = openingCash };
    }
    public void Close(decimal closingCash)
    {
        if (Status == SessionStatus.Closed) throw new DomainException("Session is already closed.");
        ClosingCash = closingCash;
        ClosedAt = DateTime.UtcNow;
        Status = SessionStatus.Closed;
        SetUpdated();
    }
}
EOF

cat > Carniceria.Domain/Interfaces/IProductRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> SearchAsync(string? search, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
EOF

cat > Carniceria.Domain/Interfaces/IOrderRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
}
EOF

cat > Carniceria.Domain/Interfaces/ITicketRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> GetByFolioAsync(string folio, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task<int> GetNextFolioNumberAsync(CancellationToken ct = default);
}
EOF

cat > Carniceria.Domain/Interfaces/ISessionRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface ISessionRepository
{
    Task<CashierSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CashierSession?> GetOpenSessionAsync(string cashierName, CancellationToken ct = default);
    Task AddAsync(CashierSession session, CancellationToken ct = default);
    Task<List<Order>> GetOrdersAsync(Guid sessionId, CancellationToken ct = default);
}
EOF

# ── Application ───────────────────────────────────────────
mkdir -p Carniceria.Application/{Common,"Features/Products/Queries","Features/Orders/Commands","Features/Sessions/Commands","Features/Sessions/Queries"}

cat > Carniceria.Application/Common/DTOs.cs << 'EOF'
using Carniceria.Domain.Entities;
namespace Carniceria.Application.Common;
public record ProductDto(Guid Id, string Name, string Category, decimal PricePerUnit, string Unit, decimal StockKg);
public record OrderItemInputDto(Guid ProductId, decimal Quantity);
public record TicketItemDto(Guid ProductId, string ProductName, decimal Quantity, string Unit, decimal UnitPrice, decimal Total);
public record TicketDto(Guid Id, string Folio, Guid OrderId, DateTime IssuedAt, string CashierName, string ShopName, List<TicketItemDto> Items, decimal Subtotal, decimal DiscountAmount, decimal TaxAmount, decimal Total, decimal CashReceived, decimal Change, PaymentMethod PaymentMethod);
public record CashierSessionDto(Guid SessionId, string CashierName, DateTime OpenedAt);
public record SessionSummaryDto(Guid SessionId, string CashierName, DateTime OpenedAt, DateTime? ClosedAt, int TotalOrders, decimal TotalSales, decimal TotalCash, decimal TotalCard, decimal TotalTransfer, decimal TotalDiscounts, decimal OpeningCash, decimal ExpectedCash);
EOF

cat > "Carniceria.Application/Features/Products/Queries/SearchProductsQuery.cs" << 'EOF'
using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Products.Queries;
public record SearchProductsQuery(string? Search) : IRequest<Result<List<ProductDto>>>;
public class SearchProductsHandler : IRequestHandler<SearchProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductRepository _products;
    public SearchProductsHandler(IProductRepository products) => _products = products;
    public async Task<Result<List<ProductDto>>> Handle(SearchProductsQuery q, CancellationToken ct)
    {
        var products = await _products.SearchAsync(q.Search, ct);
        var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Category, p.PricePerUnit, p.Unit, p.StockKg)).ToList();
        return Result.Ok(dtos);
    }
}
EOF

cat > "Carniceria.Application/Features/Orders/Commands/CreateOrderCommand.cs" << 'EOF'
using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Orders.Commands;
public record CreateOrderCommand(Guid CashierSessionId, List<OrderItemInputDto> Items, decimal DiscountPercent, PaymentMethod PaymentMethod, decimal CashReceived) : IRequest<Result<TicketDto>>;
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<TicketDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ITicketRepository _tickets;
    private readonly ISessionRepository _sessions;
    public CreateOrderHandler(IOrderRepository orders, IProductRepository products, ITicketRepository tickets, ISessionRepository sessions)
    { _orders = orders; _products = products; _tickets = tickets; _sessions = sessions; }
    public async Task<Result<TicketDto>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(cmd.CashierSessionId, ct);
        if (session is null) return Result.Fail<TicketDto>("Cashier session not found.");
        if (session.Status == SessionStatus.Closed) return Result.Fail<TicketDto>("Cannot create order on a closed session.");
        var order = Order.Create(cmd.CashierSessionId);
        foreach (var item in cmd.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null) return Result.Fail<TicketDto>($"Product {item.ProductId} not found.");
            if (product.StockKg < item.Quantity) return Result.Fail<TicketDto>($"Insufficient stock for {product.Name}.");
            order.AddItem(product, item.Quantity);
            product.DeductStock(item.Quantity);
        }
        order.ApplyDiscount(cmd.DiscountPercent);
        try { order.Confirm(cmd.PaymentMethod, cmd.CashReceived); }
        catch (DomainException ex) { return Result.Fail<TicketDto>(ex.Message); }
        var folioNumber = await _tickets.GetNextFolioNumberAsync(ct);
        var folio = folioNumber.ToString().PadLeft(5, '0');
        var ticket = Ticket.Generate(order, session.CashierName, "Carniceria Don Memo", folio);
        await _orders.AddAsync(order, ct);
        await _tickets.AddAsync(ticket, ct);
        var dto = new TicketDto(ticket.Id, ticket.Folio, ticket.OrderId, ticket.CreatedAt, ticket.CashierName, ticket.ShopName,
            order.Items.Select(i => new TicketItemDto(i.ProductId, i.ProductName, i.Quantity, i.Unit, i.UnitPrice, i.LineTotal)).ToList(),
            ticket.Subtotal, ticket.DiscountAmount, ticket.TaxAmount, ticket.Total, ticket.CashReceived, ticket.Change, ticket.PaymentMethod);
        return Result.Ok(dto);
    }
}
EOF

cat > "Carniceria.Application/Features/Sessions/Commands/OpenSessionCommand.cs" << 'EOF'
using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Sessions.Commands;
public record OpenSessionCommand(string CashierName, decimal OpeningCash) : IRequest<Result<CashierSessionDto>>;
public class OpenSessionHandler : IRequestHandler<OpenSessionCommand, Result<CashierSessionDto>>
{
    private readonly ISessionRepository _sessions;
    public OpenSessionHandler(ISessionRepository sessions) => _sessions = sessions;
    public async Task<Result<CashierSessionDto>> Handle(OpenSessionCommand cmd, CancellationToken ct)
    {
        var existing = await _sessions.GetOpenSessionAsync(cmd.CashierName, ct);
        if (existing is not null) return Result.Fail<CashierSessionDto>($"{cmd.CashierName} already has an open session.");
        var session = CashierSession.Open(cmd.CashierName, cmd.OpeningCash);
        await _sessions.AddAsync(session, ct);
        return Result.Ok(new CashierSessionDto(session.Id, session.CashierName, session.OpenedAt));
    }
}
EOF

cat > "Carniceria.Application/Features/Sessions/Commands/CloseSessionCommand.cs" << 'EOF'
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Sessions.Commands;
public record CloseSessionCommand(Guid SessionId, decimal ClosingCash) : IRequest<Result<bool>>;
public class CloseSessionHandler : IRequestHandler<CloseSessionCommand, Result<bool>>
{
    private readonly ISessionRepository _sessions;
    public CloseSessionHandler(ISessionRepository sessions) => _sessions = sessions;
    public async Task<Result<bool>> Handle(CloseSessionCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(cmd.SessionId, ct);
        if (session is null) return Result.Fail<bool>("Session not found.");
        try { session.Close(cmd.ClosingCash); }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
        return Result.Ok(true);
    }
}
EOF

cat > "Carniceria.Application/Features/Sessions/Queries/GetSessionSummaryQuery.cs" << 'EOF'
using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Sessions.Queries;
public record GetSessionSummaryQuery(Guid SessionId) : IRequest<Result<SessionSummaryDto>>;
public class GetSessionSummaryHandler : IRequestHandler<GetSessionSummaryQuery, Result<SessionSummaryDto>>
{
    private readonly ISessionRepository _sessions;
    public GetSessionSummaryHandler(ISessionRepository sessions) => _sessions = sessions;
    public async Task<Result<SessionSummaryDto>> Handle(GetSessionSummaryQuery q, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(q.SessionId, ct);
        if (session is null) return Result.Fail<SessionSummaryDto>("Session not found.");
        var orders = await _sessions.GetOrdersAsync(q.SessionId, ct);
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        return Result.Ok(new SessionSummaryDto(
            session.Id, session.CashierName, session.OpenedAt, session.ClosedAt,
            completed.Count, completed.Sum(o => o.Total),
            completed.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total),
            completed.Where(o => o.PaymentMethod == PaymentMethod.Card).Sum(o => o.Total),
            completed.Where(o => o.PaymentMethod == PaymentMethod.Transfer).Sum(o => o.Total),
            completed.Sum(o => o.DiscountAmount), session.OpeningCash,
            session.OpeningCash + completed.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total)));
    }
}
EOF

# ── Infrastructure ────────────────────────────────────────
mkdir -p Carniceria.Infrastructure/{Persistence/Repositories,Persistence/Seed}

cat > Carniceria.Infrastructure/Persistence/AppDbContext.cs << 'EOF'
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
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Product>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100).IsRequired(); e.Property(x => x.PricePerUnit).HasPrecision(18, 2); e.Property(x => x.StockKg).HasPrecision(18, 3); e.HasIndex(x => x.Name); });
        mb.Entity<Order>(e => { e.HasKey(x => x.Id); e.Property(x => x.DiscountPercent).HasPrecision(5, 2); e.Property(x => x.CashReceived).HasPrecision(18, 2); e.Ignore(x => x.Subtotal); e.Ignore(x => x.DiscountAmount); e.Ignore(x => x.TaxAmount); e.Ignore(x => x.Total); e.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade); });
        mb.Entity<OrderItem>(e => { e.HasKey(x => x.Id); e.Property(x => x.UnitPrice).HasPrecision(18, 2); e.Property(x => x.Quantity).HasPrecision(18, 3); e.Ignore(x => x.LineTotal); });
        mb.Entity<Ticket>(e => { e.HasKey(x => x.Id); e.Property(x => x.Folio).HasMaxLength(20).IsRequired(); e.HasIndex(x => x.Folio).IsUnique(); e.Property(x => x.Subtotal).HasPrecision(18, 2); e.Property(x => x.Total).HasPrecision(18, 2); e.Property(x => x.Change).HasPrecision(18, 2); });
        mb.Entity<CashierSession>(e => { e.HasKey(x => x.Id); e.Property(x => x.CashierName).HasMaxLength(100).IsRequired(); e.Property(x => x.OpeningCash).HasPrecision(18, 2); e.Property(x => x.ClosingCash).HasPrecision(18, 2); });
    }
}
EOF

cat > Carniceria.Infrastructure/Persistence/Repositories/ProductRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);
    public Task<List<Product>> SearchAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Products.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.ToLower().Contains(search.ToLower()));
        return q.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);
    }
    public async Task AddAsync(Product product, CancellationToken ct = default) { await _db.Products.AddAsync(product, ct); await _db.SaveChangesAsync(ct); }
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => _db.Products.AnyAsync(p => p.Id == id, ct);
}
EOF

cat > Carniceria.Infrastructure/Persistence/Repositories/OrderRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    public async Task AddAsync(Order order, CancellationToken ct = default) { await _db.Orders.AddAsync(order, ct); await _db.SaveChangesAsync(ct); }
}
EOF

cat > Carniceria.Infrastructure/Persistence/Repositories/TicketRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;
    public TicketRepository(AppDbContext db) => _db = db;
    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task<Ticket?> GetByFolioAsync(string folio, CancellationToken ct = default) => _db.Tickets.FirstOrDefaultAsync(t => t.Folio == folio, ct);
    public async Task AddAsync(Ticket ticket, CancellationToken ct = default) { await _db.Tickets.AddAsync(ticket, ct); await _db.SaveChangesAsync(ct); }
    public async Task<int> GetNextFolioNumberAsync(CancellationToken ct = default) { var count = await _db.Tickets.CountAsync(ct); return count + 1; }
}
EOF

cat > Carniceria.Infrastructure/Persistence/Repositories/SessionRepository.cs << 'EOF'
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _db;
    public SessionRepository(AppDbContext db) => _db = db;
    public Task<CashierSession?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.CashierSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task<CashierSession?> GetOpenSessionAsync(string cashierName, CancellationToken ct = default) => _db.CashierSessions.FirstOrDefaultAsync(s => s.CashierName == cashierName && s.Status == SessionStatus.Open, ct);
    public async Task AddAsync(CashierSession session, CancellationToken ct = default) { await _db.CashierSessions.AddAsync(session, ct); await _db.SaveChangesAsync(ct); }
    public Task<List<Order>> GetOrdersAsync(Guid sessionId, CancellationToken ct = default) => _db.Orders.Include(o => o.Items).Where(o => o.CashierSessionId == sessionId).ToListAsync(ct);
}
EOF

cat > Carniceria.Infrastructure/Persistence/Seed/DbSeeder.cs << 'EOF'
using Carniceria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Seed;
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Products.AnyAsync()) return;
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
}
EOF

cat > Carniceria.Infrastructure/DependencyInjection.cs << 'EOF'
using Carniceria.Domain.Interfaces;
using Carniceria.Infrastructure.Persistence;
using Carniceria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Carniceria.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"), b => b.MigrationsAssembly("Carniceria.Infrastructure")));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        return services;
    }
}
EOF

# ── API ───────────────────────────────────────────────────
mkdir -p Carniceria.API/Controllers

cat > Carniceria.API/Controllers/ProductsController.cs << 'EOF'
using Carniceria.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Carniceria.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;
    public ProductsController(ISender mediator) => _mediator = mediator;
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchProductsQuery(search));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
EOF

cat > Carniceria.API/Controllers/OrdersController.cs << 'EOF'
using Carniceria.Application.Common;
using Carniceria.Application.Features.Orders.Commands;
using Carniceria.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Carniceria.API.Controllers;
public record CreateOrderRequest(Guid CashierSessionId, List<OrderItemInputDto> Items, decimal DiscountPercent, PaymentMethod PaymentMethod, decimal CashReceived);
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;
    public OrdersController(ISender mediator) => _mediator = mediator;
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var result = await _mediator.Send(new CreateOrderCommand(req.CashierSessionId, req.Items, req.DiscountPercent, req.PaymentMethod, req.CashReceived));
        return result.IsSuccess ? CreatedAtAction(nameof(Create), result.Value) : BadRequest(new { error = result.Error });
    }
}
EOF

cat > Carniceria.API/Controllers/SessionsController.cs << 'EOF'
using Carniceria.Application.Features.Sessions.Commands;
using Carniceria.Application.Features.Sessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Carniceria.API.Controllers;
public record OpenSessionRequest(string CashierName, decimal OpeningCash);
public record CloseSessionRequest(decimal ClosingCash);
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISender _mediator;
    public SessionsController(ISender mediator) => _mediator = mediator;
    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenSessionRequest req)
    {
        var result = await _mediator.Send(new OpenSessionCommand(req.CashierName, req.OpeningCash));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseSessionRequest req)
    {
        var result = await _mediator.Send(new CloseSessionCommand(id, req.ClosingCash));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> Summary(Guid id)
    {
        var result = await _mediator.Send(new GetSessionSummaryQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
EOF

cat > Carniceria.API/appsettings.json << 'EOF'
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=carniceria_db;Username=postgres;Password=postgres"
  },
  "AllowedHosts": "*",
  "Cors": {
    "Origins": ["http://localhost:5173"]
  }
}
EOF

cat > Carniceria.API/appsettings.Development.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
EOF

cat > Carniceria.API/Program.cs << 'EOF'
using System.Reflection;
using Carniceria.Infrastructure;
using Carniceria.Infrastructure.Persistence;
using Carniceria.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.Load("Carniceria.Application")));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                           ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
EOF

echo ""
echo "✅ Proyecto creado correctamente."
echo ""
echo "Próximos pasos:"
echo "  1. Asegúrate de tener PostgreSQL corriendo en el puerto 5432"
echo "  2. cd Carniceria.API && dotnet run"
echo "  3. Abre http://localhost:5000/swagger"