using BookStore.Application.DTOs.Image;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/images")]
[Authorize]
[Produces("application/json")]
public class ImageUploadController : ControllerBase
{
    private readonly IImageUploadService _uploadService;

    public ImageUploadController(IImageUploadService uploadService)
        => _uploadService = uploadService;

    /// <summary>Upload 1 ảnh lên Cloudinary — trả về URL dùng cho products, categories...</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,ContentManager,Staff,Customer")]
    [ProducesResponseType(typeof(ImageUploadResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] string folder = "products")
    {
        var error = ValidateFile(file);
        if (error != null) return BadRequest(new { message = error });

        await using var stream = file.OpenReadStream();
        var result = await _uploadService.UploadAsync(stream, file.FileName, folder);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage ?? "Upload thất bại." });

        return Ok(MapToResponse(result));
    }

    /// <summary>Upload nhiều ảnh cùng lúc (tối đa 5)</summary>
    [HttpPost("upload-many")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(MultiUploadResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadMany(
        IList<IFormFile> files,
        [FromQuery] string folder = "products")
    {
        if (files == null || !files.Any())
            return BadRequest(new { message = "Không có file nào được gửi lên." });

        if (files.Count > ImageUploadConstants.MaxFilesPerRequest)
            return BadRequest(new { message = $"Tối đa {ImageUploadConstants.MaxFilesPerRequest} ảnh mỗi lần." });

        foreach (var f in files)
        {
            var err = ValidateFile(f);
            if (err != null) return BadRequest(new { message = $"File '{f.FileName}': {err}" });
        }

        var fileTuples = files.Select(f => (f.OpenReadStream() as Stream, f.FileName));
        var results = (await _uploadService.UploadManyAsync(fileTuples, folder)).ToList();

        return Ok(new MultiUploadResponseDto(
            TotalUploaded: results.Count(r => r.Success),
            TotalFailed: results.Count(r => !r.Success),
            Results: results.Select(MapToResponse)));
    }

    /// <summary>Xóa ảnh khỏi Cloudinary theo PublicId</summary>
    [HttpDelete]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Delete([FromQuery] string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return BadRequest(new { message = "publicId không được để trống." });

        var ok = await _uploadService.DeleteAsync(publicId);
        return ok
            ? Ok(new { message = "Đã xóa ảnh thành công." })
            : BadRequest(new { message = "Xóa ảnh thất bại. Kiểm tra lại publicId." });
    }

    // ── Helpers ───────────────────────────────────────────

    private static string? ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "File không được để trống.";
        if (file.Length > ImageUploadConstants.MaxFileSizeBytes)
            return $"File quá lớn. Tối đa {ImageUploadConstants.MaxFileSizeBytes / 1024 / 1024}MB.";
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!ImageUploadConstants.AllowedExtensions.Contains(ext))
            return $"Chỉ chấp nhận: {string.Join(", ", ImageUploadConstants.AllowedExtensions)}";
        if (!ImageUploadConstants.AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            return $"Content-Type không hợp lệ: {file.ContentType}";
        return null;
    }

    private static ImageUploadResponseDto MapToResponse(ImageUploadDto r) =>
        new(r.Success, r.Url, r.PublicId, r.ThumbnailUrl,
            r.ErrorMessage, r.FileSizeBytes, r.Width, r.Height);
}