using MarketClubMvc.Models;
using MarketClubMvc.Models.ModelsDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MarketClubMvc.Controllers
{
    public class ProductController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;
        private readonly IStringLocalizer<ProductController> _stringLocalizer;

        public ProductController(
            IConfiguration configuration,
            IStringLocalizer<ProductController> stringLocalizer
        )
        {
            _client = new HttpClient();
            _configuration = configuration;
            _client.BaseAddress = new Uri(_configuration["BaseApiAddress"]!);
            _stringLocalizer = stringLocalizer;
        }

        [HttpGet]
        public async Task<IActionResult> AllProducts()
        {
            try
            {
                GetCart();

                string lang = "en";

                if(Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                List<Product> products = new List<Product>();

                HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress + "/ProductApi/GetAllProducts/"+lang);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    products = JsonConvert.DeserializeObject<List<Product>>(responseData)!;

                    return View(products);
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            try
            {
                List<CartProductDto> cartProductsDto = new List<CartProductDto>();

                var cartSession = HttpContext.Session.GetString("cart");

                var lang = "es";

                if (Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                if (cartSession != null)
                {
                    cartProductsDto = JsonConvert.DeserializeObject<List<CartProductDto>>(cartSession);

                    List<CartProduct> cartProducts = new List<CartProduct>();

                    foreach (var p in cartProductsDto)
                    {
                        HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress+ "/ProductApi/GetProduct/" + lang+"/"+p.Id);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseData = await response.Content.ReadAsStringAsync();

                            var product = JsonConvert.DeserializeObject<Product>(responseData);

                            cartProducts.Add(new CartProduct
                            {
                                Id = p.Id,
                                Product = product,
                                Quantity = p.Quantity
                            });
                        }
                    }

                    return View(cartProducts);
                }

                TempData["errorMessage"] = _stringLocalizer["EmptyCart"].Value;
                return Redirect(Request.GetTypedHeaders().Referer!.ToString());
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return Redirect(Request.GetTypedHeaders().Referer!.ToString());
            }
        }

        public IActionResult GetCart()
        {
            try
            {
                var cartSession = HttpContext.Session.GetString("cart");

                if (cartSession != null)
                {
                    int productQuantity = 0;

                    List<CartProduct> cartProducts = new List<CartProduct>();

                    cartProducts = JsonConvert.DeserializeObject<List<CartProduct>>(cartSession)!;

                    foreach(var p in cartProducts)
                    {
                        productQuantity = productQuantity + p.Quantity;
                    }

                    HttpContext.Session.SetInt32("cartQuant", productQuantity);

                    return Ok();
                }

                HttpContext.Session.SetInt32("cartQuant", 0);

                return Ok();
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                throw;
            }
        }

        public IActionResult AddProductToCart(Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var foundProductInCart = false;

                    List<CartProductDto> cartProducts = new List<CartProductDto>();

                    var cartSession = HttpContext.Session.GetString("cart");

                    if (cartSession != null)
                    {
                        cartProducts = JsonConvert.DeserializeObject<List<CartProductDto>>(cartSession!)!;
                    }

                    foreach(CartProductDto p in cartProducts)
                    {
                        if (p.ProductId == product.Id)
                        {
                            p.Quantity = p.Quantity + 1;

                            foundProductInCart = true;
                        }
                    }

                    if(foundProductInCart == false)
                    {
                        cartProducts.Add(new CartProductDto()
                        {
                            Id = cartProducts.ToArray().Length+1,
                            ProductId = product.Id,
                            Quantity = 1
                        });
                    }

                    HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cartProducts));

                    TempData["successMessage"] = _stringLocalizer["ProductAddedToCart"].Value;
                }

                return Redirect(Request.GetTypedHeaders().Referer!.ToString());
            }
            catch (Exception ex)
            {
                TempData["errorsMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return Redirect(Request.GetTypedHeaders().Referer!.ToString());
            }
        }

        public IActionResult DeleteCartItem(int cartItemIndex)
        {
            try
            {
                List<CartProduct> cartProducts = new List<CartProduct>();
                int cartIndex = 0;

                var cartSession = HttpContext.Session.GetString("cart");

                if (cartSession != null)
                {
                    cartProducts = JsonConvert.DeserializeObject<List<CartProduct>>(cartSession!)!;

                    foreach(CartProduct p in cartProducts.ToList())
                    {
                        if (cartItemIndex == cartIndex)
                        {
                            if (p.Quantity > 1)
                            {
                                p.Quantity = p.Quantity - 1;
                            }
                            else
                            {
                                Debug.WriteLine(cartItemIndex);
                                cartProducts.RemoveAt(cartItemIndex);
                            }
                        }  
                        cartIndex++;
                    }

                    HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cartProducts));

                    GetCart();

                    TempData["successMessage"] = _stringLocalizer["ProductCartDeleted"].Value;
                }

                return Redirect(Request.GetTypedHeaders().Referer!.ToString());
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;

                return Redirect(Request.GetTypedHeaders().Referer!.ToString());
            }
        }
    }
}
