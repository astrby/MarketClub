namespace MarketClubMvc.Models.ModelsDto
{
    public class EditAccountResponseDto
    {
        public string Token { get; set; }
        public User User { get; set; }
        public string UserRole { get; set; }
    }
}
