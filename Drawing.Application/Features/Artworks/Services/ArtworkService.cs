using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Abstractions.Persistence;
using Drawing.Application.Features.Artworks.DTOs;
using Drawing.Application.Features.Artworks.Interfaces;
using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using AppException = Drawing.Application.Common.Exceptions.ApplicationException;

namespace Drawing.Application.Features.Artworks.Services;
public sealed class ArtworkService : IArtworkService
{
    private readonly IArtworkRepository _artworkRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    public ArtworkService(IArtworkRepository artworkRepository, IArtistRepository artistRepository, IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _artworkRepository = artworkRepository;
        _artistRepository = artistRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }
    public async Task<ArtworkDto> CreateDraftAsync(CreateArtworkRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        var artist = await _artistRepository.GetByUserIdAsync(userId, cancellationToken);

        if (artist is null || artist.Status != ArtistStatus.Approved)
        {
            throw new AppException("Yalnız təsdiqlənmiş rəssam əsər əlavə edə bilər.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("Əsərin adı tələb olunur.");

        if (request.Price <= 0)
            throw new AppException("Qiymət sıfırdan böyük olmalıdır.");

        if (request.WidthCm <= 0 || request.HeightCm <= 0)
            throw new AppException("Ölçülər sıfırdan böyük olmalıdır.");

        var artwork = new Artwork
        {
            ArtistProfileId = artist.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            TechniqueId = request.TechniqueId,
            MaterialId = request.MaterialId,
            SubjectId = request.SubjectId,
            StyleId = request.StyleId,
            CreationYear = request.CreationYear,
            IsFramed = request.IsFramed,
            City = request.City.Trim(),
            LocationDetails = request.LocationDetails?.Trim()
        };

        await _artworkRepository.AddAsync(artwork, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(artwork);
    }
    public async Task<ArtworkDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        var artwork = await _artworkRepository.GetByIdAsync(id, cancellationToken);

        if (artwork is null) return null;

        await EnsureOwnershipAsync(artwork, userId, cancellationToken);

        return MapToDto(artwork);
    }

    public async Task<IReadOnlyList<ArtworkDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        var artist = await _artistRepository.GetByUserIdAsync(userId, cancellationToken);

        if (artist is null)
            throw new AppException("Rəssam profili tapılmadı.");

        var artworks = await _artworkRepository.GetAllAsync(
            cancellationToken);

        return artworks
            .Where(x => x.ArtistProfileId == artist.Id)
            .Select(MapToDto)
            .ToList();
    }
    public async Task SubmitForReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        var artwork = await _artworkRepository.GetByIdAsync(id, cancellationToken);

        if (artwork is null)
            throw new AppException("Əsər tapılmadı.");

        await EnsureOwnershipAsync(artwork, userId, cancellationToken);

        artwork.SubmitForReview();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    private Guid GetRequiredUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not Guid userId || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("İstifadəçi sistemə daxil olmayıb.");
        }
        return userId;
    }
    private async Task EnsureOwnershipAsync(Artwork artwork, Guid userId, CancellationToken cancellationToken)
    {
        var artist = await _artistRepository.GetByUserIdAsync(userId, cancellationToken);

        if (artist is null || artist.Id != artwork.ArtistProfileId)
            throw new UnauthorizedAccessException("Bu əsəri idarə etmək icazəniz yoxdur.");
    }
    private static ArtworkDto MapToDto(Artwork artwork)
    {
        return new ArtworkDto
        {
            Id = artwork.Id,
            ArtistProfileId = artwork.ArtistProfileId,
            Title = artwork.Title,
            Description = artwork.Description,
            Price = artwork.Price,
            WidthCm = artwork.WidthCm,
            HeightCm = artwork.HeightCm,
            City = artwork.City,
            IsFramed = artwork.IsFramed,
            ModerationStatus = artwork.ModerationStatus,
            SaleStatus = artwork.SaleStatus,
            CreatedAt = artwork.CreatedAt
        };
    }
}