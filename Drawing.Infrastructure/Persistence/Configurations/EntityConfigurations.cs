using Drawing.Domain.Common;
using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drawing.Infrastructure.Persistence.Configurations;
public abstract class BaseConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
public sealed class ArtistProfileConfiguration : BaseConfiguration<ArtistProfile>
{
    public override void Configure(EntityTypeBuilder<ArtistProfile> builder)
    {
        base.Configure(builder);

        builder.ToTable("ArtistProfiles");

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Biography)
            .HasMaxLength(5000);

        builder.Property(x => x.ProfileImageUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.Status);
    }
}
public sealed class ArtworkConfiguration : BaseConfiguration<Artwork>
{
    public override void Configure(EntityTypeBuilder<Artwork> builder)
    {
        base.Configure(builder);

        builder.ToTable("Artworks", table =>
        {
            table.HasCheckConstraint(
                "CK_Artworks_Price",
                "\"Price\" > 0");

            table.HasCheckConstraint(
                "CK_Artworks_Width",
                "\"WidthCm\" > 0");

            table.HasCheckConstraint(
                "CK_Artworks_Height",
                "\"HeightCm\" > 0");
        });

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(5000);

        builder.Property(x => x.Price)
            .HasPrecision(12, 2);

        builder.Property(x => x.WidthCm)
            .HasPrecision(8, 2);

        builder.Property(x => x.HeightCm)
            .HasPrecision(8, 2);

        builder.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LocationDetails)
            .HasMaxLength(1000);

        builder.Property(x => x.ModerationReason)
            .HasMaxLength(2000);

        builder.Property(x => x.ModerationStatus)
            .HasConversion<int>();

        builder.Property(x => x.SaleStatus)
            .HasConversion<int>();

        builder.HasOne(x => x.ArtistProfile)
            .WithMany(x => x.Artworks)
            .HasForeignKey(x => x.ArtistProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Technique>()
            .WithMany()
            .HasForeignKey(x => x.TechniqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ArtworkStyle>()
            .WithMany()
            .HasForeignKey(x => x.StyleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ArtistProfileId);
        builder.HasIndex(x => x.ModerationStatus);
        builder.HasIndex(x => x.SaleStatus);
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => x.CreatedAt);
    }
}
public sealed class ArtworkImageConfiguration : BaseConfiguration<ArtworkImage>
{
    public override void Configure(EntityTypeBuilder<ArtworkImage> builder)
    {
        base.Configure(builder);

        builder.ToTable("ArtworkImages");

        builder.Property(x => x.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Artwork)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ArtworkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ArtworkId);

        builder.HasIndex(x => x.ArtworkId)
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE");
    }
}
public sealed class TechniqueConfiguration : BaseConfiguration<Technique>
{
    public override void Configure(EntityTypeBuilder<Technique> builder)
    {
        base.Configure(builder);
        builder.ToTable("Techniques");

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
public sealed class MaterialConfiguration : BaseConfiguration<Material>
{
    public override void Configure(EntityTypeBuilder<Material> builder)
    {
        base.Configure(builder);
        builder.ToTable("Materials");

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
public sealed class SubjectConfiguration : BaseConfiguration<Subject>
{
    public override void Configure(EntityTypeBuilder<Subject> builder)
    {
        base.Configure(builder);
        builder.ToTable("Subjects");

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
public sealed class ArtworkStyleConfiguration : BaseConfiguration<ArtworkStyle>
{
    public override void Configure(EntityTypeBuilder<ArtworkStyle> builder)
    {
        base.Configure(builder);
        builder.ToTable("ArtworkStyles");

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
public sealed class FavoriteConfiguration : BaseConfiguration<Favorite>
{
    public override void Configure(EntityTypeBuilder<Favorite> builder)
    {
        base.Configure(builder);

        builder.ToTable("Favorites");

        builder.HasOne(x => x.Artwork)
            .WithMany()
            .HasForeignKey(x => x.ArtworkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.ArtworkId
        }).IsUnique();
    }
}
public sealed class OrderConfiguration : BaseConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("Orders");

        builder.Property(x => x.BuyerFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.BuyerEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.BuyerPhone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DeliveryAddress)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.DeliveryCity)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(12, 2);

        builder.Property(x => x.DeliveryFee)
            .HasPrecision(12, 2);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.BuyerUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
public sealed class OrderItemConfiguration : BaseConfiguration<OrderItem>
{
    public override void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_Quantity",
                "\"Quantity\" = 1");

            table.HasCheckConstraint(
                "CK_OrderItems_UnitPrice",
                "\"UnitPrice\" > 0");
        });

        builder.Property(x => x.ArtworkTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(12, 2);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Artwork)
            .WithMany()
            .HasForeignKey(x => x.ArtworkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ArtworkId);
    }
}
public sealed class ReservationConfiguration : BaseConfiguration<Reservation>
{
    public override void Configure(EntityTypeBuilder<Reservation> builder)
    {
        base.Configure(builder);

        builder.ToTable("Reservations");

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.HasOne(x => x.Artwork)
            .WithMany()
            .HasForeignKey(x => x.ArtworkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ArtworkId)
            .IsUnique()
            .HasFilter(
                $"\"Status\" = {(int)ReservationStatus.Active}");

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => x.ExpiresAt);
    }
}
public sealed class PaymentConfiguration : BaseConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Payments");

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2);

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasMaxLength(100);

        builder.Property(x => x.TransactionReference)
            .HasMaxLength(250);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);
    }
}
public sealed class OrderStatusHistoryConfiguration : BaseConfiguration<OrderStatusHistory>
{
    public override void Configure(
        EntityTypeBuilder<OrderStatusHistory> builder)
    {
        base.Configure(builder);

        builder.ToTable("OrderStatusHistories");

        builder.Property(x => x.PreviousStatus)
            .HasConversion<int?>();

        builder.Property(x => x.NewStatus)
            .HasConversion<int>();

        builder.Property(x => x.Note)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OrderId);
    }
}
public sealed class DeliveryConfiguration : BaseConfiguration<Delivery>
{
    public override void Configure(EntityTypeBuilder<Delivery> builder)
    {
        base.Configure(builder);

        builder.ToTable("Deliveries");

        builder.Property(x => x.CourierName)
            .HasMaxLength(200);

        builder.Property(x => x.TrackingNumber)
            .HasMaxLength(150);

        builder.Property(x => x.DeliveryNotes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();
    }
}
public sealed class OrderNoteConfiguration : BaseConfiguration<OrderNote>
{
    public override void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        base.Configure(builder);

        builder.ToTable("OrderNotes");

        builder.Property(x => x.Content)
            .HasMaxLength(5000)
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OrderId);
    }
}