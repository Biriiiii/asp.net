using System.ComponentModel.DataAnnotations;

namespace _2123110233_LeDinhBang.Models
{
    public class Author
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Tên tác giả không được để trống")]
        [StringLength(255)]
        [Display(Name = "Tên tác giả")]
        public string Name { get; set; }

        [Display(Name = "Tiểu sử")]
        public string Biography { get; set; }

        // Navigation properties
        public ICollection<ProductAuthor> ProductAuthors { get; set; }
    }
}
