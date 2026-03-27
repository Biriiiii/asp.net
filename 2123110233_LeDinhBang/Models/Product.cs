using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace _2123110233_LeDinhBang.Models
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà xuất bản")]
        public Guid PublisherId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(255)]
        [Display(Name = "Tên sách/Sản phẩm")]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Slug { get; set; }

        [StringLength(20)]
        [Display(Name = "Mã ISBN")]
        public string Isbn { get; set; }

        [Display(Name = "Mô tả sản phẩm")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá gốc")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá gốc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không thể là số âm")]
        public decimal OriginalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá bán hiện tại")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không thể là số âm")]
        public decimal CurrentPrice { get; set; }

        [Display(Name = "Đang mở bán")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Category Category { get; set; }
        public Publisher Publisher { get; set; }
        public Inventory Inventory { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; }
        public ICollection<ProductAuthor> ProductAuthors { get; set; }
    }
}
