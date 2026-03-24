using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
using System.ComponentModel;

namespace Carniceria.Application.Features.Customers.Queries;

public record GetCustomerDetailQuery(Guid CustomerId) : IRequest<Result<CustomerDetailDto>>;

public class GetCustomerDetailHandler : IRequestHandler<GetCustomerDetailQuery, Result<CustomerDetailDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerDebtRepository _debts;
    private readonly ICustomerProductPriceRepository _prices;
    private readonly IProductRepository _products;

    public GetCustomerDetailHandler(
        ICustomerRepository customers,
        ICustomerDebtRepository debts,
        ICustomerProductPriceRepository prices,
        IProductRepository products)
    {
        _customers = customers;
        _debts = debts;
        _prices = prices;
        _products = products;
    }

    public async Task<Result<CustomerDetailDto>> Handle(
    GetCustomerDetailQuery q, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(q.CustomerId, ct);
        if (customer is null) return Result.Fail<CustomerDetailDto>("Customer not found.");

        var allDebts = await _debts.GetByCustomerAsync(customer.Id, ct);
        var pendingList = allDebts.Where(d => d.Status == DebtStatus.Pending).ToList();
        var totalDebt = pendingList.Sum(d => d.Amount);

        var customPrices = await _prices.GetByCustomerAsync(q.CustomerId, ct);
        var allProducts = await _products.SearchAsync(null, ct);
        var productMap = allProducts.ToDictionary(p => p.Id);

        var priceDtos = customPrices
            .Where(cp => productMap.ContainsKey(cp.ProductId))
            .Select(cp => new CustomerProductPriceDto(
                cp.ProductId,
                productMap[cp.ProductId].Name,
                productMap[cp.ProductId].PricePerUnit,
                cp.CustomPrice))
            .ToList();

        var debtDtos = pendingList.Select(d => new CustomerDebtDto(
            d.Id, d.OrderId, d.OrderFolio, d.Amount,
            d.Status.ToString(), d.CreatedAt, d.PaidAt,
            (int)(DateTime.UtcNow - d.CreatedAt).TotalDays,
            d.Note
        )).ToList();

        return Result.Ok(new CustomerDetailDto(
            customer.Id,
            customer.Name,
            customer.Phone,
            customer.Address,
            customer.DiscountPercent,
            totalDebt,
            customer.Color ?? "#6366f1",  
            customer.Emoji,              
            debtDtos,                     
            priceDtos                     
        ));
    }
}