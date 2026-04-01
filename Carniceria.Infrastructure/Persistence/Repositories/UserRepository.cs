using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        await _db.AppUsers.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        _db.AppUsers.AnyAsync(ct);
}
