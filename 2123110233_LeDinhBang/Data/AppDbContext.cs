using _2123110233_LeDinhBang.Models;
using Microsoft.EntityFrameworkCore;

namespace _2123110233_LeDinhBang.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Student> Students { get; set; }
    }
}

