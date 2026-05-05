namespace BookStore.Application.Interfaces;

/// <summary>Đổi tên thành ImageUploadDto để tránh trùng với CloudinaryDotNet.Actions.ImageUploadResult</summary>
public record ImageUploadDto(
    bool Success,
    string? Url,
    string? PublicId,
    string? ThumbnailUrl,
    string? ErrorMessage,
    long FileSizeBytes,
    int Width,
    int Height
);

public interface IImageUploadService
{
    Task<ImageUploadDto> UploadAsync(Stream fileStream, string fileName, string folder = "products");
    Task<IEnumerable<ImageUploadDto>> UploadManyAsync(
        IEnumerable<(Stream Stream, string FileName)> files,
        string folder = "products");
    Task<bool> DeleteAsync(string publicId);
}