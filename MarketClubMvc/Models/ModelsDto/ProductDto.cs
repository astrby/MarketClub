using System.ComponentModel.DataAnnotations;

namespace MarketClubMvc.Models.ModelsDto
{
    public class ProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float Price { get; set; }
        public Uri ImageUri { get; set; }
    }
}
