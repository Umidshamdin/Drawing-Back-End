using Drawing.Application.Abstractions.Persistence;
using Drawing.Domain.Common;
using Drawing.Domain.Entities;
using Drawing.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Persistence;

public sealed class DrawingDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public DrawingDbContext(DbContextOptions<DrawingDbContext> options) : base(options) { }

    public DbSet<ArtistProfile> ArtistProfiles => Set<ArtistProfile>();
    public DbSet<Artwork> Artworks => Set<Artwork>();
    public DbSet<ArtworkImage> ArtworkImages => Set<ArtworkImage>();
    public DbSet<Technique> Techniques => Set<Technique>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ArtworkStyle> ArtworkStyles => Set<ArtworkStyle>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ArtworkCategory> ArtworkCategories => Set<ArtworkCategory>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<HomeSlide> HomeSlides => Set<HomeSlide>();
    public DbSet<SitePage> SitePages => Set<SitePage>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrawingDbContext).Assembly);

        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.IsBlocked).HasDefaultValue(false);
            builder.HasIndex(x => x.IsBlocked);
        });

        modelBuilder.Entity<ArtistProfile>()
            .HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<ArtistProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Favorite>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderNote>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<RefreshSession>(entity =>
        {
            entity.ToTable("RefreshSessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty) entry.Entity.Id = Guid.NewGuid();
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
