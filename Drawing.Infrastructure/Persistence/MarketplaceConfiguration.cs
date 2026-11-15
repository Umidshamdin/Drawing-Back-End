using Drawing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drawing.Infrastructure.Persistence;
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.ShowOnHome, x.DisplayOrder });
    }
}

public sealed class ArtworkCategoryConfiguration : IEntityTypeConfiguration<ArtworkCategory>
{
    public void Configure(EntityTypeBuilder<ArtworkCategory> builder)
    {
        builder.ToTable("ArtworkCategories");
        builder.HasKey(x => new { x.ArtworkId, x.CategoryId });

        builder.HasOne(x => x.Artwork)
            .WithMany()
            .HasForeignKey(x => x.ArtworkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.ArtworkCategories)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CategoryId);
    }
}

public sealed class HomeSlideConfiguration : IEntityTypeConfiguration<HomeSlide>
{
    public void Configure(EntityTypeBuilder<HomeSlide> builder)
    {
        builder.ToTable("HomeSlides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(400);
        builder.Property(x => x.ImageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ButtonText).HasMaxLength(80);
        builder.Property(x => x.LinkUrl).HasMaxLength(500);
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public sealed class SitePageConfiguration : IEntityTypeConfiguration<SitePage>
{
    public void Configure(EntityTypeBuilder<SitePage> builder)
    {
        builder.ToTable("SitePages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
    }
}
