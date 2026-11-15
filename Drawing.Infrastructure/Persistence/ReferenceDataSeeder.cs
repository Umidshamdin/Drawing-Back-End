using Drawing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Drawing.Infrastructure.Persistence;

public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DrawingDbContext>();

        if (!await db.Techniques.AnyAsync())
        {
            db.Techniques.AddRange(
                new Technique { Name = "Yağlı boya" },
                new Technique { Name = "Akril" },
                new Technique { Name = "Akvarel" },
                new Technique { Name = "Qrafika" },
                new Technique { Name = "Qarışıq texnika" });
        }

        if (!await db.Materials.AnyAsync())
        {
            db.Materials.AddRange(
                new Material { Name = "Kətan" },
                new Material { Name = "Kağız" },
                new Material { Name = "Taxta" },
                new Material { Name = "Karton" });
        }

        if (!await db.Subjects.AnyAsync())
        {
            db.Subjects.AddRange(
                new Subject { Name = "Mənzərə" },
                new Subject { Name = "Portret" },
                new Subject { Name = "Natürmort" },
                new Subject { Name = "Abstrakt" });
        }

        if (!await db.ArtworkStyles.AnyAsync())
        {
            db.ArtworkStyles.AddRange(
                new ArtworkStyle { Name = "Realizm" },
                new ArtworkStyle { Name = "İmpressionizm" },
                new ArtworkStyle { Name = "Ekspressionizm" },
                new ArtworkStyle { Name = "Minimalizm" });
        }

        await db.SaveChangesAsync();
    }
}
