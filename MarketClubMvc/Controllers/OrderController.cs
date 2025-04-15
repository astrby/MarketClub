using MarketClubMvc.Models;
using MarketClubMvc.Models.ModelsDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MarketClubMvc.Controllers
{
    public class OrderController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;
        private readonly IStringLocalizer<OrderController> _stringLocalizer;

        public OrderController(
            IConfiguration configuration,
            IStringLocalizer<OrderController> stringLocalizer
         )
        {
            _client = new HttpClient();
            _configuration = configuration;
            _client.BaseAddress = new Uri(_configuration["BaseApiAddress"]!);
            _stringLocalizer = stringLocalizer;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cartSession = HttpContext.Session.GetString("cart");
                List<CartProductDto> cartProductsDto = new List<CartProductDto>();

                List<CartProduct> cartProducts = new List<CartProduct>();

                if (cartSession != null)
                {
                    cartProductsDto = JsonConvert.DeserializeObject<List<CartProductDto>>(cartSession!)!;

                    var lang = "en";

                    if (Request.Cookies.TryGetValue("Language", out string value))
                    {
                        lang = value;
                    }

                    foreach (CartProductDto p in cartProductsDto)
                    {
                        HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress+ "/ProductApi/GetProduct/" + lang+"/"+p.Id);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseData = await response.Content.ReadAsStringAsync();

                            var product = JsonConvert.DeserializeObject<Models.Product>(responseData);

                            cartProducts.Add(new CartProduct
                            {
                                Id = p.Id,
                                Product = product,
                                Quantity = p.Quantity
                            });
                        }
                    }
                }

                OrderDto order = new OrderDto()
                {
                    PaymentMethod = "",
                    Shipping = "",
                    Total = 0,
                    CartProducts = cartProducts,
                    TransactionId = "",
                };

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("AllProducts", "Product");
            }
        }

        [HttpPost]
        public IActionResult Checkout(OrderDto order)
        {
            try
            {
                var currency = "crc";
                var successUrl = _configuration["BaseAddress"] + "/Order/Success";
                var cancelUrl = _configuration["BaseAddress"] + "/Order/Cancel";
                StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                    "card"
                },

                    LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = Convert.ToInt32(order.Total) * 100,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "MarketClub",
                                Description = "MarketClub Order"
                            }
                        },

                        Quantity = 1,
                    }
                },

                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                };

                var service = new SessionService();
                var session = service.Create(options);

                HttpContext.Session.SetString("stripeSessionId", session.Id);

                HttpContext.Session.SetString("total", Convert.ToString(order.Total));
                HttpContext.Session.SetString("shipping", order.Shipping);
                HttpContext.Session.SetString("paymentMethod", order.PaymentMethod);

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("AllProducts", "Product");
            }
        }

        public async Task<IActionResult> Success()
        {
            try
            {
                var stripeSessionId = HttpContext.Session.GetString("stripeSessionId");

                var service = new SessionService();
                var session = service.Get(stripeSessionId);

                if (session.PaymentStatus == "paid")
                {
                    var cartSession = HttpContext.Session.GetString("cart");

                    var total = HttpContext.Session.GetString("total");
                    var shipping = HttpContext.Session.GetString("shipping");
                    var paymentMethod = HttpContext.Session.GetString("paymentMethod");

                    List<CartProductDto> cartProductsDto = new List<CartProductDto>();

                    List<CartProduct> cartProducts = new List<CartProduct>();

                    if (cartSession != null)
                    {
                        cartProductsDto = JsonConvert.DeserializeObject<List<CartProductDto>>(cartSession!)!;

                        var lang = "en";

                        if (Request.Cookies.TryGetValue("Language", out string value))
                        {
                            lang = value;
                        }

                        foreach (CartProductDto p in cartProductsDto)
                        {
                            HttpResponseMessage responseProducts = await _client.GetAsync(_client.BaseAddress + "/ProductApi/GetProduct/" + lang + "/" + p.Id);

                            if (responseProducts.IsSuccessStatusCode)
                            {
                                var responseData = await responseProducts.Content.ReadAsStringAsync();

                                var product = JsonConvert.DeserializeObject<Models.Product>(responseData);

                                cartProducts.Add(new CartProduct
                                {
                                    Id = p.Id,
                                    Product = product,
                                    Quantity = p.Quantity
                                });
                            }
                        }

                        OrderDto order = new OrderDto()
                        {
                            CartProducts = cartProducts,
                            TransactionId = session.PaymentIntentId,
                            Total = float.Parse(total!, System.Globalization.CultureInfo.InvariantCulture),
                            Shipping = shipping!,
                            PaymentMethod = paymentMethod!
                        };

                        var token = HttpContext.Session.GetString("token");

                        string data = JsonConvert.SerializeObject(order);
                        StringContent content = new StringContent(data, Encoding.UTF8, "application/json");

                        HttpResponseMessage response;

                        if (token == null)
                        {
                            response = await _client.PostAsync(_client.BaseAddress + "/OrderApi/PostOrder", content);
                        }
                        else
                        {
                            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            response = await _client.PostAsync(_client.BaseAddress + "/OrderApi/CheckUserPostOrder", content);
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            HttpContext.Session.Remove("cart");
                            HttpContext.Session.Remove("total");
                            HttpContext.Session.Remove("shipping");
                            HttpContext.Session.Remove("paymentMethod");
                            HttpContext.Session.Remove("stripeSessionId");

                            TempData["successMessage"] = _stringLocalizer["PaymentSuccessfull"].Value;

                            return RedirectToAction("AllProducts", "Product");
                        }

                        var responseDataError = await response.Content.ReadAsStringAsync();

                        TempData["errorMessage"] = _stringLocalizer[responseDataError].Value;

                        return Cancel();
                    }
                }

                return Cancel();
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("AllProducts", "Product");
            }
        }

        public IActionResult Cancel()
        {
            TempData["errorMessage"] = _stringLocalizer["PaymentError"].Value;

            return RedirectToAction("AllProducts", "Product");
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            try
            {
                List<Order> orders = new List<Order>();

                var token = HttpContext.Session.GetString("token");

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress + "/OrderApi/GetOrders");

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    orders = JsonConvert.DeserializeObject<List<Order>>(responseData)!;

                    return View(orders);
                }

                var responseDataError = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer[responseDataError].Value;

                return RedirectToAction("AllProducts", "Product");
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("AllProducts", "Product");
            }
        }

        [HttpGet("GetOrder/{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var token = HttpContext.Session.GetString("token");

            var lang = "en";

            if (Request.Cookies.TryGetValue("Language", out string value))
            {
                lang = value;
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress + "/OrderApi/GetOrder/"+lang+"/" + id);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();

                var order = JsonConvert.DeserializeObject<Order>(responseData)!;

                return View(order);
            }

            var responseDataError = await response.Content.ReadAsStringAsync();

            TempData["errorMessage"] = _stringLocalizer[responseDataError].Value;

            return RedirectToAction("Orders","Order");
        }
    }
}
