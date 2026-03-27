using System.ComponentModel.DataAnnotations;

namespace _2123110233_LeDinhBang.Models
{
    public class Inventory
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        [Display(Name = "Số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ")]
        public int StockQuantity { get; set; }

        [Display(Name = "Số lượng đang tạm giữ")]
        [Range(0, int.MaxValue)]
        public int ReservedQuantity { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product Product { get; set; }
    }
}
