using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _2123110233_LeDinhBang.Data;
using _2123110233_LeDinhBang.Models;

namespace _2123110233_LeDinhBang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Tiêm DbContext vào Controller (Dependency Injection)
        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Products
        // Lấy danh sách tất cả sản phẩm đang mở bán
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            // Trả về danh sách và chọn lọc các trường cần thiết để tránh lỗi vòng lặp JSON vô tận
            var products = await _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.CurrentPrice,
                    p.OriginalPrice,
                    CategoryName = p.Category.Name,
                    // Lấy ảnh bìa chính (nếu có)
                    PrimaryImage = p.ProductImages.FirstOrDefault(i => i.IsPrimary).ImageUrl
                })
                .ToListAsync();

            return Ok(products);
        }

        // 2. GET: api/Products/5
        // Xem chi tiết 1 sản phẩm kèm theo toàn bộ thông tin liên quan
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Publisher)
                .Include(p => p.Inventory)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAuthors)
                    .ThenInclude(pa => pa.Author) // Lấy thông tin Tác giả thông qua bảng trung gian
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm!" });
            }

            // Map ra một object an toàn để trả về FE
            var result = new
            {
                product.Id,
                product.Name,
                product.Slug,
                product.Description,
                product.Isbn,
                product.OriginalPrice,
                product.CurrentPrice,
                Category = product.Category.Name,
                Publisher = product.Publisher.Name,
                Stock = product.Inventory?.StockQuantity ?? 0,
                Authors = product.ProductAuthors.Select(pa => pa.Author.Name).ToList(),
                Images = product.ProductImages.Select(i => new { i.ImageUrl, i.IsPrimary }).ToList()
            };

            return Ok(result);
        }

        // 3. POST: api/Products
        // Thêm mới một sản phẩm (kèm theo việc khởi tạo bảng Inventory)
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            // Tự động tạo bản ghi Tồn kho (Inventory) bằng 0 khi vừa thêm sách mới
            product.Inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                StockQuantity = 0,
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // 4. PUT: api/Products/5
        // Cập nhật thông tin sản phẩm
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(Guid id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest(new { message = "ID sản phẩm không khớp!" });
            }

            product.UpdatedAt = DateTime.UtcNow;
            _context.Entry(product).State = EntityState.Modified;

            // Không cho phép update các trường này qua API chung
            _context.Entry(product).Property(p => p.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // 5. DELETE: api/Products/5
        // Xóa mềm (Soft Delete): Thay vì xóa hẳn khỏi DB, ta chỉ ẩn nó đi
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Xóa mềm: Chuyển trạng thái IsActive thành false thay vì dùng _context.Products.Remove()
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Sản phẩm đã được ngưng bán thành công!" });
        }

        private bool ProductExists(Guid id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}