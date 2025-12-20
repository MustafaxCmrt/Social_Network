using Domain.Common;

namespace Domain.Entities;

public class Categories : BaseEntity
{
    public string Title { get; set; } = null!; // Zorunlu - kategorinin başlığı olmalı
    public string Slug { get; set; } = null!; // Zorunlu - URL için slug olmalı
    public string? Description { get; set; } // Opsiyonel - açıklama olmayabilir

    public ICollection<Threads> Threads { get; set; } = new List<Threads>();
}