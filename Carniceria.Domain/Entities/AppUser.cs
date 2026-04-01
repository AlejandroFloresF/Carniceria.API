using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public enum UserRole { Admin, Cashier }

public class AppUser : BaseEntity
{
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    private AppUser() { }

    public static AppUser Create(string username, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Username is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        return new AppUser { Username = username, PasswordHash = passwordHash, Role = role };
    }
}
