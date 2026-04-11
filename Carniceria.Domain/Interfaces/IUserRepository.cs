using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default);
    Task<AppUser?> GetByPasskeyCredentialIdAsync(byte[] credentialId, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, Guid excludeId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid excludeId, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
