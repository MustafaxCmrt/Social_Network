using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Application.DTOs.Club;
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Kulüp yönetimi servisi implementasyonu
/// </summary>
public class ClubService : IClubService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ClubService> _logger;

    public ClubService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ILogger<ClubService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ==================== KULÜP BAŞVURU İŞLEMLERİ ====================

    public async Task<ClubRequestListDto> CreateClubRequestAsync(CreateClubRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        // Aynı isimde bekleyen başvuru var mı kontrol et
        var existingRequest = await _unitOfWork.ClubRequests.FirstOrDefaultAsync(
            cr => cr.Name.ToLower() == dto.Name.ToLower() && cr.Status == ClubRequestStatus.Pending,
            cancellationToken);

        if (existingRequest != null)
            throw new InvalidOperationException("Bu isimde bekleyen bir kulüp başvurusu zaten var");

        // Aynı isimde kulüp var mı kontrol et
        var existingClub = await _unitOfWork.Clubs.FirstOrDefaultAsync(
            c => c.Name.ToLower() == dto.Name.ToLower(),
            cancellationToken);

        if (existingClub != null)
            throw new InvalidOperationException("Bu isimde bir kulüp zaten mevcut");

        var clubRequest = new ClubRequests
        {
            Name = dto.Name,
            Description = dto.Description,
            Purpose = dto.Purpose,
            Status = ClubRequestStatus.Pending,
            RequestedByUserId = userId
        };

        await _unitOfWork.ClubRequests.CreateAsync(clubRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Moderatörlere bildirim gönder
        var moderators = await _unitOfWork.Users.FindAsync(
            u => u.Role == Roles.Moderator || u.Role == Roles.Admin,
            cancellationToken);

        foreach (var mod in moderators)
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = mod.Id,
                ActorUserId = userId,
                Type = NotificationType.ClubRequestReceived,
                Message = $"{dto.Name} adında yeni bir kulüp başvurusu var"
            }, cancellationToken);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        return new ClubRequestListDto(
            clubRequest.Id,
            clubRequest.Name,
            clubRequest.Description,
            clubRequest.Purpose,
            clubRequest.Status,
            clubRequest.RequestedByUserId,
            user?.Username ?? "Bilinmiyor",
            clubRequest.CreatedAt,
            null,
            null,
            null,
            null
        );
    }

    public async Task<PagedResultDto<ClubRequestListDto>> GetPendingClubRequestsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || (user.Role != Roles.Moderator && user.Role != Roles.Admin))
            throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok");

        var (requests, totalCount) = await _unitOfWork.ClubRequests.FindPagedAsync(
            predicate: cr => cr.Status == ClubRequestStatus.Pending,
            include: query => query
                .Include(cr => cr.RequestedByUser)
                .Include(cr => cr.ReviewedByUser),
            orderBy: q => q.OrderByDescending(cr => cr.CreatedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken);

        var items = requests.Select(cr => new ClubRequestListDto(
            cr.Id,
            cr.Name,
            cr.Description,
            cr.Purpose,
            cr.Status,
            cr.RequestedByUserId,
            cr.RequestedByUser.Username,
            cr.CreatedAt,
            cr.ReviewedByUserId,
            cr.ReviewedByUser?.Username,
            cr.ReviewedAt,
            cr.RejectionReason
        )).ToList();

        return new PagedResultDto<ClubRequestListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResultDto<ClubRequestListDto>> GetMyClubRequestsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var (requests, totalCount) = await _unitOfWork.ClubRequests.FindPagedAsync(
            predicate: cr => cr.RequestedByUserId == userId,
            include: query => query
                .Include(cr => cr.RequestedByUser)
                .Include(cr => cr.ReviewedByUser),
            orderBy: q => q.OrderByDescending(cr => cr.CreatedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken);

        var items = requests.Select(cr => new ClubRequestListDto(
            cr.Id,
            cr.Name,
            cr.Description,
            cr.Purpose,
            cr.Status,
            cr.RequestedByUserId,
            cr.RequestedByUser.Username,
            cr.CreatedAt,
            cr.ReviewedByUserId,
            cr.ReviewedByUser?.Username,
            cr.ReviewedAt,
            cr.RejectionReason
        )).ToList();

        return new PagedResultDto<ClubRequestListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ClubRequestListDto> ReviewClubRequestAsync(ReviewClubRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || (user.Role != Roles.Moderator && user.Role != Roles.Admin))
            throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok");

        var request = await _unitOfWork.ClubRequests.GetByIdAsync(dto.RequestId, cancellationToken);
        if (request == null)
            throw new KeyNotFoundException("Kulüp başvurusu bulunamadı");

        if (request.Status != ClubRequestStatus.Pending)
            throw new InvalidOperationException("Bu başvuru zaten incelenmiş");

        request.Status = dto.Approve ? ClubRequestStatus.Approved : ClubRequestStatus.Rejected;
        request.ReviewedByUserId = userId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = dto.Approve ? null : dto.RejectionReason;

        _unitOfWork.ClubRequests.Update(request);

        // Onaylandıysa kulübü oluştur
        if (dto.Approve)
        {
            var slug = GenerateSlug(request.Name);
            var club = new Clubs
            {
                Name = request.Name,
                Slug = slug,
                Description = request.Description,
                IsPublic = true,
                RequiresApproval = false,
                MemberCount = 1,
                FounderId = request.RequestedByUserId
            };

            await _unitOfWork.Clubs.CreateAsync(club, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Başvuran kişiyi başkan olarak ekle
            var membership = new ClubMemberships
            {
                ClubId = club.Id,
                UserId = request.RequestedByUserId,
                Role = ClubRole.President,
                Status = MembershipStatus.Approved,
                JoinedAt = DateTime.UtcNow
            };

            await _unitOfWork.ClubMemberships.CreateAsync(membership, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Başvurana bildirim gönder
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
        {
            UserId = request.RequestedByUserId,
            ActorUserId = userId,
            Type = dto.Approve ? NotificationType.ClubRequestApproved : NotificationType.ClubRequestRejected,
            Message = dto.Approve
                ? $"{request.Name} kulüp başvurunuz onaylandı!"
                : $"{request.Name} kulüp başvurunuz reddedildi. Sebep: {dto.RejectionReason}"
        }, cancellationToken);

        var reviewedByUser = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        var requestedByUser = await _unitOfWork.Users.GetByIdAsync(request.RequestedByUserId, cancellationToken);

        return new ClubRequestListDto(
            request.Id,
            request.Name,
            request.Description,
            request.Purpose,
            request.Status,
            request.RequestedByUserId,
            requestedByUser?.Username ?? "Bilinmiyor",
            request.CreatedAt,
            request.ReviewedByUserId,
            reviewedByUser?.Username,
            request.ReviewedAt,
            request.RejectionReason
        );
    }

    // ==================== KULÜP CRUD İŞLEMLERİ ====================

    public async Task<PagedResultDto<ClubListDto>> GetAllClubsAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        Expression<Func<Clubs, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            predicate = c => c.Name.ToLower().Contains(searchLower) ||
                             c.Description!.ToLower().Contains(searchLower);
        }

        var (clubs, totalCount) = await _unitOfWork.Clubs.FindPagedAsync(
            predicate: predicate,
            orderBy: q => q.OrderBy(c => c.Name),
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var items = clubs.Select(c => new ClubListDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.LogoUrl,
            c.MemberCount,
            c.IsPublic
        )).ToList();

        return new PagedResultDto<ClubListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ClubDto?> GetClubByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var club = await _unitOfWork.Clubs.GetByIdAsync(id, cancellationToken);

        if (club == null)
            return null;

        var founder = await _unitOfWork.Users.GetByIdAsync(club.FounderId, cancellationToken);

        return new ClubDto(
            club.Id,
            club.Name,
            club.Slug,
            club.Description,
            club.LogoUrl,
            club.BannerUrl,
            club.IsPublic,
            club.RequiresApproval,
            club.MemberCount,
            club.FounderId,
            founder?.Username ?? "Bilinmiyor",
            club.CreatedAt
        );
    }

    public async Task<ClubDto?> GetClubBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var club = await _unitOfWork.Clubs.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

        if (club == null)
            return null;

        var founder = await _unitOfWork.Users.GetByIdAsync(club.FounderId, cancellationToken);

        return new ClubDto(
            club.Id,
            club.Name,
            club.Slug,
            club.Description,
            club.LogoUrl,
            club.BannerUrl,
            club.IsPublic,
            club.RequiresApproval,
            club.MemberCount,
            club.FounderId,
            founder?.Username ?? "Bilinmiyor",
            club.CreatedAt
        );
    }

    public async Task<ClubDto> UpdateClubAsync(UpdateClubDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        var isAdmin = user?.Role == Roles.Admin;

        var club = await _unitOfWork.Clubs.GetByIdAsync(dto.Id, cancellationToken);
        if (club == null)
            throw new KeyNotFoundException("Kulüp bulunamadı");

        // Başkan veya Admin kontrolü
        var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == dto.Id && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (!isAdmin && (membership == null || membership.Role != ClubRole.President))
            throw new UnauthorizedAccessException("Bu işlem için başkan veya admin yetkisi gerekiyor");

        if (dto.Description != null)
            club.Description = dto.Description;
        if (dto.IsPublic.HasValue)
            club.IsPublic = dto.IsPublic.Value;
        if (dto.RequiresApproval.HasValue)
            club.RequiresApproval = dto.RequiresApproval.Value;

        _unitOfWork.Clubs.Update(club);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var founder = await _unitOfWork.Users.GetByIdAsync(club.FounderId, cancellationToken);

        return new ClubDto(
            club.Id,
            club.Name,
            club.Slug,
            club.Description,
            club.LogoUrl,
            club.BannerUrl,
            club.IsPublic,
            club.RequiresApproval,
            club.MemberCount,
            club.FounderId,
            founder?.Username ?? "Bilinmiyor",
            club.CreatedAt
        );
    }

    public async Task<bool> DeleteClubAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user?.Role != Roles.Admin)
            throw new UnauthorizedAccessException("Bu işlem için admin yetkisi gerekiyor");

        var club = await _unitOfWork.Clubs.GetByIdAsync(id, cancellationToken);
        if (club == null)
            return false;

        _unitOfWork.Clubs.Delete(club);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<string> UploadClubLogoAsync(int clubId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
            throw new KeyNotFoundException("Kulüp bulunamadı");

        var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (membership == null || membership.Role != ClubRole.President)
            throw new UnauthorizedAccessException("Bu işlem için başkan yetkisi gerekiyor");

        club.LogoUrl = imageUrl;
        _unitOfWork.Clubs.Update(club);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }

    public async Task<string> UploadClubBannerAsync(int clubId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
            throw new KeyNotFoundException("Kulüp bulunamadı");

        var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (membership == null || membership.Role != ClubRole.President)
            throw new UnauthorizedAccessException("Bu işlem için başkan yetkisi gerekiyor");

        club.BannerUrl = imageUrl;
        _unitOfWork.Clubs.Update(club);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }

    // ==================== ÜYELİK İŞLEMLERİ ====================

    public async Task<ClubMemberDto> JoinClubAsync(JoinClubDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var club = await _unitOfWork.Clubs.GetByIdAsync(dto.ClubId, cancellationToken);
        if (club == null)
            throw new KeyNotFoundException("Kulüp bulunamadı");

        // Zaten üye mi kontrol et
        var existingMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == dto.ClubId && m.UserId == userId,
            cancellationToken);

        if (existingMembership != null)
        {
            if (existingMembership.Status == MembershipStatus.Approved)
                throw new InvalidOperationException("Zaten bu kulübün üyesisiniz");
            if (existingMembership.Status == MembershipStatus.Pending)
                throw new InvalidOperationException("Bekleyen bir başvurunuz zaten var");
            if (existingMembership.Status == MembershipStatus.Kicked)
                throw new InvalidOperationException("Bu kulüpten çıkarıldığınız için tekrar katılamazsınız");
        }

        var status = club.RequiresApproval ? MembershipStatus.Pending : MembershipStatus.Approved;
        var membership = new ClubMemberships
        {
            ClubId = dto.ClubId,
            UserId = userId,
            Role = ClubRole.Member,
            Status = status,
            JoinNote = dto.JoinNote,
            JoinedAt = status == MembershipStatus.Approved ? DateTime.UtcNow : null
        };

        await _unitOfWork.ClubMemberships.CreateAsync(membership, cancellationToken);

        // Onay gerektirmiyorsa üye sayısını artır
        if (status == MembershipStatus.Approved)
        {
            var currentCount = await _unitOfWork.ClubMemberships.CountAsync(
                m => m.ClubId == dto.ClubId && m.Status == MembershipStatus.Approved,
                cancellationToken);
            club.MemberCount = currentCount + 1; // +1 çünkü yeni üyelik henüz save edilmedi
            _unitOfWork.Clubs.Update(club);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Kulüp yöneticilerine bildirim gönder (onay gerekiyorsa)
        if (club.RequiresApproval)
        {
            var officers = await _unitOfWork.ClubMemberships.FindAsync(
                m => m.ClubId == dto.ClubId &&
                     m.Status == MembershipStatus.Approved &&
                     (m.Role == ClubRole.President || m.Role == ClubRole.VicePresident || m.Role == ClubRole.Officer),
                cancellationToken);

            foreach (var officer in officers)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = officer.UserId,
                    ActorUserId = userId,
                    Type = NotificationType.ClubMembershipRequest,
                    Message = $"{club.Name} kulübüne yeni üyelik başvurusu var"
                }, cancellationToken);
            }
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        return new ClubMemberDto(
            membership.Id,
            membership.UserId,
            user?.Username ?? "Bilinmiyor",
            user?.FirstName ?? "Bilinmiyor",
            user?.LastName ?? "",
            user?.ProfileImg,
            membership.Role,
            membership.Status,
            membership.JoinedAt,
            membership.JoinNote
        );
    }

    public async Task<bool> LeaveClubAsync(int clubId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (membership == null)
            throw new InvalidOperationException("Bu kulübün üyesi değilsiniz");

        if (membership.Role == ClubRole.President)
            throw new InvalidOperationException("Başkan olduğunuz kulüpten ayrılamazsınız. Önce başkanlığı devredin.");

        membership.Status = MembershipStatus.Left;
        _unitOfWork.ClubMemberships.Update(membership);

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club != null)
        {
            var currentCount = await _unitOfWork.ClubMemberships.CountAsync(
                m => m.ClubId == clubId && m.Status == MembershipStatus.Approved && m.Id != membership.Id,
                cancellationToken);
            club.MemberCount = currentCount;
            _unitOfWork.Clubs.Update(club);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<PagedResultDto<ClubMemberDto>> GetClubMembersAsync(int clubId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (memberships, totalCount) = await _unitOfWork.ClubMemberships.FindPagedAsync(
            predicate: m => m.ClubId == clubId && m.Status == MembershipStatus.Approved,
            include: query => query.Include(m => m.User),
            orderBy: q => q.OrderByDescending(m => m.Role).ThenBy(m => m.JoinedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken);

        var items = memberships.Select(m => new ClubMemberDto(
            m.Id,
            m.UserId,
            m.User.Username,
            m.User.FirstName,
            m.User.LastName,
            m.User.ProfileImg,
            m.Role,
            m.Status,
            m.JoinedAt,
            m.JoinNote
        )).ToList();

        return new PagedResultDto<ClubMemberDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResultDto<ClubMemberDto>> GetPendingMembersAsync(int clubId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        // Kulüp yöneticisi mi kontrol et
        var userMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (userMembership == null ||
            (userMembership.Role != ClubRole.President &&
             userMembership.Role != ClubRole.VicePresident &&
             userMembership.Role != ClubRole.Officer))
            throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekiyor");

        var (memberships, totalCount) = await _unitOfWork.ClubMemberships.FindPagedAsync(
            predicate: m => m.ClubId == clubId && m.Status == MembershipStatus.Pending,
            include: query => query.Include(m => m.User),
            orderBy: q => q.OrderBy(m => m.CreatedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken);

        var items = memberships.Select(m => new ClubMemberDto(
            m.Id,
            m.UserId,
            m.User.Username,
            m.User.FirstName,
            m.User.LastName,
            m.User.ProfileImg,
            m.Role,
            m.Status,
            m.JoinedAt,
            m.JoinNote
        )).ToList();

        return new PagedResultDto<ClubMemberDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ClubMemberDto> ProcessMembershipAsync(ProcessMembershipDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var membership = await _unitOfWork.ClubMemberships.GetByIdAsync(dto.MembershipId, cancellationToken);
        if (membership == null)
            throw new KeyNotFoundException("Üyelik başvurusu bulunamadı");

        if (membership.Status != MembershipStatus.Pending)
            throw new InvalidOperationException("Bu başvuru zaten işlenmiş");

        // Kulüp yöneticisi mi kontrol et
        var userMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == membership.ClubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (userMembership == null ||
            (userMembership.Role != ClubRole.President &&
             userMembership.Role != ClubRole.VicePresident &&
             userMembership.Role != ClubRole.Officer))
            throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekiyor");

        membership.Status = dto.Approve ? MembershipStatus.Approved : MembershipStatus.Rejected;
        membership.JoinedAt = dto.Approve ? DateTime.UtcNow : null;

        _unitOfWork.ClubMemberships.Update(membership);

        // Onaylandıysa üye sayısını güncelle
        if (dto.Approve)
        {
            var club = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
            if (club != null)
            {
                var currentCount = await _unitOfWork.ClubMemberships.CountAsync(
                    m => m.ClubId == membership.ClubId && m.Status == MembershipStatus.Approved,
                    cancellationToken);
                club.MemberCount = currentCount + 1; // +1 çünkü status henüz save edilmedi
                _unitOfWork.Clubs.Update(club);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Başvurana bildirim gönder
        var club2 = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
        {
            UserId = membership.UserId,
            ActorUserId = userId,
            Type = dto.Approve ? NotificationType.ClubMembershipApproved : NotificationType.ClubMembershipRejected,
            Message = dto.Approve
                ? $"{club2?.Name} kulübüne katılma başvurunuz onaylandı!"
                : $"{club2?.Name} kulübüne katılma başvurunuz reddedildi"
        }, cancellationToken);

        var user = await _unitOfWork.Users.GetByIdAsync(membership.UserId, cancellationToken);

        return new ClubMemberDto(
            membership.Id,
            membership.UserId,
            user?.Username ?? "Bilinmiyor",
            user?.FirstName ?? "Bilinmiyor",
            user?.LastName ?? "",
            user?.ProfileImg,
            membership.Role,
            membership.Status,
            membership.JoinedAt,
            membership.JoinNote
        );
    }

    public async Task<ClubMemberDto> UpdateMemberRoleAsync(UpdateMemberRoleDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var membership = await _unitOfWork.ClubMemberships.GetByIdAsync(dto.MembershipId, cancellationToken);
        if (membership == null)
            throw new KeyNotFoundException("Üyelik bulunamadı");

        // Sadece başkan rol değiştirebilir
        var userMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == membership.ClubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (userMembership == null || userMembership.Role != ClubRole.President)
            throw new UnauthorizedAccessException("Bu işlem için başkan yetkisi gerekiyor");

        if (membership.Role == ClubRole.President)
            throw new InvalidOperationException("Başkanın rolü değiştirilemez");

        if (dto.NewRole == ClubRole.President)
            throw new InvalidOperationException("Başkanlık bu yöntemle devredilemez. TransferPresidency kullanın");

        membership.Role = dto.NewRole;
        _unitOfWork.ClubMemberships.Update(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Bildirim gönder
        var club = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
        {
            UserId = membership.UserId,
            ActorUserId = userId,
            Type = NotificationType.ClubRoleChanged,
            Message = $"{club?.Name} kulübündeki rolünüz {dto.NewRole} olarak değiştirildi"
        }, cancellationToken);

        var user = await _unitOfWork.Users.GetByIdAsync(membership.UserId, cancellationToken);

        return new ClubMemberDto(
            membership.Id,
            membership.UserId,
            user?.Username ?? "Bilinmiyor",
            user?.FirstName ?? "Bilinmiyor",
            user?.LastName ?? "",
            user?.ProfileImg,
            membership.Role,
            membership.Status,
            membership.JoinedAt,
            membership.JoinNote
        );
    }

    public async Task<bool> KickMemberAsync(KickMemberDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var membership = await _unitOfWork.ClubMemberships.GetByIdAsync(dto.MembershipId, cancellationToken);
        if (membership == null)
            throw new KeyNotFoundException("Üyelik bulunamadı");

        // Sadece başkan ve yardımcı başkan üye çıkarabilir
        var userMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == membership.ClubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (userMembership == null ||
            (userMembership.Role != ClubRole.President && userMembership.Role != ClubRole.VicePresident))
            throw new UnauthorizedAccessException("Bu işlem için başkan veya yardımcı başkan yetkisi gerekiyor");

        if (membership.Role == ClubRole.President)
            throw new InvalidOperationException("Başkan çıkarılamaz");

        membership.Status = MembershipStatus.Kicked;
        _unitOfWork.ClubMemberships.Update(membership);

        var club = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
        if (club != null)
        {
            var currentCount = await _unitOfWork.ClubMemberships.CountAsync(
                m => m.ClubId == membership.ClubId && m.Status == MembershipStatus.Approved && m.Id != membership.Id,
                cancellationToken);
            club.MemberCount = currentCount;
            _unitOfWork.Clubs.Update(club);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Bildirim gönder
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
        {
            UserId = membership.UserId,
            ActorUserId = userId,
            Type = NotificationType.ClubMemberKicked,
            Message = $"{club?.Name} kulübünden çıkarıldınız"
        }, cancellationToken);

        return true;
    }

    public async Task<bool> TransferPresidencyAsync(int clubId, int newPresidentUserId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        // Mevcut başkanın üyeliği
        var currentPresidentMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (currentPresidentMembership == null || currentPresidentMembership.Role != ClubRole.President)
            throw new UnauthorizedAccessException("Bu işlem için başkan olmanız gerekiyor");

        // Yeni başkanın üyeliği
        var newPresidentMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == newPresidentUserId && m.Status == MembershipStatus.Approved,
            cancellationToken);

        if (newPresidentMembership == null)
            throw new InvalidOperationException("Yeni başkan bu kulübün üyesi değil");

        // Rolleri değiştir
        currentPresidentMembership.Role = ClubRole.Member;
        newPresidentMembership.Role = ClubRole.President;

        _unitOfWork.ClubMemberships.Update(currentPresidentMembership);
        _unitOfWork.ClubMemberships.Update(newPresidentMembership);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Bildirim gönder
        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
        {
            UserId = newPresidentUserId,
            ActorUserId = userId,
            Type = NotificationType.ClubPresidencyTransferred,
            Message = $"{club?.Name} kulübünün yeni başkanı oldunuz!"
        }, cancellationToken);

        return true;
    }

    public async Task<MembershipStatusDto> GetMembershipStatusAsync(int clubId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId();

        if (userId == null)
        {
            return new MembershipStatusDto(
                false,
                null,
                null
            );
        }

        var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
            m => m.ClubId == clubId && m.UserId == userId,
            cancellationToken);

        if (membership == null)
        {
            return new MembershipStatusDto(
                false,
                null,
                null
            );
        }

        return new MembershipStatusDto(
            membership.Status == MembershipStatus.Approved,
            membership.Role,
            membership.Status
        );
    }

    public async Task<List<MyClubDto>> GetMyClubsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var memberships = await _unitOfWork.ClubMemberships.FindWithIncludesAsync(
            predicate: m => m.UserId == userId && m.Status == MembershipStatus.Approved,
            include: query => query.Include(m => m.Club),
            orderBy: q => q.OrderByDescending(m => m.Role).ThenBy(m => m.JoinedAt),
            cancellationToken);

        return memberships.Select(m => new MyClubDto(
            m.ClubId,
            m.Club.Name,
            m.Club.Slug,
            m.Club.LogoUrl,
            m.Role,
            m.Status,
            m.JoinedAt
        )).ToList();
    }

    // ==================== HELPER METHODS ====================

    private static string GenerateSlug(string name)
    {
        // Türkçe karakterleri değiştir
        var slug = name.ToLowerInvariant()
            .Replace('ç', 'c')
            .Replace('ğ', 'g')
            .Replace('ı', 'i')
            .Replace('ö', 'o')
            .Replace('ş', 's')
            .Replace('ü', 'u');

        // Özel karakterleri kaldır ve boşlukları tire yap
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
