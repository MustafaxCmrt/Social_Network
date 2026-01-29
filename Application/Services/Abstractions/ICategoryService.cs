using Application.DTOs.Category;
using Application.DTOs.Common;

namespace Application.Services.Abstractions;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PagedResultDto<CategoryDto>> GetAllCategoriesPaginatedAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        int? parentCategoryId = null,
        CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, bool force = false, CancellationToken cancellationToken = default);
    
    // Alt Kategori Metodları
    Task<List<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);
}
