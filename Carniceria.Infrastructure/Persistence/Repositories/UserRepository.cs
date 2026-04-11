using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), ct);

    public Task<AppUser?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default) =>
        _db.AppUsers.FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    public Task<AppUser?> GetByPasskeyCredentialIdAsync(byte[] credentialId, CancellationToken ct = default) =>
        _db.AppUsers.FirstOrDefaultAsync(u => u.PasskeyCredentialId != null && u.PasskeyCredentialId == credentialId, ct);

    public Task<bool> UsernameExistsAsync(string username, Guid excludeId, CancellationToken ct = default) =>
        _db.AppUsers.AnyAsync(u => u.Username == username.Trim().ToLower() && u.Id != excludeId, ct);

    public Task<bool> EmailExistsAsync(string email, Guid excludeId, CancellationToken ct = default) =>
        _db.AppUsers.AnyAsync(u => u.Email == email.Trim().ToLower() && u.Id != excludeId, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        await _db.AppUsers.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        _db.AppUsers.AnyAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
