using Application.DTOs.AuditLog;
using Application.DTOs.Category;
using Application.DTOs.Common;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.UnitOfWork;
using System.Text.RegularExpressions;

namespace Application.Services.Concrete;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public CategoryService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // PERFORMANS OPTİMİZASYONU: Include kaldırıldı
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri
        var categories = (await _unitOfWork.Categories.FindAsync(
            predicate: c => c.ClubId == null,
            cancellationToken: cancellationToken)).ToList();
        
        // SubCategory COUNT'ları RAM'de hesapla (kategori sayısı az)
        var subCategoryCounts = categories
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Thread COUNT'ları için her kategori için ayrı query
        // NOT: Kategori sayısı genelde az olduğu için (10-100) bu kabul edilebilir
        // Alternatif: Raw SQL ile tek query'de GROUP BY COUNT
        var result = new List<CategoryDto>();
        
        foreach (var c in categories.OrderByDescending(c => c.CreatedAt))
        {
            // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
            var threadCount = await _unitOfWork.Threads.CountAsync(
                t => t.CategoryId == c.Id && t.ClubId == null,
                cancellationToken);
            
            result.Add(new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = threadCount,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = subCategoryCounts.GetValueOrDefault(c.Id, 0)
            });
        }
        
        return result;
    }

    public async Task<PagedResultDto<CategoryDto>> GetAllCategoriesPaginatedAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        int? parentCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Max 50 kategori per page

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // PERFORMANS OPTİMİZASYONU: Include kaldırıldı - N+1 query sorunu çözüldü
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri (kulüp kategorileri hariç)
        var (categories, totalCount) = await _unitOfWork.Categories.FindPagedAsync(
            predicate: c =>
                (normalizedSearch == null
                 || c.Title.ToLower().Contains(normalizedSearch)
                 || (c.Description != null && c.Description.ToLower().Contains(normalizedSearch)))
                && (
                    parentCategoryId.HasValue
                        ? (parentCategoryId.Value == 0
                            ? c.ParentCategoryId == null
                            : c.ParentCategoryId == parentCategoryId.Value)
                        : c.ParentCategoryId == null
                )
                && c.ClubId == null, // Sadece normal forum kategorileri
            include: null, // Include kaldırıldı - performans optimizasyonu
            orderBy: q => q.OrderByDescending(c => c.CreatedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // PERFORMANS: Sadece bu sayfadaki kategoriler için COUNT'ları hesapla
        var items = new List<CategoryDto>();
        
        foreach (var c in categories)
        {
            // Her kategori için ayrı COUNT query (pagination olduğu için genelde 10-50 kategori max)
            // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
            var threadCount = await _unitOfWork.Threads.CountAsync(
                t => t.CategoryId == c.Id && t.ClubId == null,
                cancellationToken);
            
            var subCategoryCount = await _unitOfWork.Categories.CountAsync(
                cat => cat.ParentCategoryId == c.Id,
                cancellationToken);
            
            items.Add(new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = threadCount,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = subCategoryCount
            });
        }

        return new PagedResultDto<CategoryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // PERFORMANS OPTİMİZASYONU: Include kaldırıldı
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri
        var category = await _unitOfWork.Categories.FirstOrDefaultAsync(
            predicate: c => c.Id == id && c.ClubId == null,
            cancellationToken);

        if (category == null)
            return null;

        // COUNT'ları ayrı sorgularla al
        // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
        var threadCount = await _unitOfWork.Threads.CountAsync(
            t => t.CategoryId == id && t.ClubId == null,
            cancellationToken);
        
        var subCategoryCount = await _unitOfWork.Categories.CountAsync(
            c => c.ParentCategoryId == id,
            cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            CreatedUserId = category.CreatedUserId,
            UpdatedUserId = category.UpdatedUserId,
            ThreadCount = threadCount,
            ParentCategoryId = category.ParentCategoryId,
            SubCategoryCount = subCategoryCount
        };
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        // PERFORMANS OPTİMİZASYONU: Include kaldırıldı
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri
        var category = await _unitOfWork.Categories.FirstOrDefaultAsync(
            predicate: c => c.Slug == slug && c.ClubId == null,
            cancellationToken);

        if (category == null)
            return null;

        // COUNT'ları ayrı sorgularla al
        // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
        var threadCount = await _unitOfWork.Threads.CountAsync(
            t => t.CategoryId == category.Id && t.ClubId == null,
            cancellationToken);
        
        var subCategoryCount = await _unitOfWork.Categories.CountAsync(
            c => c.ParentCategoryId == category.Id,
            cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            CreatedUserId = category.CreatedUserId,
            UpdatedUserId = category.UpdatedUserId,
            ThreadCount = threadCount,
            ParentCategoryId = category.ParentCategoryId,
            SubCategoryCount = subCategoryCount
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default)
    {
        // Slug oluştur
        var slug = GenerateSlug(createCategoryDto.Title);

        // Aynı slug var mı kontrol et
        var existingCategory = await _unitOfWork.Categories.FirstOrDefaultAsync(
            c => c.Slug.ToLower() == slug.ToLower(), cancellationToken);

        if (existingCategory != null)
        {
            throw new InvalidOperationException($"'{createCategoryDto.Title}' başlıklı kategori zaten mevcut.");
        }
        
        // ParentCategory kontrolü
        if (createCategoryDto.ParentCategoryId.HasValue)
        {
            var parentCategory = await _unitOfWork.Categories.GetByIdAsync(createCategoryDto.ParentCategoryId.Value, cancellationToken);
            if (parentCategory == null)
            {
                throw new KeyNotFoundException($"ID: {createCategoryDto.ParentCategoryId.Value} olan üst kategori bulunamadı.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var category = new Categories
            {
                Title = createCategoryDto.Title,
                Slug = slug,
                Description = createCategoryDto.Description,
                ParentCategoryId = createCategoryDto.ParentCategoryId
            };

            await _unitOfWork.Categories.CreateAsync(category, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Audit log kaydet
            await _auditLogService.CreateLogAsync(new CreateAuditLogDto
            {
                Action = "CreateCategory",
                EntityType = "Category",
                EntityId = category.Id,
                NewValue = $"Title: {category.Title}, Slug: {category.Slug}",
                Success = true
            }, cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Title = category.Title,
                Slug = category.Slug,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                CreatedUserId = category.CreatedUserId,
                UpdatedUserId = category.UpdatedUserId,
                ThreadCount = 0,
                ParentCategoryId = category.ParentCategoryId,
                SubCategoryCount = 0
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

        if (category == null)
        {
            throw new KeyNotFoundException($"ID: {id} olan kategori bulunamadı.");
        }

        // Yeni slug oluştur
        var newSlug = GenerateSlug(updateCategoryDto.Title);

        // Farklı bir kategoride aynı slug var mı kontrol et
        var existingCategory = await _unitOfWork.Categories.FirstOrDefaultAsync(
            c => c.Slug.ToLower() == newSlug.ToLower() && c.Id != id, cancellationToken);

        if (existingCategory != null)
        {
            throw new InvalidOperationException($"'{updateCategoryDto.Title}' başlıklı kategori zaten mevcut.");
        }
        
        // ParentCategoryId güncelleniyorsa validation
        if (updateCategoryDto.ParentCategoryId.HasValue)
        {
            // Kendini parent yapamaz
            if (updateCategoryDto.ParentCategoryId.Value == id)
            {
                throw new InvalidOperationException("Bir kategori kendi üst kategorisi olamaz.");
            }
            
            // Parent category var mı kontrol et
            var parentCategory = await _unitOfWork.Categories.GetByIdAsync(updateCategoryDto.ParentCategoryId.Value, cancellationToken);
            if (parentCategory == null)
            {
                throw new KeyNotFoundException($"ID: {updateCategoryDto.ParentCategoryId.Value} olan üst kategori bulunamadı.");
            }
            
            // Circular reference kontrolü (parent'ın parent'ı bu kategori olamaz)
            if (await IsCircularReferenceAsync(id, updateCategoryDto.ParentCategoryId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Dairesel referans oluşturulamaz. Parent kategorinin parent'ı bu kategori olamaz.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            category.Title = updateCategoryDto.Title;
            category.Slug = newSlug;
            category.Description = updateCategoryDto.Description;
            category.ParentCategoryId = updateCategoryDto.ParentCategoryId;

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Title = category.Title,
                Slug = category.Slug,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                CreatedUserId = category.CreatedUserId,
                UpdatedUserId = category.UpdatedUserId,
                ThreadCount = category.Threads?.Count ?? 0,
                ParentCategoryId = category.ParentCategoryId,
                SubCategoryCount = category.SubCategories?.Count ?? 0
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id, bool force = false, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

        if (category == null)
        {
            return false;
        }

        // Kategoriye ait konu var mı kontrol et
        if (category.Threads?.Any() == true)
        {
            throw new InvalidOperationException(
                $"Bu kategori silinemez çünkü {category.Threads.Count} adet konu içermektedir.");
        }
        
        // Alt kategorisi var mı kontrol et
        if (category.SubCategories?.Any() == true)
        {
            if (!force)
            {
                throw new InvalidOperationException(
                    $"Bu kategori silinemez çünkü {category.SubCategories.Count} adet alt kategorisi vardır. " +
                    "Alt kategorileri de silmek için 'force=true' parametresi kullanın.");
            }
            
            // force=true ise alt kategorileri recursive sil
            await DeleteSubCategoriesRecursiveAsync(id, cancellationToken);
        }

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            Action = "DeleteCategory",
            EntityType = "Category",
            EntityId = id,
            OldValue = $"Title: {category.Title}, Slug: {category.Slug}",
            NewValue = "Deleted",
            Success = true
        }, cancellationToken);

        return true;
    }

    // Slug oluşturma helper metodu
    private static string GenerateSlug(string title)
    {
        // Türkçe karakterleri İngilizce karşılıklarına çevir
        var text = title.ToLower();
        
        // Türkçe karakterleri değiştir
        text = text.Replace("ı", "i")
                   .Replace("ğ", "g")
                   .Replace("ü", "u")
                   .Replace("ş", "s")
                   .Replace("ö", "o")
                   .Replace("ç", "c");

        // Diğer özel karakterleri kaldır
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
        
        // Birden fazla boşluğu tek boşluğa çevir
        text = Regex.Replace(text, @"\s+", " ").Trim();
        
        // Boşlukları tire ile değiştir
        text = Regex.Replace(text, @"\s", "-");

        return text;
    }
    
    // Yeni metodlar - Alt Kategori İşlemleri
    
    public async Task<List<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        // PERFORMANS: Tree için tüm kategorileri çekmek gerekli
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri
        var allCategoriesList = (await _unitOfWork.Categories.FindAsync(
            predicate: c => c.ClubId == null,
            cancellationToken: cancellationToken)).ToList();
        
        // SubCategory COUNT'ları RAM'de hesapla (hızlı)
        var subCategoryCounts = allCategoriesList
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Thread COUNT'ları için her kategori için ayrı query
        // TODO: Bu kısım raw SQL ile optimize edilebilir (SELECT CategoryId, COUNT(*) FROM Threads GROUP BY CategoryId)
        // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
        var threadCountsDict = new Dictionary<int, int>();
        foreach (var category in allCategoriesList)
        {
            var count = await _unitOfWork.Threads.CountAsync(
                t => t.CategoryId == category.Id && t.ClubId == null,
                cancellationToken);
            threadCountsDict[category.Id] = count;
        }
        
        var rootCategories = allCategoriesList
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Title)
            .ToList();

        // Recursive tree oluştur (COUNT dictionary'leri ile)
        return rootCategories.Select(c => BuildCategoryTree(c, allCategoriesList, threadCountsDict, subCategoryCounts)).ToList();
    }

    public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default)
    {
        // PERFORMANS OPTİMİZASYONU: Include kaldırıldı
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri
        var categories = (await _unitOfWork.Categories.FindAsync(
            predicate: c => c.ParentCategoryId == parentId && c.ClubId == null,
            cancellationToken: cancellationToken)).ToList();
        
        if (!categories.Any())
            return Enumerable.Empty<CategoryDto>();
        
        // Her kategori için ayrı COUNT query (genelde 10-20 alt kategori max)
        var result = new List<CategoryDto>();
        
        foreach (var c in categories)
        {
            // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
            var threadCount = await _unitOfWork.Threads.CountAsync(
                t => t.CategoryId == c.Id && t.ClubId == null,
                cancellationToken);
            
            var subCategoryCount = await _unitOfWork.Categories.CountAsync(
                cat => cat.ParentCategoryId == c.Id,
                cancellationToken);
            
            result.Add(new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = threadCount,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = subCategoryCount
            });
        }
        
        return result;
    }

    public async Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // PERFORMANS OPTİMİZASYONU: Include kaldırıldı
        // ÖNEMLI: ClubId == null filtresi - sadece normal forum kategorileri
        var categories = (await _unitOfWork.Categories.FindAsync(
            predicate: c => c.ParentCategoryId == null && c.ClubId == null,
            cancellationToken: cancellationToken)).ToList();
        
        if (!categories.Any())
            return Enumerable.Empty<CategoryDto>();
        
        // Her kategori için ayrı COUNT query (genelde 5-15 root kategori)
        var result = new List<CategoryDto>();
        
        foreach (var c in categories)
        {
            // ÖNEMLI: Sadece normal forum thread'lerini say (ClubId == null)
            var threadCount = await _unitOfWork.Threads.CountAsync(
                t => t.CategoryId == c.Id && t.ClubId == null,
                cancellationToken);
            
            var subCategoryCount = await _unitOfWork.Categories.CountAsync(
                cat => cat.ParentCategoryId == c.Id,
                cancellationToken);
            
            result.Add(new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = threadCount,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = subCategoryCount
            });
        }
        
        return result;
    }
    
    // Helper Methods
    
    private CategoryTreeDto BuildCategoryTree(
        Categories category, 
        List<Categories> allCategories,
        Dictionary<int, int> threadCountsDict,
        Dictionary<int, int> subCategoryCounts)
    {
        var treeDto = new CategoryTreeDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            ThreadCount = threadCountsDict.GetValueOrDefault(category.Id, 0),
            ParentCategoryId = category.ParentCategoryId,
            SubCategories = new List<CategoryTreeDto>()
        };

        // Alt kategorileri recursive olarak ekle
        var subCategories = allCategories.Where(c => c.ParentCategoryId == category.Id).ToList();
        foreach (var sub in subCategories)
        {
            treeDto.SubCategories.Add(BuildCategoryTree(sub, allCategories, threadCountsDict, subCategoryCounts));
        }

        return treeDto;
    }
    
    private async Task<bool> IsCircularReferenceAsync(int categoryId, int newParentId, CancellationToken cancellationToken)
    {
        var currentParentId = newParentId;
        var visitedIds = new HashSet<int> { categoryId };

        while (currentParentId > 0)
        {
            if (visitedIds.Contains(currentParentId))
            {
                return true; // Circular reference bulundu
            }

            visitedIds.Add(currentParentId);
            
            var parentCategory = await _unitOfWork.Categories.GetByIdAsync(currentParentId, cancellationToken);
            if (parentCategory?.ParentCategoryId == null)
            {
                break; // Root'a ulaşıldı
            }

            currentParentId = parentCategory.ParentCategoryId.Value;
        }

        return false;
    }
    
    private async Task DeleteSubCategoriesRecursiveAsync(int parentId, CancellationToken cancellationToken)
    {
        var children = (await _unitOfWork.Categories.FindWithIncludesAsync(
            predicate: c => c.ParentCategoryId == parentId,
            include: q => q.Include(c => c.Threads),
            cancellationToken: cancellationToken)).ToList();

        foreach (var child in children)
        {
            // Önce alt kategorinin alt kategorilerini sil (recursive)
            await DeleteSubCategoriesRecursiveAsync(child.Id, cancellationToken);
            
            // Thread kontrolü
            if (child.Threads?.Any() == true)
            {
                throw new InvalidOperationException(
                    $"Alt kategori '{child.Title}' silinemez çünkü {child.Threads.Count} adet konu içermektedir.");
            }
            
            // Alt kategoriyi sil
            _unitOfWork.Categories.Delete(child);
        }
    }
    
    // Kulüp Kategorileri Metodları
    
    /// <summary>
    /// Belirli bir kulübün kategorilerini getirir
    /// </summary>
    public async Task<IEnumerable<CategoryDto>> GetClubCategoriesAsync(int clubId, CancellationToken cancellationToken = default)
    {
        // ÖNEMLI: ClubId == clubId filtresi - sadece bu kulübün kategorileri
        var categories = (await _unitOfWork.Categories.FindAsync(
            predicate: c => c.ClubId == clubId,
            cancellationToken: cancellationToken)).ToList();
        
        if (!categories.Any())
            return Enumerable.Empty<CategoryDto>();
        
        // SubCategory COUNT'ları RAM'de hesapla
        var subCategoryCounts = categories
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Thread COUNT'ları için her kategori için ayrı query
        var result = new List<CategoryDto>();
        
        foreach (var c in categories.OrderBy(x => x.Title))
        {
            // Kulüp kategorilerinde ClubId filtresi de olmalı
            var threadCount = await _unitOfWork.Threads.CountAsync(
                t => t.CategoryId == c.Id && t.ClubId == clubId,
                cancellationToken);
            
            var subCategoryCount = subCategoryCounts.GetValueOrDefault(c.Id, 0);
            
            result.Add(new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = threadCount,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = subCategoryCount
            });
        }
        
        return result;
    }
    
    /// <summary>
    /// Kulüp için yeni kategori oluşturur
    /// </summary>
    public async Task<CategoryDto> CreateClubCategoryAsync(int clubId, CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default)
    {
        // Kulüp var mı kontrol et
        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
        {
            throw new KeyNotFoundException($"ID: {clubId} olan kulüp bulunamadı.");
        }
        
        // Slug oluştur
        var slug = GenerateSlug(createCategoryDto.Title);

        // Aynı kulüp içinde aynı slug var mı kontrol et
        var existingCategory = await _unitOfWork.Categories.FirstOrDefaultAsync(
            c => c.Slug.ToLower() == slug.ToLower() && c.ClubId == clubId, 
            cancellationToken);

        if (existingCategory != null)
        {
            throw new InvalidOperationException($"Bu kulüpte '{createCategoryDto.Title}' başlıklı kategori zaten mevcut.");
        }
        
        // ParentCategory kontrolü (eğer belirtilmişse, aynı kulüpte olmalı)
        if (createCategoryDto.ParentCategoryId.HasValue)
        {
            var parentCategory = await _unitOfWork.Categories.FirstOrDefaultAsync(
                c => c.Id == createCategoryDto.ParentCategoryId.Value && c.ClubId == clubId,
                cancellationToken);
            
            if (parentCategory == null)
            {
                throw new InvalidOperationException("Üst kategori bu kulübe ait değil veya bulunamadı.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var category = new Categories
            {
                Title = createCategoryDto.Title,
                Slug = slug,
                Description = createCategoryDto.Description,
                ParentCategoryId = createCategoryDto.ParentCategoryId,
                ClubId = clubId // ÖNEMLI: ClubId'yi set et
            };

            await _unitOfWork.Categories.CreateAsync(category, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Audit log kaydet
            await _auditLogService.CreateLogAsync(new CreateAuditLogDto
            {
                Action = "CreateClubCategory",
                EntityType = "Category",
                EntityId = category.Id,
                NewValue = $"ClubId: {clubId}, Title: {category.Title}, Slug: {category.Slug}",
                Success = true
            }, cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Title = category.Title,
                Slug = category.Slug,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                CreatedUserId = category.CreatedUserId,
                UpdatedUserId = category.UpdatedUserId,
                ThreadCount = 0,
                ParentCategoryId = category.ParentCategoryId,
                SubCategoryCount = 0
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
