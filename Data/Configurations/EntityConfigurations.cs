using JardinNoemi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JardinNoemi.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(c => c.Name).HasMaxLength(80);
        b.Property(c => c.Slug).HasMaxLength(100);
        b.Property(c => c.Description).HasMaxLength(400);
        b.Property(c => c.ImageUrl).HasMaxLength(400);
        b.HasIndex(c => c.Slug).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(p => p.Name).HasMaxLength(120);
        b.Property(p => p.Slug).HasMaxLength(140);
        b.Property(p => p.Sku).HasMaxLength(40);
        b.Property(p => p.ShortDescription).HasMaxLength(240);
        b.Property(p => p.Description).HasMaxLength(4000);
        b.Property(p => p.Light).HasMaxLength(80);
        b.Property(p => p.Watering).HasMaxLength(80);
        b.Property(p => p.Size).HasMaxLength(80);
        b.Property(p => p.CareLevel).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(p => p.Slug).IsUnique();
        b.HasIndex(p => p.Sku).IsUnique();
        b.HasIndex(p => new { p.IsActive, p.CategoryId });
        b.HasIndex(p => p.IsFeatured);

        b.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Ignore(p => p.IsOnSale);

        b.ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "\"Stock\" >= 0"));
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.Property(i => i.Url).HasMaxLength(400);
        b.Property(i => i.AltText).HasMaxLength(200);
        b.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.Property(c => c.UserId).HasMaxLength(450);
        b.HasIndex(c => c.UserId);
        b.HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        b.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(o => o.OrderNumber).HasMaxLength(24);
        b.Property(o => o.AccessToken).HasMaxLength(64);
        b.Property(o => o.CustomerName).HasMaxLength(160);
        b.Property(o => o.Email).HasMaxLength(256);
        b.Property(o => o.Phone).HasMaxLength(32);
        b.Property(o => o.Notes).HasMaxLength(500);
        b.Property(o => o.UserId).HasMaxLength(450);
        b.Property(o => o.PaymentProvider).HasMaxLength(60);
        b.Property(o => o.PaymentReference).HasMaxLength(120);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.DeliveryMethod).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(o => o.OrderNumber).IsUnique();
        b.HasIndex(o => o.UserId);
        b.HasIndex(o => o.CreatedAt);

        b.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(x => x.Street).HasMaxLength(160).HasColumnName("ShipStreet");
            a.Property(x => x.Apartment).HasMaxLength(60).HasColumnName("ShipApartment");
            a.Property(x => x.City).HasMaxLength(80).HasColumnName("ShipCity");
            a.Property(x => x.Region).HasMaxLength(80).HasColumnName("ShipRegion");
            a.Property(x => x.PostalCode).HasMaxLength(16).HasColumnName("ShipPostalCode");
        });

        b.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(i => i.ProductName).HasMaxLength(120);
        b.Property(i => i.ProductSlug).HasMaxLength(140);
        b.Property(i => i.Sku).HasMaxLength(40);
        b.Property(i => i.ImageUrl).HasMaxLength(400);
        b.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.Property(c => c.Email).HasMaxLength(256);
        b.Property(c => c.FirstName).HasMaxLength(80);
        b.Property(c => c.LastName).HasMaxLength(80);
        b.Property(c => c.Phone).HasMaxLength(32);
        b.Property(c => c.UserId).HasMaxLength(450);
        b.HasIndex(c => c.UserId).IsUnique();
        b.HasIndex(c => c.Email);
        b.Ignore(c => c.FullName);

        b.HasMany(c => c.Addresses)
            .WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.Property(a => a.Street).HasMaxLength(160);
        b.Property(a => a.Apartment).HasMaxLength(60);
        b.Property(a => a.City).HasMaxLength(80);
        b.Property(a => a.Region).HasMaxLength(80);
        b.Property(a => a.PostalCode).HasMaxLength(16);
    }
}

internal sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> b)
    {
        b.Property(n => n.Email).HasMaxLength(256);
        b.HasIndex(n => n.Email).IsUnique();
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.Property(m => m.Name).HasMaxLength(120);
        b.Property(m => m.Email).HasMaxLength(256);
        b.Property(m => m.Phone).HasMaxLength(32);
        b.Property(m => m.Message).HasMaxLength(2000);
    }
}

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.FirstName).HasMaxLength(80);
        b.Property(u => u.LastName).HasMaxLength(80);
    }
}
