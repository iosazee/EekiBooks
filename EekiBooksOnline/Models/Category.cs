using System.ComponentModel.DataAnnotations;

namespace EekiBooksOnline.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
