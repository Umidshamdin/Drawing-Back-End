using Drawing.Application.Abstractions.Persistence;
using Drawing.Domain.Common;
using Drawing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Persistence;
public sealed class DrawingDbContext : DbContext, IUnitOfWork
{
    public DrawingDbContext(DbContextOptions<DrawingDbContext> options) : base(options)
    {
    }
    public DbSet<ArtistProfile> ArtistProfiles => Set<ArtistProfile>();
    public DbSet<Artwork> Artworks => Set<Artwork>();
    public DbSet<ArtworkImage> ArtworkImages => Set<ArtworkImage>();
    public DbSet<Technique> Techniques => Set<Technique>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ArtworkStyle> ArtworkStyles => Set<ArtworkStyle>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrawingDbContext).Assembly);
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }
    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}