using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Inventory.Commands;

public record RegisterEntryCommand(
    Guid ProductId,
    decimal QuantityKg,
    decimal CostPerKg,
    EntrySource Source,
    string? Notes
) : IRequest<Result<InventoryEntryDto>>;

public class RegisterEntryHandler : IRequestHandler<RegisterEntryCommand, Result<InventoryEntryDto>>
{
    private readonly IInventoryRepository _inventory;
    private readonly IProductRepository _products;

    public RegisterEntryHandler(IInventoryRepository inventory, IProductRepository products)
        => (_inventory, _products) = (inventory, products);

    public async Task<Result<InventoryEntryDto>> Handle(RegisterEntryCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Fail<InventoryEntryDto>("Product not found.");

        try
        {
            var entry = InventoryEntry.Create(
                cmd.ProductId, product.Name,
                cmd.QuantityKg, cmd.CostPerKg,
                cmd.Source, cmd.Notes);

            // Aumenta el stock del producto
            product.AddStock(cmd.QuantityKg);

            await _inventory.AddEntryAsync(entry, ct);
            await _inventory.SaveChangesAsync(ct);

            return Result.Ok(new InventoryEntryDto(
                entry.Id, entry.ProductId, entry.ProductName,
                entry.QuantityKg, entry.CostPerKg,
                entry.Source.ToString(), entry.Notes, entry.EntryDate));
        }
        catch (DomainException ex) { return Result.Fail<InventoryEntryDto>(ex.Message); }
    }
}