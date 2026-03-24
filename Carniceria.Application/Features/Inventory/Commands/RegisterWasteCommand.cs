using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Inventory.Commands;

public record RegisterWasteCommand(
    Guid ProductId,
    decimal QuantityKg,
    WasteReason Reason,
    string? Notes
) : IRequest<Result<WasteRecordDto>>;

public class RegisterWasteHandler : IRequestHandler<RegisterWasteCommand, Result<WasteRecordDto>>
{
    private readonly IInventoryRepository _inventory;
    private readonly IProductRepository _products;

    public RegisterWasteHandler(IInventoryRepository inventory, IProductRepository products)
        => (_inventory, _products) = (inventory, products);

    public async Task<Result<WasteRecordDto>> Handle(RegisterWasteCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Fail<WasteRecordDto>("Product not found.");

        if (product.StockKg < cmd.QuantityKg)
            return Result.Fail<WasteRecordDto>(
                $"Cannot register waste of {cmd.QuantityKg}kg — only {product.StockKg}kg in stock.");

        try
        {
            var waste = WasteRecord.Create(
                cmd.ProductId, product.Name,
                cmd.QuantityKg, cmd.Reason, cmd.Notes);

            product.DeductStock(cmd.QuantityKg);

            await _inventory.AddWasteAsync(waste, ct);
            await _inventory.SaveChangesAsync(ct);

            return Result.Ok(new WasteRecordDto(
                waste.Id, waste.ProductId, waste.ProductName,
                waste.QuantityKg, waste.Reason.ToString(),
                waste.Notes, waste.WasteDate));
        }
        catch (DomainException ex) { return Result.Fail<WasteRecordDto>(ex.Message); }
    }
}