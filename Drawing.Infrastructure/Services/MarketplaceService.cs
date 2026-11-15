using Drawing.Application.Features.Artworks.DTOs;
using Drawing.Application.Features.Marketplace;
using Drawing.Application.Features.Orders;
using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using Drawing.Infrastructure.Notifications;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Services;
public sealed class MarketplaceService : IMarketplaceService
{
    private readonly DrawingDbContext _db;
    public MarketplaceService(DrawingDbContext db)
    {
        _db = db;
    }

    private static string Url(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        if (key.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return key;

        return "/api/v1/media/" + key;
    }

    private static string CardImageUrl(string key)
    {
        if (!key.StartsWith("artworks/", StringComparison.Ordinal)) return Url(key);

        return "/api/v1/media/thumbnails/" + Path.GetFileNameWithoutExtension(key) + ".webp";
    }

    private static ArtworkCardDto Card(Artwork artwork)
    {
        var image = artwork.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => CardImageUrl(x.StorageKey))
            .FirstOrDefault();

        return new ArtworkCardDto(
            artwork.Id,
            artwork.Title,
            artwork.Price,
            artwork.City,
            artwork.ArtistProfile?.DisplayName ?? string.Empty,
            artwork.SaleStatus.ToString(),
            image,
            artwork.PublishedAt);
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto(
            order.Id,
            order.BuyerUserId,
            order.BuyerFullName,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt,
            order.Items.Select(x => new OrderItemDto(x.ArtworkId, x.ArtworkTitle, x.UnitPrice)).ToList());
    }

    private IQueryable<Artwork> PublicArtworks()
    {
        return _db.Artworks
            .AsNoTracking()
            .Where(x => x.ModerationStatus == ArtworkModerationStatus.Approved && x.ArtistProfile.Status == ArtistStatus.Approved)
            .Include(x => x.ArtistProfile)
            .Include(x => x.Images);
    }

    private async Task<Artwork> OwnedArtwork(Guid userId, Guid artworkId, CancellationToken ct)
    {
        var artwork = await _db.Artworks
            .Include(x => x.ArtistProfile)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == artworkId, ct)
            ?? throw new KeyNotFoundException("Əsər tapılmadı.");

        if (artwork.ArtistProfile.UserId != userId)
            throw new UnauthorizedAccessException("Bu əsər sizə aid deyil.");

