using MarketClubMvc.Models;
using MarketClubMvc.Models.ModelsDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MarketClubMvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;
        private readonly IStringLocalizer<AccountController> _stringLocalizer;

        public AccountController(
            IConfiguration configuration,
            IStringLocalizer<AccountController> stringLocalizer
        )
        {
            _client = new HttpClient();
            _configuration = configuration;
            _client.BaseAddress = new Uri(_configuration["BaseApiAddress"]!);
            _stringLocalizer = stringLocalizer;
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignupDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["errorMessage"] = _stringLocalizer["ModelNotValid"].Value;
                    return View(model);
                }

                string data = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _client.PostAsync(_client.BaseAddress + "/AccountApi/UserSignup", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["successMessage"] = _stringLocalizer["UserRegisteredSuccessfully"].Value;
                    return RedirectToAction("Login", "Account");
                }

                var responseDataError = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer[responseDataError];

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            try
            {
                LoginResponseDto loginResponse = new LoginResponseDto();

                if (!ModelState.IsValid)
                {
                    TempData["errorMessage"] = _stringLocalizer["ModelNotValid"].Value;
                    return View(model);
                }

                string data = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _client.PostAsync(_client.BaseAddress + "/AccountApi/UserLogin", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();

                    loginResponse = JsonConvert.DeserializeObject<LoginResponseDto>(responseData)!;

                    HttpContext.Session.SetString("token", loginResponse.Token);
                    HttpContext.Session.SetString("username", loginResponse.Username);
                    HttpContext.Session.SetString("userRole", loginResponse.UserRole);

                    TempData["successMessage"] = _stringLocalizer["LoggedInSuccessfully"].Value;

                    return RedirectToAction("AllProducts", "Product");
                }

                var responseDataError = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer[responseDataError];
                return View(model);  
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return View(model);
            }
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> EditAccount(string username)
        {
            if(username == null)
            {
                TempData["errorMessage"] = "BlankUsername";
                return RedirectToAction("AllProducts","Product");
            }

            var token = HttpContext.Session.GetString("token");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",token);
            HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress+"/AccountApi/EditAccount");

            if (response.IsSuccessStatusCode)
            {
                User user = new User();

                var responseData = await response.Content.ReadAsStringAsync();
                user = JsonConvert.DeserializeObject<User>(responseData);

                return View(user);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditAccountConfirm(User model)
        {
            if (!ModelState.IsValid)
            {
                TempData["errorMessage"] = "ModelNotValid";
            }

            var data = JsonConvert.SerializeObject(model);
            var token = HttpContext.Session.GetString("token");

            StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",token);
            HttpResponseMessage response = await _client.PostAsync(_client.BaseAddress+"/AccountApi/EditAccountConfirm", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                EditAccountResponseDto editAccountResponse = JsonConvert.DeserializeObject<EditAccountResponseDto>(responseData);

                HttpContext.Session.SetString("token", editAccountResponse.Token);
                HttpContext.Session.SetString("username", editAccountResponse.User.Username);

                return RedirectToAction("EditAccount", "Account", new { username = editAccountResponse.User.Username });
            }

            return RedirectToAction("EditAccount","Account", new {username = model.Username});
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("token");
            HttpContext.Session.Remove("username");
            HttpContext.Session.Remove("userRole");
            return RedirectToAction("Login","Account");
        }
    }
}