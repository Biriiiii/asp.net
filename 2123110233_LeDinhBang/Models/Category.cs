using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2123110233_LeDinhBang.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(255)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Đường dẫn (Slug) không được để trống")]
        [StringLength(255)]
        public string Slug { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Category Parent { get; set; }
        public ICollection<Category> Subcategories { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
