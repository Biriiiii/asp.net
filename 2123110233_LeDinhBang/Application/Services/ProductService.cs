using BookStore.Application.DTOs.Product;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Xml.Linq;

namespace BookStore.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IPublisherRepository _publishers;
    private readonly IAuthorRepository _authors;
    private readonly AppDbContext _db;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IPublisherRepository publishers,
        IAuthorRepository authors,
        AppDbContext db)
    {
        _products = products;
        _categories = categories;
        _publishers = publishers;
        _authors = authors;
        _db = db;
    }

    public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams query)
    {
        var filter = new ProductFilter
        {
            Keyword    = query.Keyword,
            CategoryId = query.CategoryId,
            AuthorId   = query.AuthorId,
            PublisherId= query.PublisherId,
            MinPrice   = query.MinPrice,
            MaxPrice   = query.MaxPrice,
            Language   = query.Language,
            IsActive   = query.IsActive,
            IsFeatured = query.IsFeatured,
            InStockOnly= query.InStockOnly,
            SortBy     = query.SortBy,
            Page       = Math.Max(1, query.Page),
            PageSize   = Math.Clamp(query.PageSize, 1, 100)
        };

        var (items, total) = await _products.GetPagedAsync(filter);
        var dtos = items.Select(MapToListItem);
        return new PagedResult<ProductListItemDto>(dtos, total, filter.Page, filter.PageSize);
    }

    public async Task<ProductDetailDto> GetByIdAsync(Guid id)
    {
        var product = await _products.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm với Id: {id}");
        return MapToDetail(product);
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug)
    {
        var product = await _products.GetBySlugAsync(slug)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm với slug: {slug}");
        return MapToDetail(product);
    }

    public async Task<ProductDetailDto> CreateAsync(CreateProductRequest req)
    {
        // Validate slug unique
        if (await _products.SlugExistsAsync(req.Slug))
            throw new InvalidOperationException($"Slug '{req.Slug}' đã tồn tại.");

        // Validate foreign keys
        var category  = await _categories.GetByIdAsync(req.CategoryId)
            ?? throw new KeyNotFoundException("Danh mục không tồn tại.");
        var publisher = await _publishers.GetByIdAsync(req.PublisherId)
            ?? throw new KeyNotFoundException("Nhà xuất bản không tồn tại.");

        var product = new Product
        {
            CategoryId    = req.CategoryId,
            PublisherId   = req.PublisherId,
            Title         = req.Title.Trim(),
            Slug          = req.Slug.Trim().ToLower(),
            Isbn          = req.Isbn?.Trim(),
            PageCount     = req.PageCount,
            WeightGram    = req.WeightGram,
            Language      = req.Language,
            CoverType     = req.CoverType,
            OriginalPrice = req.OriginalPrice,
            SalePrice     = req.SalePrice,
            Description   = req.Description,
            IsActive      = req.IsActive,
            IsFeatured    = req.IsFeatured,
            PublishedDate = req.PublishedDate
        };

        // Thêm authors
        foreach (var a in req.Authors)
        {
            var author = await _authors.GetByIdAsync(a.AuthorId)
                ?? throw new KeyNotFoundException($"Tác giả Id={a.AuthorId} không tồn tại.");
            product.ProductAuthors.Add(new ProductAuthor
            {
                AuthorId = a.AuthorId,
                Role     = a.Role
            });
        }

        // Thêm images
        foreach (var img in req.Images)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl     = img.ImageUrl,
                AltText      = img.AltText,
                IsPrimary    = img.IsPrimary,
                DisplayOrder = img.DisplayOrder
            });
        }

        // Tạo inventory mặc định
        product.Inventory = new Inventory { ProductId = product.Id };

        await _products.AddAsync(product);
        await _products.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductRequest req)
    {
        // Dùng GetDetailAsync — KHÔNG AsNoTracking → EF tự track entity
        var product = await _products.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id: {id}");

        // Validate slug unique (bỏ qua chính nó)
        if (await _products.SlugExistsAsync(req.Slug, id))
            throw new InvalidOperationException($"Slug '{req.Slug}' đã tồn tại.");

        _ = await _categories.GetByIdAsync(req.CategoryId)
            ?? throw new KeyNotFoundException("Danh mục không tồn tại.");
        _ = await _publishers.GetByIdAsync(req.PublisherId)
            ?? throw new KeyNotFoundException("Nhà xuất bản không tồn tại.");

        // Cập nhật các field thông thường
        product.CategoryId    = req.CategoryId;
        product.PublisherId   = req.PublisherId;
        product.Title         = req.Title.Trim();
        product.Slug          = req.Slug.Trim().ToLower();
        product.Isbn          = req.Isbn?.Trim();
        product.PageCount     = req.PageCount;
        product.WeightGram    = req.WeightGram;
        product.Language      = req.Language;
        product.CoverType     = req.CoverType;
        product.OriginalPrice = req.OriginalPrice;
        product.SalePrice     = req.SalePrice;
        product.Description   = req.Description;
        product.IsActive      = req.IsActive;
        product.IsFeatured    = req.IsFeatured;
        product.PublishedDate = req.PublishedDate;
        product.UpdatedAt     = DateTime.UtcNow;

        // ── Cập nhật Authors (Đồng bộ hóa để tránh lỗi Concurrency) ──
        var newAuthorIds = req.Authors.Select(a => a.AuthorId).ToList();

        // 1. Xóa các tác giả không còn trong danh sách mới (Xóa khỏi collection)
        var authorsToRemove = product.ProductAuthors.Where(pa => !newAuthorIds.Contains(pa.AuthorId)).ToList();
        foreach (var pa in authorsToRemove)
        {
            product.ProductAuthors.Remove(pa);
        }

        // 2. Cập nhật hoặc Thêm mới
        foreach (var a in req.Authors)
        {
            var existingAuthor = product.ProductAuthors.FirstOrDefault(pa => pa.AuthorId == a.AuthorId);
            if (existingAuthor != null)
            {
                // Cập nhật
                existingAuthor.Role = a.Role;
            }
            else
            {
                // Thêm mới
                _ = await _authors.GetByIdAsync(a.AuthorId)
                    ?? throw new KeyNotFoundException($"Tác giả Id={a.AuthorId} không tồn tại.");

                product.ProductAuthors.Add(new ProductAuthor
                {
                    ProductId = product.Id,
                    AuthorId  = a.AuthorId,
                    Role      = a.Role
                });
            }
        }

        // ── Cập nhật Images (Cách chuẩn xác nhất để tránh lỗi State) ──
        // 1. Đánh dấu xóa các ảnh cũ. Tuyệt đối KHÔNG dùng .Clear() ở đây
        // vì Clear() sẽ làm thay đổi Foreign Key (ProductId) của các ảnh cũ thành null, 
        // khiến EF Core hiểu lầm là đang "Sửa" (Modified) thay vì "Xóa" (Deleted).
        if (product.Images.Any())
        {
            _db.Set<ProductImage>().RemoveRange(product.Images);
        }

        // 2. Thêm ảnh mới vào danh sách. Collection sẽ chứa cả ảnh đã đánh dấu xóa và ảnh mới,
        // EF Core sẽ tự động phân loại Deleted và Added khi SaveChanges.
        if (req.Images != null && req.Images.Any())
        {
            foreach (var img in req.Images)
            {
                var newImg = new ProductImage
                {
                    ProductId    = product.Id,
                    ImageUrl     = img.ImageUrl,
                    AltText      = img.AltText,
                    IsPrimary    = img.IsPrimary,
                    DisplayOrder = img.DisplayOrder
                };
                
                product.Images.Add(newImg);
                
                // [CỰC KỲ QUAN TRỌNG]
                // Vì ProductImage kế thừa BaseEntity (có sẵn Id = Guid.NewGuid()),
                // EF Core thấy Id != rỗng nên sẽ đoán nhầm đây là thực thể đã tồn tại và set state = Modified.
                // Ta phải ÉP CỨNG state = Added để EF Core phát sinh lệnh INSERT thay vì UPDATE.
                _db.Entry(newImg).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
        }

        // Bắt buộc gọi Update() để Repository đánh dấu State = Modified (theo cấu trúc Repo hiện tại)
        _products.Update(product);

        try
        {
            await _products.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault();
            var entityName = entry?.Entity.GetType().Name ?? "Unknown";
            throw new Exception($"Lỗi CSDL trên thực thể: {entityName}. Trạng thái: {entry?.State}. Chi tiết: {ex.Message}");
        }

        return await GetByIdAsync(product.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _products.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id: {id}");
        // Soft delete
        product.IsActive  = false;
        product.UpdatedAt = DateTime.UtcNow;
        _products.Update(product);
        await _products.SaveChangesAsync();
    }

    public async Task<InventoryDto> UpdateInventoryAsync(Guid productId, UpdateInventoryRequest req)
    {
        var product = await _products.GetDetailAsync(productId)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id: {productId}");

        var inv = product.Inventory ?? new Inventory { ProductId = productId };
        inv.QtyAvailable      = req.QtyAvailable;
        inv.MinThreshold      = req.MinThreshold;
        inv.WarehouseLocation = req.WarehouseLocation;
        inv.UpdatedAt         = DateTime.UtcNow;

        if (product.Inventory == null)
        {
            product.Inventory = inv;
            await _products.SaveChangesAsync();
        }
        else
        {
            _products.Update(product);
            await _products.SaveChangesAsync();
        }

        return MapInventory(inv);
    }

    public async Task<byte[]> ExportExcelAsync(ProductQueryParams query)
    {
        query.Page = 1;
        query.PageSize = 100;
        var page = await GetPagedAsync(query);
        var rows = new List<IReadOnlyList<string>>
        {
            ProductExcelHeaders
        };

        foreach (var item in page.Items)
        {
            var detail = await GetByIdAsync(item.Id);
            rows.Add(new[]
            {
                detail.Id.ToString(),
                detail.Title,
                detail.Slug,
                detail.Isbn ?? "",
                detail.Category.Id.ToString(),
                detail.Publisher.Id.ToString(),
                detail.PageCount.ToString(CultureInfo.InvariantCulture),
                detail.WeightGram.ToString(CultureInfo.InvariantCulture),
                detail.Language,
                detail.CoverType,
                detail.OriginalPrice.ToString(CultureInfo.InvariantCulture),
                detail.SalePrice.ToString(CultureInfo.InvariantCulture),
                detail.Description ?? "",
                detail.IsActive.ToString(),
                detail.IsFeatured.ToString(),
                detail.PublishedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                detail.Inventory?.QtyAvailable.ToString(CultureInfo.InvariantCulture) ?? "0",
                detail.Inventory?.MinThreshold.ToString(CultureInfo.InvariantCulture) ?? "5",
                detail.Inventory?.WarehouseLocation ?? ""
            });
        }

        return CreateXlsx(rows);
    }

    public async Task<ProductExcelImportResultDto> ImportExcelAsync(Stream fileStream)
    {
        var rows = ReadXlsx(fileStream);
        if (rows.Count <= 1)
        {
            return new ProductExcelImportResultDto(0, 0, 0, 0, new[] { "File Excel không có dữ liệu." });
        }

        var headers = rows[0]
            .Select((name, index) => new { name = name.Trim(), index })
            .Where(x => !string.IsNullOrWhiteSpace(x.name))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        for (var i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            try
            {
                var idText = GetCell(row, headers, "Id");
                var slug = RequiredCell(row, headers, "Slug", rowNumber).Trim().ToLowerInvariant();
                var existing = Guid.TryParse(idText, out var id)
                    ? await _products.GetDetailAsync(id)
                    : await _products.GetBySlugAsync(slug);

                var request = new UpdateProductRequest
                {
                    Title = RequiredCell(row, headers, "Title", rowNumber),
                    Slug = slug,
                    Isbn = EmptyToNull(GetCell(row, headers, "Isbn")),
                    CategoryId = ParseGuid(RequiredCell(row, headers, "CategoryId", rowNumber), "CategoryId", rowNumber),
                    PublisherId = ParseGuid(RequiredCell(row, headers, "PublisherId", rowNumber), "PublisherId", rowNumber),
                    PageCount = ParseInt(GetCell(row, headers, "PageCount"), 1),
                    WeightGram = ParseInt(GetCell(row, headers, "WeightGram"), 300),
                    Language = EmptyToNull(GetCell(row, headers, "Language")) ?? "vi",
                    CoverType = ParseEnum(GetCell(row, headers, "CoverType"), Domain.Enums.CoverType.Paperback),
                    OriginalPrice = ParseDecimal(GetCell(row, headers, "OriginalPrice")),
                    SalePrice = ParseDecimal(GetCell(row, headers, "SalePrice")),
                    Description = EmptyToNull(GetCell(row, headers, "Description")),
                    IsActive = ParseBool(GetCell(row, headers, "IsActive"), true),
                    IsFeatured = ParseBool(GetCell(row, headers, "IsFeatured"), false),
                    PublishedDate = ParseDate(GetCell(row, headers, "PublishedDate")),
                    Authors = ParseAuthorRequests(GetCell(row, headers, "AuthorIds"), GetCell(row, headers, "AuthorId")),
                    Images = ParseImageRequests(GetCell(row, headers, "ImageUrls"))
                };

                request.Authors = await FilterExistingAuthorsAsync(request.Authors);

                if (existing == null)
                {
                    await CreateAsync(request);
                    created++;
                }
                else
                {
                    await UpdateAsync(existing.Id, request);
                    updated++;
                }

                var targetId = existing?.Id;
                if (!targetId.HasValue)
                {
                    targetId = (await _products.GetBySlugAsync(slug))?.Id;
                }

                if (targetId.HasValue)
                {
                    await UpdateInventoryAsync(targetId.Value, new UpdateInventoryRequest
                    {
                        QtyAvailable = ParseInt(GetCell(row, headers, "QtyAvailable"), 0),
                        MinThreshold = ParseInt(GetCell(row, headers, "MinThreshold"), 5),
                        WarehouseLocation = EmptyToNull(GetCell(row, headers, "WarehouseLocation"))
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Dòng {rowNumber}: {ex.Message}");
            }
        }

        var total = rows.Skip(1).Count(r => r.Any(c => !string.IsNullOrWhiteSpace(c)));
        return new ProductExcelImportResultDto(total, created, updated, errors.Count, errors);
    }

    // ── Mappers ──────────────────────────────────────────

    private static ProductListItemDto MapToListItem(Product p)
    {
        var discount = p.OriginalPrice > 0
            ? (int)Math.Round((p.OriginalPrice - p.SalePrice) / p.OriginalPrice * 100)
            : 0;
        return new ProductListItemDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Isbn,
            p.OriginalPrice,
            p.SalePrice,
            discount,
            p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl,
            p.Category?.Name ?? "",
            p.Publisher?.Name, // Thêm tên NXB vào đây
            p.ProductAuthors.Select(pa => pa.Author?.Name ?? ""),
            p.Inventory?.QtyActual > 0,
            p.Inventory?.QtyAvailable ?? 0,
            p.IsFeatured,
            p.IsActive,
            p.CreatedAt
        );
    }

    private static ProductDetailDto MapToDetail(Product p)
    {
        var discount = p.OriginalPrice > 0
            ? (int)Math.Round((p.OriginalPrice - p.SalePrice) / p.OriginalPrice * 100)
            : 0;

        return new ProductDetailDto(
            p.Id, p.Title, p.Slug, p.Isbn,
            p.PageCount, p.WeightGram, p.Language, p.CoverType.ToString(),
            p.OriginalPrice, p.SalePrice, discount,
            p.Description, p.IsActive, p.IsFeatured, p.PublishedDate,
            p.Category != null 
                ? new CategorySummaryDto(p.Category.Id, p.Category.Name, p.Category.Slug)
                : new CategorySummaryDto(Guid.Empty, "N/A", ""),
            p.Publisher != null
                ? new PublisherSummaryDto(p.Publisher.Id, p.Publisher.Name, p.Publisher.Country)
                : new PublisherSummaryDto(Guid.Empty, "N/A", ""),
            p.ProductAuthors.Select(pa => new ProductAuthorDto(pa.AuthorId, pa.Author?.Name ?? "N/A", pa.Role, pa.Author?.AvatarUrl)),
            p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder)),
            p.Inventory == null ? null : MapInventory(p.Inventory),
            p.CreatedAt, p.UpdatedAt
        );
    }

    private static InventoryDto MapInventory(Inventory inv) =>
        new(inv.QtyAvailable, inv.QtyReserved, inv.QtyActual, inv.MinThreshold, inv.IsLowStock, inv.IsOutOfStock, inv.WarehouseLocation);

    private static readonly string[] ProductExcelHeaders =
    {
        "Id", "Title", "Slug", "Isbn", "CategoryId", "PublisherId", "PageCount", "WeightGram",
        "Language", "CoverType", "OriginalPrice", "SalePrice", "Description", "IsActive",
        "IsFeatured", "PublishedDate", "QtyAvailable", "MinThreshold", "WarehouseLocation",
        "ImageUrls", "AuthorIds"
    };

    private static byte[] CreateXlsx(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            AddZipEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            AddZipEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Products" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            AddZipEntry(archive, "xl/worksheets/sheet1.xml", BuildSheetXml(rows));
        }

        return ms.ToArray();
    }

    private static string BuildSheetXml(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var sheetData = string.Join("", rows.Select((row, rowIndex) =>
        {
            var rowNumber = rowIndex + 1;
            var cells = string.Join("", row.Select((value, colIndex) =>
                $"<c r=\"{GetColumnName(colIndex + 1)}{rowNumber}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(value ?? "")}</t></is></c>"));
            return $"<row r=\"{rowNumber}\">{cells}</row>";
        }));

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>{sheetData}</sheetData>
            </worksheet>
            """;
    }

    private static List<List<string>> ReadXlsx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("Không tìm thấy sheet đầu tiên trong file Excel.");

        using var sheetStream = sheet.Open();
        var doc = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var result = new List<List<string>>();

        foreach (var row in doc.Descendants(ns + "row"))
        {
            var values = new List<string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = cell.Attribute("r")?.Value ?? "";
                var columnIndex = GetColumnIndex(reference);
                while (values.Count < columnIndex - 1)
                {
                    values.Add("");
                }

                values.Add(ReadCellValue(cell, ns, sharedStrings));
            }

            result.Add(values);
        }

        return result;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return new List<string>();
        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "si").Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value))).ToList();
    }

    private static string ReadCellValue(XElement cell, XNamespace ns, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
        {
            return cell.Descendants(ns + "t").FirstOrDefault()?.Value ?? "";
        }

        var raw = cell.Element(ns + "v")?.Value ?? "";
        if (type == "s" && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        return raw;
    }

    private static void AddZipEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static string GetColumnName(int index)
    {
        var name = "";
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static int GetColumnIndex(string reference)
    {
        var letters = new string(reference.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
        var index = 0;
        foreach (var c in letters)
        {
            index = index * 26 + c - 'A' + 1;
        }
        return Math.Max(index, 1);
    }

    private static string GetCell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var index) && index < row.Count ? row[index].Trim() : "";

    private static string RequiredCell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headers, string name, int rowNumber)
    {
        var value = GetCell(row, headers, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Thiếu cột hoặc giá trị {name}.");
        }
        return value;
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static Guid ParseGuid(string value, string field, int rowNumber) =>
        Guid.TryParse(value, out var result) ? result : throw new InvalidOperationException($"{field} không đúng định dạng Guid.");
    private static int ParseInt(string value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
    private static decimal ParseDecimal(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;
    private static bool ParseBool(string value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (bool.TryParse(value, out var result)) return result;
        return value.Trim() switch
        {
            "1" => true,
            "0" => false,
            "yes" => true,
            "no" => false,
            "YES" => true,
            "NO" => false,
            _ => fallback
        };
    }
    private static DateTime? ParseDate(string value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;
    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var result) ? result : fallback;

    private static List<ProductImageRequest> ParseImageRequests(string imageUrls)
    {
        if (string.IsNullOrWhiteSpace(imageUrls))
        {
            return new List<ProductImageRequest>();
        }

        return imageUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select((url, index) => new ProductImageRequest(
                url,
                index == 0 ? "Ảnh chính sản phẩm" : $"Ảnh sản phẩm {index + 1}",
                index == 0,
                index))
            .ToList();
    }

    private static List<ProductAuthorRequest> ParseAuthorRequests(string authorIds, string authorId)
    {
        var raw = string.IsNullOrWhiteSpace(authorIds) ? authorId : authorIds;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<ProductAuthorRequest>();
        }

        return raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => Guid.TryParse(value, out _))
            .Select(value => new ProductAuthorRequest(Guid.Parse(value), "Author"))
            .ToList();
    }

    private async Task<List<ProductAuthorRequest>> FilterExistingAuthorsAsync(IEnumerable<ProductAuthorRequest> authors)
    {
        var result = new List<ProductAuthorRequest>();
        foreach (var author in authors)
        {
            if (await _authors.GetByIdAsync(author.AuthorId) != null)
            {
                result.Add(author);
            }
        }

        return result;
    }
}
