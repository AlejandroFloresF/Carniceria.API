using Carniceria.Domain.Entities;
using Carniceria.Application.Common;
namespace Carniceria.Application.Common;
public record ProductDto(Guid Id, string Name, string Category, decimal PricePerUnit, string Unit, decimal StockKg);
public record OrderItemInputDto(Guid ProductId, decimal Quantity);
public record TicketItemDto(Guid ProductId, string ProductName, decimal Quantity, string Unit, decimal UnitPrice, decimal Total);
public record CashierSessionDto(Guid SessionId, string CashierName, DateTime OpenedAt, decimal openingCash);
public record SessionSummaryDto(Guid SessionId, string CashierName, DateTime OpenedAt, DateTime? ClosedAt, int TotalOrders, decimal TotalSales, decimal TotalCash, decimal TotalCard, decimal TotalTransfer, decimal TotalDiscounts, decimal OpeningCash, decimal ExpectedCash, decimal TotalDebtPayments);
public record CustomerDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    decimal DiscountPercent,
    decimal TotalDebt,
    string Color,
    string? Emoji
);
public record CustomerProductPriceDto(
    Guid ProductId,
    string ProductName,
    decimal GeneralPrice,
    decimal CustomPrice
);

public record CustomerDebtDto(
    Guid Id,
    Guid OrderId,
    string OrderFolio,
    decimal Amount,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    int DaysPending,
    string? Note
);

public record CustomerDetailDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    decimal DiscountPercent,
    decimal TotalDebt,
    string Color,
    string? Emoji,
    List<CustomerDebtDto> PendingDebts,
    List<CustomerProductPriceDto> CustomPrices
);

public record DashboardFiltersDto(
    DateTime From,
    DateTime To,
    Guid? SessionId
);

public record DailySalesDto(DateTime Date, decimal Total, int OrderCount);

public record TopProductDto(
    string ProductName,
    decimal TotalKg,
    decimal TotalRevenue,
    int OrderCount
);

public record TopCustomerDto(
    string CustomerName,
    decimal TotalSpent,
    int OrderCount,
    DateTime LastPurchase
);

public record PeriodComparisonDto(
    decimal CurrentTotal,
    decimal PreviousTotal,
    decimal ChangePercent,
    int CurrentOrders,
    int PreviousOrders
);

public record DashboardDto(
    decimal TotalSales,
    int TotalOrders,
    decimal AverageTicket,
    decimal TotalDiscounts,
    decimal TotalCash,
    decimal TotalCard,
    decimal TotalTransfer,
    List<DailySalesDto> SalesByDay,
    List<TopProductDto> TopProducts,
    List<TopCustomerDto> TopCustomers,
    PeriodComparisonDto Comparison,
    DateTime From,
    DateTime To,
    Guid? SessionId,
    decimal TotalCreditSales,
    decimal TotalPendingDebt,
    decimal MaxTicket,
    int UniqueCustomers,
    double AvgItemsPerOrder,
    int? PeakHour,
    string? BestSellingProductName,
    decimal BestSellingProductRevenue,
    decimal TotalDebtPayments
);

public record TicketDto(
    Guid Id,
    string Folio,
    Guid OrderId,
    DateTime IssuedAt,
    string CashierName,
    string ShopName,
    List<TicketItemDto> Items,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    decimal CashReceived,
    decimal Change,
    PaymentMethod PaymentMethod,
    string? CustomerName
);
// ── Gastos ───────────────────────────────────────────────
public record ScheduledExpenseDto(
    Guid Id, string Name, string? Description, decimal Amount,
    string Category, string Recurrence, DateTime NextDueDate,
    int AlertDaysBefore, bool IsActive, bool IsOverdue, bool IsUpcoming
);
public record ExpenseRequestDto(
    Guid Id, string Description, decimal Amount, string Category,
    string Status, string RequestedBy, Guid? SessionId, Guid? ScheduledExpenseId,
    string? ReviewedBy, string? DenyReason, string? Notes,
    DateTime RequestedAt, DateTime? ReviewedAt
);
public record ExpenseNotificationItemDto(
    string Type, string Title, string Subtitle, Guid? ReferenceId, string Severity
);
public record ExpenseNotificationsDto(
    int PendingRequestsCount, int UpcomingExpensesCount,
    List<ExpenseNotificationItemDto> Items
);

// ── Inventario ───────────────────────────────────────────

public record InventoryEntryDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal QuantityKg,
    decimal CostPerKg,
    string Source,
    string? Notes,
    DateTime EntryDate
);

public record WasteRecordDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal QuantityKg,
    string Reason,
    string? Notes,
    DateTime WasteDate
);

public record StockAlertDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal CurrentStockKg,
    decimal MinimumStockKg,
    bool IsBelowMinimum
);

public record StockMovementDto(
    DateTime Date,
    string Type,
    decimal QuantityKg,
    decimal StockAfter,
    string? Reference
);

public record StockStatusDto(
    Guid ProductId,
    string ProductName,
    string Category,
    string Unit,
    decimal CurrentStockKg,
    decimal MinimumStockKg,
    bool IsBelowMinimum,
    decimal TotalSoldLast7Days,
    decimal TotalWasteLast7Days,
    decimal AverageDailySales,
    decimal SalePrice
);




