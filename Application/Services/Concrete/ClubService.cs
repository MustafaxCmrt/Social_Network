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

    public async Task<ClubDto> CreateClubDirectAsync(CreateClubDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.Role != Roles.Admin)
            throw new UnauthorizedAccessException("Bu işlem için admin yetkisi gerekiyor");

        // Aynı isimde kulüp var mı kontrol et
        var slug = GenerateSlug(dto.Name);
        var existingClub = await _unitOfWork.Clubs.FirstOrDefaultAsync(
            c => c.Name.ToLower() == dto.Name.ToLower() || c.Slug == slug,
            cancellationToken);

        if (existingClub != null)
            throw new InvalidOperationException("Bu isimde bir kulüp zaten mevcut");

        var club = new Clubs
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            RequiresApproval = dto.RequiresApproval,
            MemberCount = 1,
            FounderId = userId
        };

        await _unitOfWork.Clubs.CreateAsync(club, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Admin'i başkan olarak ekle
        var membership = new ClubMemberships
        {
            ClubId = club.Id,
            UserId = userId,
            Role = ClubRole.President,
            Status = MembershipStatus.Approved,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.ClubMemberships.CreateAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {UserId} tarafından {ClubName} kulübü doğrudan oluşturuldu", userId, club.Name);

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
            user.Username,
            club.CreatedAt,
            club.ApplicationStatus,
            club.RejectionReason,
            club.ReviewedAt,
            true,
            ClubRole.President,
            MembershipStatus.Approved
        );
    }

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
            include: query => query.Include(c => c.Founder),
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
            c.IsPublic,
            c.FounderId,
            c.Founder?.Username ?? "Bilinmiyor",
            c.ApplicationStatus,
            c.RejectionReason,
            c.ReviewedAt
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

    public async Task<ClubDto?> GetClubByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        Clubs? club;

        if (int.TryParse(identifier, out var id))
            club = await _unitOfWork.Clubs.GetByIdAsync(id, cancellationToken);
        else
            club = await _unitOfWork.Clubs.FirstOrDefaultAsync(c => c.Slug == identifier, cancellationToken);

        if (club == null)
            return null;

        var founder = await _unitOfWork.Users.GetByIdAsync(club.FounderId, cancellationToken);

        // Giriş yapmış kullanıcının üyelik durumunu ekle
        var userId = _currentUserService.GetCurrentUserId();
        bool? isMember = null;
        ClubRole? currentUserRole = null;
        MembershipStatus? currentUserStatus = null;

        if (userId != null)
        {
            var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
                m => m.ClubId == club.Id && m.UserId == userId,
                cancellationToken);

            if (membership != null)
            {
                isMember = membership.Status == MembershipStatus.Approved;
                currentUserRole = membership.Role;
                currentUserStatus = membership.Status;
            }
            else
            {
                isMember = false;
            }
        }

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
            club.CreatedAt,
            club.ApplicationStatus,
            club.RejectionReason,
            club.ReviewedAt,
            isMember,
            currentUserRole,
            currentUserStatus
        );
    }

    public async Task<ClubDto?> UpdateClubAsync(UpdateClubDto dto, CancellationToken cancellationToken = default)
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

        // Name güncellemesi (varsa slug'ı da güncelle)
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            // Aynı isimde başka kulüp var mı kontrol et
            var existingClub = await _unitOfWork.Clubs.FirstOrDefaultAsync(
                c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != dto.Id,
                cancellationToken);

            if (existingClub != null)
                throw new InvalidOperationException("Bu isimde bir kulüp zaten mevcut");

            club.Name = dto.Name;
            club.Slug = GenerateSlug(dto.Name);
        }

        if (!string.IsNullOrWhiteSpace(dto.Description))
            club.Description = dto.Description;

        if (!string.IsNullOrWhiteSpace(dto.LogoUrl))
            club.LogoUrl = dto.LogoUrl;

        if (!string.IsNullOrWhiteSpace(dto.BannerUrl))
            club.BannerUrl = dto.BannerUrl;

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
            club.CreatedAt,
            club.ApplicationStatus,
            club.RejectionReason,
            club.ReviewedAt,
            null,
            null,
            null
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

    public async Task<string?> UploadClubImageAsync(int clubId, string imageUrl, string imageType, CancellationToken cancellationToken = default)
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

        if (imageType == "logo")
            club.LogoUrl = imageUrl;
        else if (imageType == "banner")
            club.BannerUrl = imageUrl;
        else
            throw new InvalidOperationException("Geçersiz resim tipi. 'logo' veya 'banner' olmalıdır");

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
            club.MemberCount = currentCount + 1;
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

    public async Task<PagedResultDto<ClubMemberDto>> GetClubMembersAsync(int clubId, int page, int pageSize, MembershipStatus? status = null, CancellationToken cancellationToken = default)
    {
        // Varsayılan olarak onaylı üyeleri getir
        var filterStatus = status ?? MembershipStatus.Approved;

        // Bekleyen üyeleri görmek için yönetici yetkisi gerekir
        if (filterStatus == MembershipStatus.Pending)
        {
            var userId = _currentUserService.GetCurrentUserId()
                ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

            var userMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
                m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
                cancellationToken);

            if (userMembership == null ||
                (userMembership.Role != ClubRole.President &&
                 userMembership.Role != ClubRole.VicePresident &&
                 userMembership.Role != ClubRole.Officer))
                throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekiyor");
        }

        var (memberships, totalCount) = await _unitOfWork.ClubMemberships.FindPagedAsync(
            predicate: m => m.ClubId == clubId && m.Status == filterStatus,
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

    public async Task<ClubMemberDto?> ProcessMembershipAsync(ProcessMembershipDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var membership = await _unitOfWork.ClubMemberships.GetByIdAsync(dto.MembershipId, cancellationToken);
        if (membership == null)
            throw new KeyNotFoundException("Üyelik bulunamadı");

        // Kick işlemi için üyelik Approved olmalı, diğerleri için Pending
        if (dto.Action == MembershipAction.Kick)
        {
            if (membership.Status != MembershipStatus.Approved)
                throw new InvalidOperationException("Sadece aktif üyeler çıkarılabilir");

            if (membership.Role == ClubRole.President)
                throw new InvalidOperationException("Başkan çıkarılamaz");

            // Kick için başkan veya yardımcı başkan yetkisi gerekir
            var kickerMembership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
                m => m.ClubId == membership.ClubId && m.UserId == userId && m.Status == MembershipStatus.Approved,
                cancellationToken);

            if (kickerMembership == null ||
                (kickerMembership.Role != ClubRole.President && kickerMembership.Role != ClubRole.VicePresident))
                throw new UnauthorizedAccessException("Bu işlem için başkan veya yardımcı başkan yetkisi gerekiyor");

            membership.Status = MembershipStatus.Kicked;
            _unitOfWork.ClubMemberships.Update(membership);

            // Üye sayısını güncelle
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
        }
        else
        {
            // Approve/Reject için üyelik Pending olmalı
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

            var isApprove = dto.Action == MembershipAction.Approve;
            membership.Status = isApprove ? MembershipStatus.Approved : MembershipStatus.Rejected;
            membership.JoinedAt = isApprove ? DateTime.UtcNow : null;

            _unitOfWork.ClubMemberships.Update(membership);

            // Onaylandıysa üye sayısını güncelle
            if (isApprove)
            {
                var club = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
                if (club != null)
                {
                    var currentCount = await _unitOfWork.ClubMemberships.CountAsync(
                        m => m.ClubId == membership.ClubId && m.Status == MembershipStatus.Approved,
                        cancellationToken);
                    club.MemberCount = currentCount + 1;
                    _unitOfWork.Clubs.Update(club);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Başvurana bildirim gönder
            var notifClub = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = membership.UserId,
                ActorUserId = userId,
                Type = isApprove ? NotificationType.ClubMembershipApproved : NotificationType.ClubMembershipRejected,
                Message = isApprove
                    ? $"{notifClub?.Name} kulübüne katılma başvurunuz onaylandı!"
                    : $"{notifClub?.Name} kulübüne katılma başvurunuz reddedildi"
            }, cancellationToken);
        }

        var memberUser = await _unitOfWork.Users.GetByIdAsync(membership.UserId, cancellationToken);

        return new ClubMemberDto(
            membership.Id,
            membership.UserId,
            memberUser?.Username ?? "Bilinmiyor",
            memberUser?.FirstName ?? "Bilinmiyor",
            memberUser?.LastName ?? "",
            memberUser?.ProfileImg,
            membership.Role,
            membership.Status,
            membership.JoinedAt,
            membership.JoinNote
        );
    }

    public async Task<ClubMemberDto?> UpdateMemberRoleAsync(UpdateMemberRoleDto dto, CancellationToken cancellationToken = default)
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

        // Başkanlık devri
        if (dto.NewRole == ClubRole.President)
        {
            userMembership.Role = ClubRole.Member;
            membership.Role = ClubRole.President;

            _unitOfWork.ClubMemberships.Update(userMembership);
            _unitOfWork.ClubMemberships.Update(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var club = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = membership.UserId,
                ActorUserId = userId,
                Type = NotificationType.ClubPresidencyTransferred,
                Message = $"{club?.Name} kulübünün yeni başkanı oldunuz!"
            }, cancellationToken);
        }
        else
        {
            membership.Role = dto.NewRole;
            _unitOfWork.ClubMemberships.Update(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var club = await _unitOfWork.Clubs.GetByIdAsync(membership.ClubId, cancellationToken);
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = membership.UserId,
                ActorUserId = userId,
                Type = NotificationType.ClubRoleChanged,
                Message = $"{club?.Name} kulübündeki rolünüz {dto.NewRole} olarak değiştirildi"
            }, cancellationToken);
        }

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

    // ==================== KULÜP BAŞVURU DURUMU GÜNCELLEMELERİ ====================

    public async Task<bool> UpdateClubApplicationStatusAsync(int clubId, UpdateClubApplicationStatusDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || (user.Role != Roles.Moderator && user.Role != Roles.Admin))
            throw new UnauthorizedAccessException("Bu işlem için moderatör veya admin yetkisi gerekiyor");

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
            return false;

        club.ApplicationStatus = dto.Status;
        club.ReviewedAt = DateTime.UtcNow;
        club.ReviewedBy = userId;
        club.RejectionReason = dto.Status == ClubApplicationStatus.Rejected ? dto.RejectionReason : null;

        _unitOfWork.Clubs.Update(club);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Kulüp sahibine bildirim gönder
        var notificationType = dto.Status switch
        {
            ClubApplicationStatus.Approved => NotificationType.ClubRequestApproved,
            ClubApplicationStatus.Rejected => NotificationType.ClubRequestRejected,
            _ => NotificationType.ClubRequestReceived
        };

        var message = dto.Status switch
        {
            ClubApplicationStatus.Approved => $"{club.Name} kulübünüz onaylandı!",
            ClubApplicationStatus.Rejected => $"{club.Name} kulübünüz reddedildi. Sebep: {dto.RejectionReason}",
            _ => $"{club.Name} kulübünüz inceleme altına alındı"
        };

        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
        {
            UserId = club.FounderId,
            ActorUserId = userId,
            Type = notificationType,
            Message = message
        }, cancellationToken);

        _logger.LogInformation(
            "Kulüp {ClubId} başvuru durumu {ReviewerId} tarafından {Status} olarak güncellendi",
            clubId, userId, dto.Status);

        return true;
    }

    public async Task<PagedResultDto<ClubListDto>> GetUserClubApplicationsAsync(int page, int pageSize, ClubApplicationStatus? status = null, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        Expression<Func<Clubs, bool>> predicate = c => c.FounderId == userId;

        // Status filtresi varsa ekle
        if (status.HasValue)
        {
            var statusValue = status.Value;
            predicate = c => c.FounderId == userId && c.ApplicationStatus == statusValue;
        }

        var (clubs, totalCount) = await _unitOfWork.Clubs.FindPagedAsync(
            predicate: predicate,
            include: query => query.Include(c => c.Founder),
            orderBy: q => q.OrderByDescending(c => c.CreatedAt),
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
            c.IsPublic,
            c.FounderId,
            c.Founder?.Username ?? "Bilinmiyor",
            c.ApplicationStatus,
            c.RejectionReason,
            c.ReviewedAt
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

    public async Task<PagedResultDto<ClubMemberDto>> GetPendingMembershipsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Oturum açmanız gerekiyor");

        // Kullanıcının yönetici/moderatör olduğu kulüpleri al
        var userRoles = await _unitOfWork.Users
            .FindAsync(u => u.Id == userId, cancellationToken: cancellationToken);
        
        var user = userRoles.FirstOrDefault();
        if (user == null)
            throw new UnauthorizedAccessException("Kullanıcı bulunamadı");

        var isAdminOrModerator = user.Role == Roles.Admin || user.Role == Roles.Moderator;

        // Admin/Moderator ise tüm pending başvuruları görebilir
        // Normal kullanıcı ise sadece yönettiği kulüplerin pending başvurularını görebilir
        Expression<Func<ClubMemberships, bool>> predicate;

        if (isAdminOrModerator)
        {
            // Admin/Moderator tüm pending başvuruları görebilir
            predicate = cm => cm.Status == MembershipStatus.Pending;
        }
        else
        {
            // Normal kullanıcı sadece başkan/başkan yardımcısı/yönetim kurulu üyesi olduğu kulüplerin başvurularını görebilir
            var managedClubIds = await _unitOfWork.ClubMemberships
                .FindAsync(
                    cm => cm.UserId == userId && 
                          (cm.Role == ClubRole.President || cm.Role == ClubRole.VicePresident || cm.Role == ClubRole.Officer) &&
                          cm.Status == MembershipStatus.Approved,
                    cancellationToken: cancellationToken);

            var clubIds = managedClubIds.Select(cm => cm.ClubId).ToList();

            if (!clubIds.Any())
            {
                // Yönettiği kulüp yoksa boş liste döndür
                return new PagedResultDto<ClubMemberDto>
                {
                    Items = new List<ClubMemberDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }

            predicate = cm => clubIds.Contains(cm.ClubId) && cm.Status == MembershipStatus.Pending;
        }

        var (memberships, totalCount) = await _unitOfWork.ClubMemberships.FindPagedAsync(
            predicate: predicate,
            include: query => query
                .Include(cm => cm.User)
                .Include(cm => cm.Club),
            orderBy: q => q.OrderBy(cm => cm.CreatedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var items = memberships.Select(cm => new ClubMemberDto(
            cm.Id,
            cm.UserId,
            cm.User.Username,
            cm.User.FirstName,
            cm.User.LastName,
            cm.User.ProfileImg,
            cm.Role,
            cm.Status,
            cm.JoinedAt,
            cm.JoinNote
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
}
