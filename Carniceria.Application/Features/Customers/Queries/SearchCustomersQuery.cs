using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Queries;

public record SearchCustomersQuery(string? Search) : IRequest<Result<List<CustomerDto>>>;

public class SearchCustomersHandler : IRequestHandler<SearchCustomersQuery, Result<List<CustomerDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerDebtRepository _debts;     // ← faltaba

    public SearchCustomersHandler(
        ICustomerRepository customers,
        ICustomerDebtRepository debts)
    {
        _customers = customers;
        _debts = debts;
    }

    public async Task<Result<List<CustomerDto>>> Handle(SearchCustomersQuery q, CancellationToken ct)
    {
        var list = await _customers.SearchAsync(q.Search, ct);
        var result = new List<CustomerDto>();

        foreach (var c in list)
        {
            var totalDebt = await _debts.GetTotalPendingAsync(c.Id, ct);
            result.Add(new CustomerDto(
                c.Id, c.Name, c.Phone, c.Address,
                c.DiscountPercent, totalDebt, c.Color ?? "#6366f1", c.Emoji, c.Notes));
        }

        return Result.Ok(result);
    }
}