using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
}
