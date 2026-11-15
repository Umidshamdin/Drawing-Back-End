using System.Threading.RateLimiting;
using Drawing.API.Infrastructure;
using Drawing.Application;
using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Application.Features.Marketplace;
using Drawing.Application.Features.Orders;
using Drawing.Application.Features.ReferenceData;
using Drawing.Infrastructure;
using Drawing.Infrastructure.Identity;
using Drawing.Infrastructure.Persistence;
using Drawing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token daxil edin."
        };

        document.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            }
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IArtistCatalogService, ArtistCatalogService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IContentAdminService, ContentAdminService>();
builder.Services.AddScoped<IOrderManagementService, OrderManagementService>();

builder.Services.AddScoped<SafeImageService>();

builder.Services.AddHostedService<ReservationExpiryWorker>();
builder.Services.AddHostedService<EmailOutboxWorker>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("public-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

if (!builder.Environment.IsDevelopment() && builder.Configuration["Mail:Mode"] != "Smtp")
    throw new InvalidOperationException("Production mühitində SMTP email xidməti konfiqurasiya edilməlidir.");

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Drawing API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("ReactClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { Status = "alive" }));

app.MapGet("/health/ready", async (DrawingDbContext db, CancellationToken ct) =>
{
    var connected = await db.Database.CanConnectAsync(ct);
    return connected ? Results.Ok(new { Status = "ready" }) : Results.StatusCode(503);
});

await IdentityDataSeeder.SeedRolesAsync(app.Services);

app.Run();
