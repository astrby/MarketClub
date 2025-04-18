﻿using Microsoft.AspNetCore.Identity;

namespace MarketClubMvc.Models
{
    public class User
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }

    }
}
