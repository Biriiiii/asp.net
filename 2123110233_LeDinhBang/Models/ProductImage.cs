using System.ComponentModel.DataAnnotations;

namespace _2123110233_LeDinhBang.Models
{
    public class ProductImage
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh không được để trống")]
        public string ImageUrl { get; set; }

        [Display(Name = "Là ảnh bìa chính")]
        public bool IsPrimary { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; }

        // Navigation properties
        public Product Product { get; set; }
    }
}
