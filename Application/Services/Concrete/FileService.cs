using Application.Services.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileService> _logger;
    
    // İzin verilen uzantılar
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    
    // Maksimum dosya boyutu (5 MB)
    private const long MaxFileSize = 5 * 1024 * 1024;

    public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Dosya kontrolü
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Dosya boş olamaz");
            }

            // 2. Boyut kontrolü
            if (!IsValidFileSize(file.Length))
            {
                throw new ArgumentException($"Dosya boyutu maksimum {MaxFileSize / 1024 / 1024} MB olabilir");
            }

            // 3. Uzantı kontrolü
            if (!IsValidImageExtension(file.FileName))
            {
                throw new ArgumentException($"Sadece {string.Join(", ", _allowedExtensions)} uzantılı dosyalar yüklenebilir");
            }

            // 4. Unique dosya adı oluştur
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // 5. Klasör yolu oluştur
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
            
            // Klasör yoksa oluştur
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 6. Dosya yolunu oluştur
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 7. Dosyayı kaydet
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // 8. URL döndür
            var fileUrl = $"/uploads/{folder}/{uniqueFileName}";
            
            _logger.LogInformation("Dosya yüklendi: {FileUrl}", fileUrl);
            
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yükleme hatası: {FileName}", file?.FileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return false;
            }

            // URL'den dosya yolunu oluştur
            // fileUrl = "/uploads/posts/abc123.jpg"
            var fileName = fileUrl.TrimStart('/');
            var filePath = Path.Combine(_environment.WebRootPath, fileName);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation("Dosya silindi: {FileUrl}", fileUrl);
                return true;
            }

            _logger.LogWarning("Silinecek dosya bulunamadı: {FileUrl}", fileUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya silme hatası: {FileUrl}", fileUrl);
            return false;
        }
    }

    public bool IsValidImageExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    public bool IsValidFileSize(long fileSize)
    {
        return fileSize > 0 && fileSize <= MaxFileSize;
    }
}
