namespace Application.DTOs.Dashboard;

/// <summary>
/// En aktif kullanıcı bilgisi
/// </summary>
public record TopUserDto
{
    /// <summary>
    /// Kullanıcı ID
    /// </summary>
    public int UserId { get; init; }

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; init; } = null!;

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; init; } = null!;

    /// <summary>
    /// Thread sayısı
    /// </summary>
    public int ThreadCount { get; init; }

    /// <summary>
    /// Post sayısı
    /// </summary>
    public int PostCount { get; init; }

    /// <summary>
    /// Toplam aktivite (thread + post)
    /// </summary>
    public int TotalActivity { get; init; }
}
