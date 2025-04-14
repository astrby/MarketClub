using System.ComponentModel.DataAnnotations;

namespace MarketClubMvc.Models.ModelsDto
{
    public class ProductDtoForm
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        [Range(500, 1000000, ErrorMessage ="Price must be between 500 and 1000000")]
        public float Price { get; set; }
        [Required]
        public  IFormFile Image{ get; set; }
    }
}
