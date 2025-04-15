namespace MarketClubMvc.Models.ModelsDto
{
    public class OrderDto
    {
        public List<CartProduct> CartProducts { get; set; }

        public string Shipping { get; set; }
        public float Total { get; set; }
        public string PaymentMethod { get; set; }

        public string TransactionId { get; set; }
    }
}
