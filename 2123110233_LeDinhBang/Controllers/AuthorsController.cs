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
    public class AuthorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthorsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Authors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Author>>> GetAuthors()
        {
            return await _context.Authors.ToListAsync();
        }

        // GET: api/Authors/5
        // Xem chi tiết tác giả và liệt kê các sách của người đó
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetAuthor(Guid id)
        {
            var author = await _context.Authors
                .Include(a => a.ProductAuthors)
                    .ThenInclude(pa => pa.Product) // Xuyên qua bảng trung gian để lấy thông tin sách
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
            {
                return NotFound();
            }

            var result = new
            {
                author.Id,
                author.Name,
                author.Biography,
                BooksWritten = author.ProductAuthors.Select(pa => new
                {
                    pa.Product.Id,
                    pa.Product.Name,
                    pa.Product.CurrentPrice
                }).ToList()
            };

            return Ok(result);
        }

        // POST: api/Authors
        [HttpPost]
        public async Task<ActionResult<Author>> PostAuthor(Author author)
        {
            author.Id = Guid.NewGuid();
            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAuthor", new { id = author.Id }, author);
        }

        // PUT: api/Authors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuthor(Guid id, Author author)
        {
            if (id != author.Id)
            {
                return BadRequest();
            }

            _context.Entry(author).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        private bool AuthorExists(Guid id)
        {
            return _context.Authors.Any(e => e.Id == id);
        }
    }
}