        return artwork;
    }

    public async Task<HomeDto> HomeAsync(CancellationToken ct)
    {
        var slides = (await _db.HomeSlides
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Take(10)
            .ToListAsync(ct))
            .Select(x => new HomeSlideDto(x.Id, x.Title, x.Subtitle, Url(x.ImageKey), x.ButtonText, x.LinkUrl))
            .ToList();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive && x.ShowOnHome)
            .OrderBy(x => x.DisplayOrder)
            .Take(12)
            .ToListAsync(ct);

        var carousels = new List<HomeCategoryDto>();

        foreach (var category in categories)
        {
            var artworks = await PublicArtworks()
                .Where(x => x.SaleStatus == ArtworkSaleStatus.Available && _db.ArtworkCategories.Any(ac => ac.ArtworkId == x.Id && ac.CategoryId == category.Id))
                .OrderByDescending(x => x.PublishedAt)
                .Take(12)
                .ToListAsync(ct);

            var categoryDto = new CategoryDto(category.Id, category.Name, category.Slug, category.DisplayOrder);

            carousels.Add(new HomeCategoryDto(categoryDto, artworks.Select(Card).ToList()));
        }


        var featured = await PublicArtworks()
            .Where(x => x.SaleStatus == ArtworkSaleStatus.Available && x.IsFeatured)
            .OrderByDescending(x => x.PublishedAt)
            .Take(12)
            .ToListAsync(ct);


        return new HomeDto(slides, carousels, featured.Select(Card).ToList());
    }

    public async Task<NavbarDto> NavbarAsync(CancellationToken ct)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.DisplayOrder))
            .ToListAsync(ct);

        return new NavbarDto(categories);
    }

    public async Task<FooterDto> FooterAsync(CancellationToken ct)
    {
        var settings = await _db.SiteSettings
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.Value, ct);

        string? GetSetting(string key) => settings.TryGetValue(key, out var value) ? value : null;

        var links = await _db.SitePages
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderBy(x => x.Title)
            .Select(x => new SitePageDto(x.Slug, x.Title, string.Empty))
            .ToListAsync(ct);

        return new FooterDto(
            GetSetting("footer.email"),
            GetSetting("footer.phone"),
            GetSetting("footer.instagram"),
            GetSetting("footer.facebook"),
            links);
    }

    public async Task<PagedResult<ArtworkCardDto>> SearchAsync(ArtworkSearchRequest request, CancellationToken ct)
    {
        var page = Math.Clamp(request.Page, 1, 100000);
        var pageSize = Math.Clamp(request.PageSize, 1, 48);
        var query = PublicArtworks();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = request.Q.Trim();

            query = query.Where(x =>
                EF.Functions.ILike(x.Title, "%" + term + "%") ||
                EF.Functions.ILike(x.ArtistProfile.DisplayName, "%" + term + "%"));
        }

        if (request.AvailableOnly)
            query = query.Where(x => x.SaleStatus == ArtworkSaleStatus.Available);

        if (request.CategoryId is Guid categoryId)
            query = query.Where(x => _db.ArtworkCategories.Any(ac => ac.ArtworkId == x.Id && ac.CategoryId == categoryId));

        if (request.TechniqueId is Guid techniqueId)
            query = query.Where(x => x.TechniqueId == techniqueId);

        if (request.SubjectId is Guid subjectId)
            query = query.Where(x => x.SubjectId == subjectId);

        if (request.MinPrice is decimal minPrice)
            query = query.Where(x => x.Price >= minPrice);

        if (request.MaxPrice is decimal maxPrice)
            query = query.Where(x => x.Price <= maxPrice);

        if (request.MinWidthCm is decimal minWidth)
            query = query.Where(x => x.WidthCm >= minWidth);

        if (request.MaxWidthCm is decimal maxWidth)
            query = query.Where(x => x.WidthCm <= maxWidth);

        var total = await query.CountAsync(ct);

        query = request.Sort switch
        {
            "price_asc" => query.OrderBy(x => x.Price).ThenBy(x => x.Id),
            "price_desc" => query.OrderByDescending(x => x.Price).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.PublishedAt).ThenBy(x => x.Id)
        };

        var artworks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ArtworkCardDto>(artworks.Select(Card).ToList(), page, pageSize, total);
    }

    public async Task<ArtworkDetailsDto?> ArtworkAsync(Guid id, CancellationToken ct)
    {
        var artwork = await PublicArtworks().FirstOrDefaultAsync(x => x.Id == id, ct);

        if (artwork is null) return null;

        var categories = await _db.ArtworkCategories
            .AsNoTracking()
            .Where(x => x.ArtworkId == id)
            .Select(x => x.Category.Name)
            .ToListAsync(ct);

        var images = artwork.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => Url(x.StorageKey))
            .ToList();

        return new ArtworkDetailsDto(
            artwork.Id,
            artwork.Title,
            artwork.Description,
            artwork.Price,
            artwork.WidthCm,
            artwork.HeightCm,
            artwork.City,
            artwork.ArtistProfile.DisplayName,
            artwork.ArtistProfileId,
            artwork.SaleStatus.ToString(),
            images,
            categories);
    }

    public async Task<IReadOnlyList<CategoryDto>> CategoriesAsync(CancellationToken ct)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.DisplayOrder))
            .ToListAsync(ct);
    }

    public Task<SitePageDto?> PageAsync(string slug, CancellationToken ct)
    {
        return _db.SitePages
            .AsNoTracking()
            .Where(x => x.Published && x.Slug == slug)
            .Select(x => new SitePageDto(x.Slug, x.Title, x.Content))
            .FirstOrDefaultAsync(ct);
    }

    public async Task ContactAsync(ContactRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Əlaqə formasının bütün sahələrini doldurun.");

        _db.ContactMessages.Add(new ContactMessage
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Message = request.Message.Trim()
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SaveCategoryAsync(Guid? id, CategoryUpsertRequest request, CancellationToken ct)
    {
        Category category;

        if (id.HasValue)
        {
            category = await _db.Categories.FindAsync(new object[] { id.Value }, ct)
                ?? throw new KeyNotFoundException("Kateqoriya tapılmadı.");
        }
        else
        {
            category = new Category();
            _db.Categories.Add(category);
        }

        category.Name = request.Name.Trim();
        category.Slug = request.Slug.Trim();
        category.Description = request.Description?.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
        category.ShowOnHome = request.ShowOnHome;

        await _db.SaveChangesAsync(ct);

        return category.Id;
    }

    public async Task<Guid> SaveSlideAsync(Guid? id, SlideUpsertRequest request, CancellationToken ct)
    {
        HomeSlide slide;

        if (!string.IsNullOrWhiteSpace(request.LinkUrl))
        {
            var link = request.LinkUrl.Trim();

            if (!link.StartsWith('/') || link.StartsWith("//"))
                throw new ArgumentException("Slider keçidi sayt daxilində nisbi URL olmalıdır.");
        }

        if (!request.ImageKey.StartsWith("slides/", StringComparison.Ordinal))
            throw new ArgumentException("Slider üçün düzgün şəkil seçilməyib.");

        if (id.HasValue)
        {
            slide = await _db.HomeSlides.FindAsync(new object[] { id.Value }, ct)
                ?? throw new KeyNotFoundException("Slider tapılmadı.");
        }
        else
        {
            slide = new HomeSlide();
            _db.HomeSlides.Add(slide);
        }

        slide.Title = request.Title.Trim();
        slide.Subtitle = request.Subtitle?.Trim();
        slide.ImageKey = request.ImageKey.Trim();
        slide.ButtonText = request.ButtonText?.Trim();
        slide.LinkUrl = request.LinkUrl?.Trim();
        slide.SortOrder = request.SortOrder;
        slide.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return slide.Id;
    }

    public async Task<Guid> SavePageAsync(Guid? id, SitePageUpsertRequest request, CancellationToken ct)
    {
        SitePage page;

        if (id.HasValue)
        {
            page = await _db.SitePages.FindAsync(new object[] { id.Value }, ct)
                ?? throw new KeyNotFoundException("Səhifə tapılmadı.");
        }
        else
        {
            page = new SitePage();
            _db.SitePages.Add(page);
        }

        page.Slug = request.Slug.Trim();
        page.Title = request.Title.Trim();
        page.Content = request.Content;
        page.Published = request.Published;

        await _db.SaveChangesAsync(ct);

        return page.Id;
    }

    public async Task SetSettingAsync(string key, string value, CancellationToken ct)
    {
        var allowedKeys = new[]
        {
            "footer.email",
            "footer.phone",
            "footer.instagram",
            "footer.facebook"
        };

        if (!allowedKeys.Contains(key))
            throw new ArgumentException("Bu parametrə icazə verilmir.");

        if (value is null)
            throw new ArgumentException("Parametrin dəyəri tələb olunur.");

        var setting = await _db.SiteSettings.FirstOrDefaultAsync(x => x.Key == key, ct);

        if (setting is null)
        {
            setting = new SiteSetting { Key = key };
            _db.SiteSettings.Add(setting);
        }

        setting.Value = value.Trim();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken ct)
    {


        if (request.RequestId == Guid.Empty)
            throw new ArgumentException("Sifariş sorğusunun RequestId dəyəri tələb olunur.");

        var existingOrder = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.BuyerUserId == userId && x.RequestId == request.RequestId, ct);

        if (existingOrder is not null)
            return MapOrder(existingOrder);

        if (request.ArtworkId == Guid.Empty)
            throw new ArgumentException("Əsər seçilməyib.");

        if (string.IsNullOrWhiteSpace(request.City) || !string.Equals(request.City.Trim(), "Bakı", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("İlk versiyada sifariş yalnız Bakı üçün mümkündür.");

        var artwork = await _db.Artworks
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.ArtworkId &&
                x.ModerationStatus == ArtworkModerationStatus.Approved &&
                x.ArtistProfile.Status == ArtistStatus.Approved &&
                x.SaleStatus == ArtworkSaleStatus.Available, ct);

        if (artwork is null)
            throw new InvalidOperationException("Əsər sifariş üçün mövcud deyil.");

        var order = new Order
        {
            BuyerUserId = userId,
            BuyerFullName = request.FullName.Trim(),
            BuyerEmail = request.Email.Trim(),
            BuyerPhone = request.Phone.Trim(),
            DeliveryAddress = request.Address.Trim(),
            DeliveryCity = "Bakı",
            TotalAmount = artwork.Price,
            RequestId = request.RequestId,
        };

        order.Items.Add(new OrderItem
        {
            ArtworkId = artwork.Id,
            ArtworkTitle = artwork.Title,
            UnitPrice = artwork.Price,
            Quantity = 1
        });

        _db.Orders.Add(order);


        _db.OutboxEmails.Add(new OutboxEmail
        {
            ToEmail = order.BuyerEmail,
            Subject = "Sifariş sorğunuz qəbul edildi",
            Body = $"Sifariş nömrəsi: {order.Id}\nƏsər: {artwork.Title}\nMəbləğ: {order.TotalAmount} AZN\nSifarişiniz administrator tərəfindən yoxlanılacaq."
        });


        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Order = order,
            NewStatus = OrderStatus.Pending,
            ChangedByUserId = userId
        });

        await _db.SaveChangesAsync(ct);

        return MapOrder(order);
    }

    public async Task<PagedResult<OrderDto>> MyOrdersAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Orders.AsNoTracking().Where(x => x.BuyerUserId == userId);
        var total = await query.CountAsync(ct);

        var orders = await query
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(orders.Select(MapOrder).ToList(), page, pageSize, total);
    }

    public async Task<OrderDto?> OrderAsync(Guid userId, Guid id, bool admin, CancellationToken ct)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.Id == id);

        if (!admin)
            query = query.Where(x => x.BuyerUserId == userId);

        var order = await query.FirstOrDefaultAsync(ct);

        return order is null ? null : MapOrder(order);
    }

    public async Task<IReadOnlyList<OrderDto>> AdminOrdersAsync(CancellationToken ct)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return orders.Select(MapOrder).ToList();
    }


    public async Task TransitionOrderAsync(Guid adminId, Guid id, AdminOrderTransitionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Action)) throw new ArgumentException("Sifariş əməliyyatı tələb olunur.");

        var action = request.Action.Trim().ToLowerInvariant();

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var order = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Sifariş tapılmadı.");

        if (order.Items.Count != 1) throw new InvalidOperationException("Sifariş yalnız bir orijinal əsərdən ibarət olmalıdır.");

        var artworkId = order.Items.Single().ArtworkId;

        var artwork = await _db.Artworks.Include(x => x.ArtistProfile).FirstOrDefaultAsync(x => x.Id == artworkId, ct)
            ?? throw new KeyNotFoundException("Əsər tapılmadı.");

        var previousStatus = order.Status;

        switch (action)
        {
            case "confirm":
                {
                    if (order.Status != OrderStatus.Pending) throw new InvalidOperationException("Yalnız yeni sifariş təsdiqlənə bilər.");
                    if (artwork.ModerationStatus != ArtworkModerationStatus.Approved || artwork.ArtistProfile.Status != ArtistStatus.Approved)
                        throw new InvalidOperationException("Əsər və ya rəssam təsdiqlənməyib.");

                    if (artwork.SaleStatus != ArtworkSaleStatus.Available)
                        throw new InvalidOperationException("Əsər satış üçün mövcud deyil.");

                    var hasReservation = await _db.Reservations.AnyAsync(x => x.ArtworkId == artworkId && x.Status == ReservationStatus.Active, ct);

                    if (hasReservation) throw new InvalidOperationException("Əsər artıq rezerv edilib.");

                    order.Confirm();
                    artwork.Reserve();

                    _db.Reservations.Add(new Reservation
                    {
                        ArtworkId = artworkId,
                        OrderId = order.Id,
                        ExpiresAt = DateTime.UtcNow.AddHours(Math.Clamp(request.ReservationHours, 1, 168))
                    });

                    break;
                }

            case "requestpayment":
                {
                    if (order.Status != OrderStatus.Confirmed) throw new InvalidOperationException("Sifariş əvvəlcə təsdiqlənməlidir.");

                    var reservation = await _db.Reservations.FirstOrDefaultAsync(x => x.OrderId == id && x.Status == ReservationStatus.Active, ct);

                    if (reservation is null || reservation.ExpiresAt <= DateTime.UtcNow)
                        throw new InvalidOperationException("Aktiv rezervasiya müddəti bitib.");

                    var fee = request.DeliveryFee ?? 0;

                    if (fee < 0 || fee > 5000) throw new ArgumentException("Çatdırılma haqqı düzgün deyil.");

                    order.RequestPayment();
                    order.DeliveryFee = fee;
                    order.TotalAmount = order.Items.Sum(x => x.UnitPrice * x.Quantity) + fee;

                    break;
                }

            case "paid":
                {
                    if (order.Status != OrderStatus.AwaitingPayment) throw new InvalidOperationException("Sifariş ödəniş gözləmir.");

                    var reservation = await _db.Reservations.FirstOrDefaultAsync(x => x.OrderId == id && x.Status == ReservationStatus.Active, ct);

                    if (reservation is null || reservation.ExpiresAt <= DateTime.UtcNow)
                        throw new InvalidOperationException("Rezervasiya müddəti bitib. Ödəniş təsdiqlənə bilməz.");

                    if (artwork.SaleStatus != ArtworkSaleStatus.Reserved)
                        throw new InvalidOperationException("Əsərin rezervasiya vəziyyəti düzgün deyil.");

                    var alreadyPaid = await _db.Payments.AnyAsync(x => x.OrderId == id && x.Status == PaymentStatus.Confirmed, ct);

                    if (alreadyPaid) throw new InvalidOperationException("Bu sifariş üçün ödəniş artıq təsdiqlənib.");

                    var payment = new Payment
                    {
                        OrderId = id,
                        Amount = order.TotalAmount,
                        Currency = "AZN"
                    };

                    payment.Confirm("Manual");

                    reservation.ConvertToSale();
                    order.MarkAsPaid();
                    artwork.MarkAsSold();

                    _db.Payments.Add(payment);

                    break;
                }

            case "prepare":
                {
                    if (order.Status != OrderStatus.Paid) throw new InvalidOperationException("Sifariş ödənilməyib.");
                    order.StartDeliveryPreparation();
                    break;
                }

            case "shipped":
                {
                    if (order.Status != OrderStatus.PreparingDelivery)
                        throw new InvalidOperationException("Sifariş çatdırılmaya hazırlanmır.");

                    var deliveryExists = await _db.Deliveries.AnyAsync(x => x.OrderId == id, ct);

                    if (!deliveryExists) throw new InvalidOperationException("Əvvəlcə çatdırılma məlumatlarını daxil edin.");

                    order.MarkAsShipped();
                    break;
                }

            case "complete":
                {
                    if (order.Status != OrderStatus.Shipped) throw new InvalidOperationException("Sifariş göndərilməyib.");

                    var delivery = await _db.Deliveries.FirstOrDefaultAsync(x => x.OrderId == id, ct)
                        ?? throw new InvalidOperationException("Çatdırılma qeydi tapılmadı.");

                    order.Complete();
                    delivery.DeliveredAt ??= DateTime.UtcNow;

                    break;
                }

            case "reject":
                {
                    if (order.Status != OrderStatus.Pending)
                        throw new InvalidOperationException("Yalnız təsdiqlənməmiş sifariş rədd edilə bilər.");

                    if (string.IsNullOrWhiteSpace(request.Reason))
                        throw new ArgumentException("Rədd səbəbi tələb olunur.");

                    order.Reject(request.Reason.Trim());

                    break;
                }

            case "cancel":
                {
                    if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.AwaitingPayment))
                        throw new InvalidOperationException("Bu mərhələdə sifariş ləğv edilə bilməz.");

                    var reservation = await _db.Reservations.FirstOrDefaultAsync(x => x.OrderId == id && x.Status == ReservationStatus.Active, ct);

                    if (reservation is not null)
                    {
                        reservation.Cancel();
                        artwork.ReleaseReservation();
                    }

                    order.Cancel();

                    break;
                }

            default:
                throw new ArgumentException("Yanlış sifariş əməliyyatı.");
        }


        string? notification = action switch
        {
            "confirm" => $"Sifarişiniz təsdiqləndi.\nSifariş: {order.Id}\nRezervasiya müddəti: {Math.Clamp(request.ReservationHours, 1, 168)} saat.",
            "requestpayment" => $"Sifariş: {order.Id}\nYekun məbləğ: {order.TotalAmount} AZN\nÖdəniş qaydası: {request.PaymentInstructions}",
            "paid" => $"Sifariş: {order.Id}\nÖdəniş təsdiqləndi.",
            "shipped" => $"Sifariş: {order.Id}\nƏsər çatdırılmaya göndərildi.",
            "complete" => $"Sifariş: {order.Id}\nÇatdırılma tamamlandı.",
            "cancel" => $"Sifariş: {order.Id}\nSifariş ləğv edildi.",
            "reject" => $"Sifariş: {order.Id}\nSifariş rədd edildi.\nSəbəb: {request.Reason}",
            _ => null
        };

        if (notification is not null)
        {
            _db.OutboxEmails.Add(new OutboxEmail
            {
                ToEmail = order.BuyerEmail,
                Subject = "Sifariş statusu yeniləndi",
                Body = notification
            });
        }


        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            PreviousStatus = previousStatus,
            NewStatus = order.Status,
            ChangedByUserId = adminId,
            Note = request.Reason
        });

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            Action = "Order." + action,
            EntityName = "Order",
            EntityId = order.Id.ToString(),
            Details = request.Reason
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }


    public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid artworkId, CancellationToken ct)
    {
        var favorite = await _db.Favorites.FirstOrDefaultAsync(x => x.UserId == userId && x.ArtworkId == artworkId, ct);

        if (favorite is not null)
        {
            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync(ct);

            return false;
        }

        var exists = await PublicArtworks().AnyAsync(x => x.Id == artworkId, ct);

        if (!exists)
            throw new KeyNotFoundException("Əsər tapılmadı.");

        _db.Favorites.Add(new Favorite
        {
            UserId = userId,
            ArtworkId = artworkId
        });

        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<IReadOnlyList<ArtworkCardDto>> FavoritesAsync(Guid userId, CancellationToken ct)
    {
        var ids = await _db.Favorites
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ArtworkId)
            .ToListAsync(ct);

        var artworks = await PublicArtworks()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(ct);

        return artworks.Select(Card).ToList();
    }

    public async Task<IReadOnlyList<ArtworkCardDto>> ArtistArtworksAsync(Guid userId, CancellationToken ct)
    {
        var artist = await _db.ArtistProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (artist is null)
            return [];

        var artworks = await _db.Artworks
            .AsNoTracking()
            .Include(x => x.ArtistProfile)
            .Include(x => x.Images)
            .Where(x => x.ArtistProfileId == artist.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return artworks.Select(Card).ToList();
    }

    public async Task UpdateArtworkAsync(Guid userId, Guid artworkId, CreateArtworkRequest request, CancellationToken ct)
    {
        var artwork = await OwnedArtwork(userId, artworkId, ct);

        if (artwork.ModerationStatus is not (ArtworkModerationStatus.Draft or ArtworkModerationStatus.Rejected or ArtworkModerationStatus.ChangesRequested))
            throw new InvalidOperationException("Bu əsər redaktə edilə bilməz.");

        if (string.IsNullOrWhiteSpace(request.Title) || request.Price <= 0 || request.WidthCm <= 0 || request.HeightCm <= 0 || string.IsNullOrWhiteSpace(request.City))
            throw new ArgumentException("Əsərin adı, qiyməti, ölçüləri və şəhəri düzgün olmalıdır.");

        artwork.Title = request.Title.Trim();
        artwork.Description = request.Description?.Trim();
        artwork.Price = request.Price;
        artwork.WidthCm = request.WidthCm;
        artwork.HeightCm = request.HeightCm;
        artwork.TechniqueId = request.TechniqueId;
        artwork.MaterialId = request.MaterialId;
        artwork.SubjectId = request.SubjectId;
        artwork.StyleId = request.StyleId;
        artwork.CreationYear = request.CreationYear;
        artwork.IsFramed = request.IsFramed;
        artwork.City = request.City.Trim();
        artwork.LocationDetails = request.LocationDetails?.Trim();

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteDraftArtworkAsync(Guid userId, Guid artworkId, CancellationToken ct)
    {
        var artwork = await OwnedArtwork(userId, artworkId, ct);

        if (artwork.ModerationStatus != ArtworkModerationStatus.Draft)
            throw new InvalidOperationException("Yalnız qaralama əsər silinə bilər.");

        var hasOrder = await _db.OrderItems.AnyAsync(x => x.ArtworkId == artwork.Id, ct);

        if (hasOrder)
            throw new InvalidOperationException("Sifariş verilmiş əsər silinə bilməz.");

        _db.Artworks.Remove(artwork);

        await _db.SaveChangesAsync(ct);
    }

    public async Task SubmitArtworkAsync(Guid userId, Guid artworkId, CancellationToken ct)
    {
        var artwork = await OwnedArtwork(userId, artworkId, ct);

        if (artwork.ArtistProfile.Status != ArtistStatus.Approved)
            throw new UnauthorizedAccessException("Rəssam profili təsdiqlənməyib.");

        artwork.SubmitForReview();

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetArtworkCategoriesAsync(Guid userId, Guid artworkId, IReadOnlyList<Guid> categoryIds, CancellationToken ct)
    {
        var artwork = await OwnedArtwork(userId, artworkId, ct);

        if (artwork.ModerationStatus is not (ArtworkModerationStatus.Draft or ArtworkModerationStatus.ChangesRequested or ArtworkModerationStatus.Rejected))
            throw new InvalidOperationException("Bu əsərin kateqoriyaları dəyişdirilə bilməz.");

        var ids = categoryIds.Distinct().ToHashSet();

        if (ids.Count > 5)
            throw new ArgumentException("Maksimum 5 kateqoriya seçilə bilər.");

        var validCount = await _db.Categories.CountAsync(x => ids.Contains(x.Id) && x.IsActive, ct);

        if (validCount != ids.Count)
            throw new ArgumentException("Yanlış və ya deaktiv kateqoriya seçilib.");

        var existing = await _db.ArtworkCategories
            .Where(x => x.ArtworkId == artworkId)
            .ToListAsync(ct);

        var existingIds = existing.Select(x => x.CategoryId).ToHashSet();

        var toRemove = existing.Where(x => !ids.Contains(x.CategoryId)).ToList();

        _db.ArtworkCategories.RemoveRange(toRemove);

        foreach (var categoryId in ids.Except(existingIds))
        {
            _db.ArtworkCategories.Add(new ArtworkCategory
            {
                ArtworkId = artworkId,
                CategoryId = categoryId
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> AddArtworkImageAsync(Guid userId, Guid artworkId, string imageKey, bool primary, CancellationToken ct)
    {
        var artwork = await OwnedArtwork(userId, artworkId, ct);

        if (artwork.ModerationStatus is not (ArtworkModerationStatus.Draft or ArtworkModerationStatus.ChangesRequested or ArtworkModerationStatus.Rejected))
            throw new InvalidOperationException("Yalnız redaktə olunan əsərə şəkil əlavə edilə bilər.");

        if (artwork.Images.Count >= 10)
            throw new ArgumentException("Bir əsərə maksimum 10 şəkil əlavə edilə bilər.");

        if (string.IsNullOrWhiteSpace(imageKey))
            throw new ArgumentException("Şəkil məlumatı tələb olunur.");

        var isPrimary = primary || artwork.Images.Count == 0;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        if (isPrimary && artwork.Images.Any(x => x.IsPrimary))
        {
            foreach (var image in artwork.Images)
                image.IsPrimary = false;

            await _db.SaveChangesAsync(ct);
        }

        var newImage = new ArtworkImage
        {
            ArtworkId = artworkId,
            StorageKey = imageKey,
            IsPrimary = isPrimary,
            SortOrder = artwork.Images.Count
        };

        _db.ArtworkImages.Add(newImage);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Url(imageKey);
    }

    public async Task DeleteArtworkImageAsync(Guid userId, Guid artworkId, Guid imageId, CancellationToken ct)
    {
        var artwork = await OwnedArtwork(userId, artworkId, ct);

        if (artwork.ModerationStatus is not (ArtworkModerationStatus.Draft or ArtworkModerationStatus.ChangesRequested or ArtworkModerationStatus.Rejected))
            throw new InvalidOperationException("Bu əsərin şəkilləri dəyişdirilə bilməz.");

        var image = artwork.Images.FirstOrDefault(x => x.Id == imageId)
            ?? throw new KeyNotFoundException("Şəkil tapılmadı.");

        var nextImage = image.IsPrimary
            ? artwork.Images.Where(x => x.Id != imageId).OrderBy(x => x.SortOrder).FirstOrDefault()
            : null;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        _db.ArtworkImages.Remove(image);

        await _db.SaveChangesAsync(ct);

        if (nextImage is not null)
        {
            nextImage.IsPrimary = true;
            await _db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<ArtworkCardDto>> PendingArtworksAsync(CancellationToken ct)
    {
        var artworks = await _db.Artworks
            .AsNoTracking()
            .Include(x => x.ArtistProfile)
            .Include(x => x.Images)
            .Where(x => x.ModerationStatus == ArtworkModerationStatus.PendingReview)
            .OrderBy(x => x.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);

        return artworks.Select(Card).ToList();
    }

    public Task ApproveArtworkAsync(Guid adminId, Guid id, CancellationToken ct)
    {
        return ReviewArtworkAsync(adminId, id, "approve", null, ct);
    }

    public async Task ReviewArtworkAsync(Guid adminId, Guid id, string action, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Moderasiya əməliyyatı seçilməyib.");

        var artwork = await _db.Artworks
            .Include(x => x.ArtistProfile)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Əsər tapılmadı.");

        var normalizedAction = action.Trim().ToLowerInvariant();

        if (normalizedAction == "approve" && artwork.ArtistProfile.Status != ArtistStatus.Approved)
            throw new InvalidOperationException("Rəssam təsdiqlənməyib.");

        switch (normalizedAction)
        {
            case "approve":
                artwork.Approve();
                break;

            case "reject":
                artwork.Reject(reason ?? string.Empty);
                break;

            case "changes":
                artwork.RequestChanges(reason ?? string.Empty);
                break;

            case "hide":
                artwork.Hide();
                break;

            default:
                throw new ArgumentException("Yanlış moderasiya qərarı.");
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            Action = "Artwork." + normalizedAction,
            EntityName = "Artwork",
            EntityId = id.ToString(),
            Details = reason
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> DashboardAsync(CancellationToken ct)
    {
        return new
        {
            Users = await _db.Users.CountAsync(ct),
            ArtistsPending = await _db.ArtistProfiles.CountAsync(x => x.Status == ArtistStatus.Pending, ct),
            ArtworksPending = await _db.Artworks.CountAsync(x => x.ModerationStatus == ArtworkModerationStatus.PendingReview, ct),
            ArtworksLive = await _db.Artworks.CountAsync(x => x.ModerationStatus == ArtworkModerationStatus.Approved, ct),
            OrdersPending = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Pending, ct),
            OrdersCompleted = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Completed, ct)
        };
    }

    public async Task<IReadOnlyList<object>> AdminUsersAsync(CancellationToken ct)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                x.IsBlocked,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return users.Cast<object>().ToList();
    }

    public async Task SetUserBlockedAsync(Guid adminId, Guid userId, bool blocked, CancellationToken ct)
    {
        if (adminId == userId)
            throw new InvalidOperationException("Öz hesabınızı bloklaya bilməzsiniz.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        user.IsBlocked = blocked;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            Action = blocked ? "User.Block" : "User.Unblock",
            EntityName = "ApplicationUser",
            EntityId = userId.ToString()
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<object>> AdminContactsAsync(CancellationToken ct)
    {
        var messages = await _db.ContactMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                x.Message,
                x.IsRead,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return messages.Cast<object>().ToList();
    }

    public async Task SetContactReadAsync(Guid id, CancellationToken ct)
    {
        var message = await _db.ContactMessages.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Müraciət tapılmadı.");

        message.IsRead = true;

        await _db.SaveChangesAsync(ct);
    }
}
