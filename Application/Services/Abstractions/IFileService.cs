using Microsoft.AspNetCore.Http;

namespace Application.Services.Abstractions;

/// <summary>
/// Dosya yükleme ve yönetimi servisi
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Resim dosyası yükler ve URL'ini döner
    /// </summary>
    /// <param name="file">Yüklenecek dosya</param>
    /// <param name="folder">Hedef klasör (posts, profiles)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yüklenen dosyanın URL'i (örn: /uploads/posts/abc123.jpg)</returns>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyayı siler
    /// </summary>
    /// <param name="fileUrl">Silinecek dosyanın URL'i</param>
    /// <returns>Silme başarılı ise true</returns>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Dosya uzantısının geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="fileName">Dosya adı</param>
    /// <returns>Geçerli ise true</returns>
    bool IsValidImageExtension(string fileName);

    /// <summary>
    /// Dosya boyutunun geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="fileSize">Dosya boyutu (byte)</param>
    /// <returns>Geçerli ise true</returns>
    bool IsValidFileSize(long fileSize);
}
