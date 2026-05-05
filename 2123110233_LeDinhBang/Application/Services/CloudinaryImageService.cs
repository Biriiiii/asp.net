using BookStore.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Alias để tránh nhầm lẫn giữa class của mình và class của Cloudinary SDK
using AppUploadDto = BookStore.Application.Interfaces.ImageUploadDto;
using CloudUploadResult = CloudinaryDotNet.Actions.ImageUploadResult;

namespace BookStore.Application.Services;

public class CloudinaryImageService : IImageUploadService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryImageService> _logger;
    private readonly string _cloudName;

    public CloudinaryImageService(
        IConfiguration config,
        ILogger<CloudinaryImageService> logger)
    {
        _logger = logger;
        _cloudName = config["Cloudinary:CloudName"]
            ?? throw new InvalidOperationException("Thiếu Cloudinary:CloudName trong appsettings.json");

        var apiKey = config["Cloudinary:ApiKey"]
            ?? throw new InvalidOperationException("Thiếu Cloudinary:ApiKey trong appsettings.json");
        var apiSecret = config["Cloudinary:ApiSecret"]
            ?? throw new InvalidOperationException("Thiếu Cloudinary:ApiSecret trong appsettings.json");

        var account = new Account(_cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    // ── Upload 1 ảnh ──────────────────────────────────────
    public async Task<AppUploadDto> UploadAsync(
        Stream fileStream, string fileName, string folder = "products")
    {
        try
        {
            ValidateFolder(folder);

            var publicId = $"bookstore/{folder}/{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}";

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = publicId,
                Overwrite = false,
                UniqueFilename = false,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            // Dùng alias CloudUploadResult để tránh ambiguous reference
            CloudUploadResult result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                return new AppUploadDto(false, null, null, null, result.Error.Message, 0, 0, 0);
            }

            var thumbnailUrl = BuildThumbnailUrl(result.PublicId);

            _logger.LogInformation(
                "Uploaded: {PublicId} ({Width}x{Height}, {Size} bytes)",
                result.PublicId, result.Width, result.Height, result.Bytes);

            return new AppUploadDto(
                true,
                result.SecureUrl.ToString(),
                result.PublicId,
                thumbnailUrl,
                null,
                result.Bytes,
                result.Width,
                result.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadAsync error: {FileName}", fileName);
            return new AppUploadDto(false, null, null, null, ex.Message, 0, 0, 0);
        }
    }

    // ── Upload nhiều ảnh ──────────────────────────────────
    public async Task<IEnumerable<AppUploadDto>> UploadManyAsync(
        IEnumerable<(Stream Stream, string FileName)> files,
        string folder = "products")
    {
        var tasks = files.Select(f => UploadAsync(f.Stream, f.FileName, folder));
        var results = await Task.WhenAll(tasks);
        return results;
    }

    // ── Xóa ảnh ──────────────────────────────────────────
    public async Task<bool> DeleteAsync(string publicId)
    {
        try
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            var ok = result.Result == "ok";
            if (!ok)
                _logger.LogWarning("Delete failed: {PublicId} → {Result}", publicId, result.Result);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync error: {PublicId}", publicId);
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────
    private string BuildThumbnailUrl(string publicId) =>
        $"https://res.cloudinary.com/{_cloudName}/image/upload/w_200,h_200,c_fill,g_auto/{publicId}";

    private static readonly string[] AllowedFolders =
        { "products", "categories", "authors", "banners", "reviews", "avatars" };

    private static void ValidateFolder(string folder)
    {
        if (!AllowedFolders.Contains(folder))
            throw new ArgumentException(
                $"Folder '{folder}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedFolders)}");
    }
}