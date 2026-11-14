using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Abstractions.Persistence;
using Drawing.Application.Features.Artists.DTOs;
using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using Drawing.Domain.Exceptions;

namespace Drawing.Application.Features.Artists.Services;
public sealed class ArtistService : IArtistService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    public ArtistService(IArtistRepository artistRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _artistRepository = artistRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }
    public async Task<ArtistProfileDto> ApplyAsync(ApplyArtistRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new DomainException(
                "Rəssam adı tələb olunur.");

        if (request.DisplayName.Trim().Length > 200)
            throw new DomainException(
                "Rəssam adı 200 simvoldan uzun ola bilməz.");

        if (request.Biography?.Length > 5000)
            throw new DomainException(
                "Bioqrafiya 5000 simvoldan uzun ola bilməz.");

        if (string.IsNullOrWhiteSpace(request.City))
            throw new DomainException(
                "Şəhər tələb olunur.");

        if (request.City.Trim().Length > 100)
            throw new DomainException(
                "Şəhər adı 100 simvoldan uzun ola bilməz.");

        if (request.ProfileImageUrl?.Length > 1000)
            throw new DomainException(
                "Şəkil ünvanı çox uzundur.");

        var existingArtist =
            await _artistRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (existingArtist is not null)
            throw new DomainException(
                "Bu istifadəçi üçün artıq rəssam profili mövcuddur.");

        var artist = new ArtistProfile
        {
            UserId = userId,
            DisplayName = request.DisplayName.Trim(),
            Biography = request.Biography?.Trim(),
            City = request.City.Trim(),
            ProfileImageUrl = request.ProfileImageUrl?.Trim()
        };

        await _artistRepository.AddAsync(
            artist,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return MapToDto(artist);
    }
    public async Task<ArtistProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        var artist = await _artistRepository.GetByUserIdAsync(userId, cancellationToken);

        return artist is null ? null : MapToDto(artist);
    }
    public async Task<IReadOnlyList<ArtistProfileDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        GetRequiredUserId();

        var artists = await _artistRepository.GetPendingAsync(cancellationToken);

        return artists.Select(MapToDto).ToList();
    }
    public async Task ApproveAsync(Guid artistId, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetRequiredUserId();

        var artist = await _artistRepository.GetByIdAsync(artistId, cancellationToken);

        if (artist is null)
            throw new KeyNotFoundException("Rəssam profili tapılmadı.");

        artist.Approve(adminUserId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
    public async Task RejectAsync(Guid artistId, string reason, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetRequiredUserId();

        var artist = await _artistRepository.GetByIdAsync(artistId, cancellationToken);

        if (artist is null)
            throw new KeyNotFoundException("Rəssam profili tapılmadı.");

        artist.Reject(adminUserId, reason);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
    private Guid GetRequiredUserId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId is not Guid userId ||
            userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("İstifadəçi sistemə daxil olmayıb.");
        }

        return userId;
    }
    private static ArtistProfileDto MapToDto(ArtistProfile artist)
    {
        return new ArtistProfileDto
        {
            Id = artist.Id,
            UserId = artist.UserId,
            DisplayName = artist.DisplayName,
            Biography = artist.Biography,
            City = artist.City,
            ProfileImageUrl = artist.ProfileImageUrl,
            Status = artist.Status,
            RejectionReason = artist.RejectionReason,
            CreatedAt = artist.CreatedAt,
            ReviewedAt = artist.ReviewedAt
        };
    }
}