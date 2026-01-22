using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Raporlama tablosu - Kullanıcılar uygunsuz içerik/davranış raporlar
/// </summary>
public class Reports : BaseEntity
{
    // KIM RAPOR ETTİ?
    public int ReporterId { get; set; } // Raporu yapan kullanıcı - Zorunlu
    
    // NEYİ RAPOR ETTİ? (En az biri dolu olmalı)
    public int? ReportedUserId { get; set; } // Rapor edilen kullanıcı - Opsiyonel
    public int? ReportedPostId { get; set; } // Rapor edilen post - Opsiyonel
    public int? ReportedThreadId { get; set; } // Rapor edilen thread - Opsiyonel
    
    // RAPOR BİLGİLERİ
    public ReportReason Reason { get; set; } // Rapor sebebi - Zorunlu (enum)
    public string? Description { get; set; } // Detay açıklama - Opsiyonel
    public ReportStatus Status { get; set; } = ReportStatus.Pending; // Durum - Varsayılan: Pending
    
    // ADMIN İNCELEME BİLGİLERİ
    public int? ReviewedByUserId { get; set; } // Hangi admin inceledi? - Opsiyonel
    public DateTime? ReviewedAt { get; set; } // Ne zaman incelendi? - Opsiyonel
    public string? AdminNote { get; set; } // Admin notu - Opsiyonel

    // NAVIGATION PROPERTIES
    /// <summary>
    /// Raporu yapan kullanıcı
    /// </summary>
    public Users Reporter { get; set; } = null!;
    
    /// <summary>
    /// Rapor edilen kullanıcı (opsiyonel)
    /// </summary>
    public Users? ReportedUser { get; set; }
    
    /// <summary>
    /// Rapor edilen post (opsiyonel)
    /// </summary>
    public Posts? ReportedPost { get; set; }
    
    /// <summary>
    /// Rapor edilen thread (opsiyonel)
    /// </summary>
    public Threads? ReportedThread { get; set; }
    
    /// <summary>
    /// İnceleyen admin (opsiyonel)
    /// </summary>
    public Users? ReviewedByUser { get; set; }
}   
