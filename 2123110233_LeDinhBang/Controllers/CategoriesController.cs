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
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        // Lấy danh sách danh mục (Chỉ lấy các danh mục gốc - ParentId là null, kèm theo danh mục con)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive && c.ParentId == null) // Chỉ lấy Root
                .Include(c => c.Subcategories) // Nối thêm các danh mục con
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Slug,
                    Subcategories = c.Subcategories.Select(sub => new
                    {
                        sub.Id,
                        sub.Name,
                        sub.Slug
                    }).ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            category.Id = Guid.NewGuid();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCategory", new { id = category.Id }, category);
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Products) // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
                .Include(c => c.Subcategories) // Kiểm tra xem có danh mục con không
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }
             
            // Ràng buộc an toàn: Không cho xóa nếu danh mục đang chứa sách hoặc chứa danh mục con
            if (category.Products.Any() || category.Subcategories.Any())
            {
                return BadRequest(new { message = "Không thể xóa! Danh mục này đang chứa sản phẩm hoặc danh mục con." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa danh mục thành công!" });
        }
    }
}