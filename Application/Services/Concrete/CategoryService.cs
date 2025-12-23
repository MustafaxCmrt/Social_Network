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
            include: query => query.Include(c => c.Threads),
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
            ThreadCount = c.Threads.Count
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query.Include(c => c.Threads),
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
            ThreadCount = category.Threads?.Count ?? 0
        };
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync(
            include: query => query.Include(c => c.Threads),
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
            UpdatedUserId = category.UpdatedUserId,            ThreadCount = category.Threads?.Count ?? 0
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

        var category = new Categories
        {
            Title = createCategoryDto.Title,
            Slug = slug,
            Description = createCategoryDto.Description
        };

        await _unitOfWork.Categories.CreateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            ThreadCount = 0
        };
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

        category.Title = updateCategoryDto.Title;
        category.Slug = newSlug;
        category.Description = updateCategoryDto.Description;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            ThreadCount = category.Threads?.Count ?? 0
        };
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
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
}
