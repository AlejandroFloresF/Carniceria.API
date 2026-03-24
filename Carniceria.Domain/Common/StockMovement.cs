namespace Carniceria.Domain.Common;

public record StockMovement(
    DateTime Date,
    string Type,
    decimal QuantityKg,
    decimal StockAfter,
    string? Reference
);