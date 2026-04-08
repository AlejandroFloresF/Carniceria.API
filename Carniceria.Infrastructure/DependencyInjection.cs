using Carniceria.Domain.Interfaces;
using Carniceria.Infrastructure.Persistence;
using Carniceria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Carniceria.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("Default"), b => b.MigrationsAssembly("Carniceria.Infrastructure"))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerDebtRepository, CustomerDebtRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICustomerProductPriceRepository, CustomerProductPriceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
        services.AddScoped<ICashWithdrawalRepository, CashWithdrawalRepository>();
        services.AddScoped<IProductPriceHistoryRepository, ProductPriceHistoryRepository>();
        return services;
    }
}
