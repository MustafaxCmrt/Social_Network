using Application.DTOs.Category;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.UnitOfWork;
using System.Text.RegularExpressions;

namespace Application.Services.Concrete;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query
                .Include(c => c.Threads)
                .Include(c => c.SubCategories),
            cancellationToken);
        
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            CreatedUserId = c.CreatedUserId,
            UpdatedUserId = c.UpdatedUserId,
            ThreadCount = c.Threads.Count,
            ParentCategoryId = c.ParentCategoryId,
            SubCategoryCount = c.SubCategories.Count
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query
                .Include(c => c.Threads)
                .Include(c => c.SubCategories),
            cancellationToken);

        var category = categories.FirstOrDefault(c => c.Id == id);

        if (category == null)
            return null;

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

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query
                .Include(c => c.Threads)
                .Include(c => c.SubCategories),
            cancellationToken);

        var category = categories.FirstOrDefault(c => c.Slug == slug);

        if (category == null)
            return null;

        return new CategoryDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,            CreatedUserId = category.CreatedUserId,
            UpdatedUserId = category.UpdatedUserId,            ThreadCount = category.Threads?.Count ?? 0,
            ParentCategoryId = category.ParentCategoryId,
            SubCategoryCount = category.SubCategories?.Count ?? 0
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default)
    {
        // Slug oluştur
        var slug = GenerateSlug(createCategoryDto.Title);

        // Aynı slug var mı kontrol et
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var existingCategory = categories.FirstOrDefault(c =>
            c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

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
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var existingCategory = categories.FirstOrDefault(c =>
            c.Slug.Equals(newSlug, StringComparison.OrdinalIgnoreCase) && c.Id != id);

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
        var allCategories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query
                .Include(c => c.Threads)
                .Include(c => c.SubCategories),
            cancellationToken);

        // Root kategorileri al (ParentCategoryId == null)
        var rootCategories = allCategories.Where(c => c.ParentCategoryId == null).ToList();

        // Recursive tree oluştur
        return rootCategories.Select(c => BuildCategoryTree(c, allCategories.ToList())).ToList();
    }

    public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query
                .Include(c => c.Threads)
                .Include(c => c.SubCategories),
            cancellationToken);

        return categories
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = c.Threads.Count,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = c.SubCategories.Count
            });
    }

    public async Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query
                .Include(c => c.Threads)
                .Include(c => c.SubCategories),
            cancellationToken);

        return categories
            .Where(c => c.ParentCategoryId == null)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedUserId = c.CreatedUserId,
                UpdatedUserId = c.UpdatedUserId,
                ThreadCount = c.Threads.Count,
                ParentCategoryId = c.ParentCategoryId,
                SubCategoryCount = c.SubCategories.Count
            });
    }
    
    // Helper Methods
    
    private CategoryTreeDto BuildCategoryTree(Categories category, List<Categories> allCategories)
    {
        var treeDto = new CategoryTreeDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            ThreadCount = category.Threads?.Count ?? 0,
            ParentCategoryId = category.ParentCategoryId,
            SubCategories = new List<CategoryTreeDto>()
        };

        // Alt kategorileri recursive olarak ekle
        var subCategories = allCategories.Where(c => c.ParentCategoryId == category.Id).ToList();
        foreach (var sub in subCategories)
        {
            treeDto.SubCategories.Add(BuildCategoryTree(sub, allCategories));
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
        var subCategories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var children = subCategories.Where(c => c.ParentCategoryId == parentId).ToList();

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
}
