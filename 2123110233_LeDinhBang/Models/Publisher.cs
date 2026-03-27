using System.ComponentModel.DataAnnotations;

namespace _2123110233_LeDinhBang.Models
{
    public class Publisher
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Tên nhà xuất bản không được để trống")]
        [StringLength(255)]
        [Display(Name = "Nhà xuất bản")]
        public string Name { get; set; }

        [StringLength(255)]
        [Display(Name = "Trang web")]
        public string Website { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        // Navigation properties
        public ICollection<Product> Products { get; set; }
    }
}
