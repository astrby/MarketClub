using Microsoft.EntityFrameworkCore;

namespace MarketClubApi.Models
{
    public class CartProduct
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        public int Quantity { get; set; }
    }
}
