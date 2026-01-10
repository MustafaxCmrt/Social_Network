using Application.DTOs.Category;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

public class CategoryController : AppController
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    // GET: api/Category
    [HttpGet("getAll")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    // GET: api/Category/5
    [HttpGet("get/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
        
        if (category == null)
        {
            return NotFound(new { message = $"ID: {id} olan kategori bulunamadı." });
        }

        return Ok(category);
    }

    // GET: api/Category/slug/csharp
    [HttpGet("get/slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryBySlugAsync(slug, cancellationToken);
        
        if (category == null)
        {
            return NotFound(new { message = $"Slug: {slug} olan kategori bulunamadı." });
        }

        return Ok(category);
    }

    // POST: api/Category
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryDto createCategoryDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(createCategoryDto, cancellationToken);
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.Id },
                category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/Category/5
    [HttpPut("update")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(
        [FromBody] UpdateCategoryDto updateCategoryDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(updateCategoryDto.Id, updateCategoryDto, cancellationToken);
            return Ok(category);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/Category/5?force=true
    [HttpDelete("delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken, [FromQuery] bool force = false)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id, force, cancellationToken);
            
            if (!result)
            {
                return NotFound(new { message = $"ID: {id} olan kategori bulunamadı." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    // GET: api/Category/tree
    /// <summary>
    /// Tüm kategorileri hiyerarşik tree yapısında getirir
    /// </summary>
    [HttpGet("tree")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryTree(CancellationToken cancellationToken)
    {
        var tree = await _categoryService.GetCategoryTreeAsync(cancellationToken);
        return Ok(tree);
    }
    
    // GET: api/Category/{id}/subcategories
    /// <summary>
    /// Belirtilen kategorinin alt kategorilerini getirir
    /// </summary>
    [HttpGet("{id}/subcategories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubCategories(int id, CancellationToken cancellationToken)
    {
        var subCategories = await _categoryService.GetSubCategoriesAsync(id, cancellationToken);
        return Ok(subCategories);
    }
    
    // GET: api/Category/root
    /// <summary>
    /// Sadece ana kategorileri getirir (üst kategorisi olmayan)
    /// </summary>
    [HttpGet("root")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRootCategories(CancellationToken cancellationToken)
    {
        var rootCategories = await _categoryService.GetRootCategoriesAsync(cancellationToken);
        return Ok(rootCategories);
    }
}
