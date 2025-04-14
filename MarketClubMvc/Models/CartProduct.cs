namespace MarketClubMvc.Models
{
    public class CartProduct
    {
        public int Id { get; set; }
        public  Product Product { get; set; }
        public int Quantity { get; set; }
    }
}
