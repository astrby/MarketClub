using MarketClubApi.Data;
using MarketClubApi.Models;
using MarketClubApi.Models.ModelsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MarketClubApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountApiController
        (
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IConfiguration configuration,
            AppDbContext context
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> UserSignup(UserSignupDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("ModelNotValid");
                }

                var emailExists = await _userManager.FindByEmailAsync(model.Email);

                if(emailExists != null)
                {
                    return BadRequest("EmailAlreadyRegistered");
                }

                var usernameExists = await _userManager.FindByNameAsync(model.Username);

                if(usernameExists != null)
                {
                    return BadRequest("UsernameAlreadyRegistered");
                }

                User user = new User();

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Address = model.Address;
                user.UserName = model.Username;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    return BadRequest("UserNotRegistered");
                }

                await _userManager.AddToRoleAsync(user, "User");

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("ModelNotValid");
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, false);

            if (!result.Succeeded)
            {
                return BadRequest("InvalidCredentials");
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == model.Username);

            var userRole = await _userManager.GetRolesAsync(user!);

            var token = GenerateJwtToken(model.Username);
           
            return Ok(new { 
                token = token,
                username = user.UserName,
                userRole = userRole[0]
            });
        }

        [Authorize(AuthenticationSchemes ="Bearer")]
        [HttpGet]
        public async Task<IActionResult> EditAccount()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);

            var user = await _userManager.FindByNameAsync(username);

            if(user == null)
            {
                return BadRequest("UserNotFound");
            }

            return Ok(user);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost]
        public async Task<IActionResult> EditAccountConfirm(User model)
        {
            User user = await _userManager.FindByIdAsync(model.Id);

            if(user == null)
            {
                return BadRequest("UserNotFound");
            }

            if(model.UserName != user.UserName)
            {
                var usernameFound = await _userManager.FindByNameAsync(model.UserName);
            
                if(usernameFound != null)
                {
                    Debug.WriteLine("UsernameFound");
                    return BadRequest("UsernameFound");
                }
            }

            if (model.Email != user.Email)
            {
                var emailFound = await _userManager.FindByEmailAsync(model.Email);

                if (emailFound != null)
                {
                    Debug.WriteLine("EmailFound");
                    return BadRequest("EmailFound");
                }
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;

            var updateUser = await _userManager.UpdateAsync(user);

            if (updateUser.Succeeded)
            {
                var token = GenerateJwtToken(user.UserName);

                var userRole = await _userManager.GetRolesAsync(user!);

                return Ok(new
                {
                    token = token,
                    user = user,
                    userRole = userRole[0]
                });
            }

            return Ok();
        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AppSettings:JwtTokenKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["AppSettings:Issuer"],
                audience: _configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
