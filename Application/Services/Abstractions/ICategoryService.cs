using Application.DTOs.Category;

namespace Application.Services.Abstractions;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
