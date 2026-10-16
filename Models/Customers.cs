using Microsoft.AspNetCore.Identity;

namespace JardinNoemi.Models;

/// <summary>Identidad de acceso (ASP.NET Core Identity).</summary>
public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string FirstName { get; set; } = string.Empty;

    [PersonalData]
    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Perfil comercial del comprador. Existe tanto para invitados como para clientes registrados
/// (vinculado mediante <see cref="UserId"/>), de modo que el panel admin vea a todos los compradores.
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Address> Addresses { get; set; } = [];
    public List<Order> Orders { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class Address
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public required string Street { get; set; }
    public string? Apartment { get; set; }
    public required string City { get; set; }
    public required string Region { get; set; }
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }
}

public class NewsletterSubscriber
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ContactMessage
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
