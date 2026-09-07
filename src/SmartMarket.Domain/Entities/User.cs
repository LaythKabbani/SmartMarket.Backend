using SmartMarket.Domain.Enums;

namespace SmartMarket.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; } = UserRole.Customer; // SuperAdmin, Merchant, Customer

    // Navigation Properties
    public ICollection<Store> Stores { get; private set; } = new List<Store>();
    public ICollection<Order> Orders { get; private set; } = new List<Order>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { }

    public User(string fullName, string email, string passwordHash, UserRole role = UserRole.Customer)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void Update(string fullName, UserRole role)
    {
        FullName = fullName;
        Role = role;
    }
}