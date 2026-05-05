namespace BookStore.Application.DTOs.Image;

// ── Responses ─────────────────────────────────────────────
public record ImageUploadResponseDto(
    bool Success,
    string? Url,
    string? PublicId,
    string? ThumbnailUrl,
    string? ErrorMessage,
    long FileSizeBytes,
    int Width,
    int Height
);

public record MultiUploadResponseDto(
    int TotalUploaded,
    int TotalFailed,
    IEnumerable<ImageUploadResponseDto> Results
);

// ── Validation constants ───────────────────────────────────
public static class ImageUploadConstants
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;  // 5MB
    public const int MaxFilesPerRequest = 5;

    public static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public static readonly string[] AllowedMimeTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };
}