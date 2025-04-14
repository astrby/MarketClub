namespace MarketClubMvc.Models
{
    public class Order
    {
        public int Id { get; set; }

        public List<CartProduct> CartProducts { get; set; }

        public string Shipping { get; set; }
        public float Total { get; set; }
        public string PaymentMethod { get; set; }

        public string TransactionId { get; set; }
        public string? UserId { get; set; }
    }
}
