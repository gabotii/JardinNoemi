using JardinNoemi.Data;
using JardinNoemi.DTOs;
using JardinNoemi.Models;
using Microsoft.EntityFrameworkCore;

namespace JardinNoemi.Services;

public interface INewsletterService
{
    Task<OperationResult> SubscribeAsync(string email, CancellationToken ct = default);
}

public sealed class NewsletterService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<NewsletterService> logger) : INewsletterService
{
    public async Task<OperationResult> SubscribeAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Idempotente: suscribirse dos veces no es un error ni revela si el correo ya existía.
        if (!await db.NewsletterSubscribers.AnyAsync(s => s.Email == normalized, ct))
        {
            db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = normalized });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Carrera con otra suscripción idéntica: el índice único ya la protege.
                logger.LogDebug(ex, "Suscripción duplicada ignorada.");
            }
        }

        return OperationResult.Ok;
    }
}

public interface IContactService
{
    Task<OperationResult> SendAsync(ContactForm form, CancellationToken ct = default);
}

public sealed class ContactService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<ContactService> logger) : IContactService
{
    public async Task<OperationResult> SendAsync(ContactForm form, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.ContactMessages.Add(new ContactMessage
            {
                Name = form.Name.Trim(),
                Email = form.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(),
                Message = form.Message.Trim()
            });
            await db.SaveChangesAsync(ct);
            return OperationResult.Ok;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "No se pudo guardar el mensaje de contacto.");
            return OperationResult.Fail("No pudimos enviar tu mensaje. Inténtalo nuevamente o escríbenos por WhatsApp.");
        }
    }
}

public interface ICustomerService
{
    Task<CustomerProfileDto?> GetProfileAsync(string userId, CancellationToken ct = default);
    Task<OperationResult> UpdateProfileAsync(string userId, ProfileForm form, CancellationToken ct = default);
    Task EnsureCustomerAsync(ApplicationUser user, CancellationToken ct = default);
}

public sealed class CustomerService(IDbContextFactory<ApplicationDbContext> dbFactory) : ICustomerService
{
    public async Task<CustomerProfileDto?> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        var customer = await db.Customers.AsNoTracking()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
        var address = customer?.Addresses.OrderByDescending(a => a.IsDefault).FirstOrDefault();

        return new CustomerProfileDto(
            user.Email ?? string.Empty,
            customer?.FirstName ?? user.FirstName,
            customer?.LastName ?? user.LastName,
            customer?.Phone ?? user.PhoneNumber,
            address?.Street,
            address?.Apartment,
            address?.City,
            address?.Region,
            address?.PostalCode);
    }

    public async Task<OperationResult> UpdateProfileAsync(string userId, ProfileForm form, CancellationToken ct = default)
    {
        if (form.Region is not null && form.Region.Length > 0 && !Regions.IsValid(form.Region))
        {
            return OperationResult.Fail("Selecciona una región válida.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return OperationResult.Fail("No encontramos tu cuenta.");

        user.FirstName = form.FirstName.Trim();
        user.LastName = form.LastName.Trim();
        user.PhoneNumber = form.Phone?.Trim();

        var customer = await db.Customers.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (customer is null)
        {
            customer = new Customer { UserId = userId, Email = user.Email!, FirstName = user.FirstName, LastName = user.LastName };
            db.Customers.Add(customer);
        }

        customer.FirstName = user.FirstName;
        customer.LastName = user.LastName;
        customer.Phone = user.PhoneNumber;

        var hasAddress = !string.IsNullOrWhiteSpace(form.Street) && !string.IsNullOrWhiteSpace(form.City) && Regions.IsValid(form.Region);
        var address = customer.Addresses.OrderByDescending(a => a.IsDefault).FirstOrDefault();
        if (hasAddress)
        {
            if (address is null)
            {
                address = new Address { Street = "", City = "", Region = "", IsDefault = true };
                customer.Addresses.Add(address);
            }
            address.Street = form.Street!.Trim();
            address.Apartment = form.Apartment?.Trim();
            address.City = form.City!.Trim();
            address.Region = form.Region!;
            address.PostalCode = form.PostalCode?.Trim();
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok;
    }

    public async Task EnsureCustomerAsync(ApplicationUser user, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Customers.AnyAsync(c => c.UserId == user.Id, ct)) return;
        db.Customers.Add(new Customer
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.PhoneNumber
        });
        await db.SaveChangesAsync(ct);
    }
}
