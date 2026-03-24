using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Inventory.Queries;

public record GetMovementsQuery(Guid ProductId, DateTime? From, DateTime? To)
    : IRequest<Result<List<StockMovementDto>>>;

public class GetMovementsHandler : IRequestHandler<GetMovementsQuery, Result<List<StockMovementDto>>>
{
    private readonly IInventoryRepository _inventory;
    public GetMovementsHandler(IInventoryRepository inventory) => _inventory = inventory;

    public async Task<Result<List<StockMovementDto>>> Handle(
        GetMovementsQuery q, CancellationToken ct)
    {
        var movements = await _inventory.GetMovementsAsync(q.ProductId, q.From, q.To, ct);

        var dtos = movements.Select(m => new StockMovementDto(
            m.Date, m.Type, m.QuantityKg, m.StockAfter, m.Reference
        )).ToList();

        return Result.Ok(dtos);
    }
}