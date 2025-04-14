using MarketClubApi.Data;
using MarketClubApi.Models;
using MarketClubApi.Models.ModelsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MarketClubApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderApiController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost]
        public async Task<IActionResult> CheckUserPostOrder(OrderDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("ModelNotValid");
                }

                var username = User.FindFirstValue(ClaimTypes.Name);

                if (username == null)
                {
                    return BadRequest("NotAuthenticated");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null)
                {
                    return BadRequest("UserNotFound");
                }

                //Formatting Order Product for db.
                Order order = new Order()
                {
                    Shipping = model.Shipping,
                    PaymentMethod = model.PaymentMethod,
                    Total = model.Total,
                    TransactionId = model.TransactionId,
                    UserId = user.Id,
                };

                await _context.AddAsync(order);
                await _context.SaveChangesAsync();

                foreach (var cartProduct in model.CartProducts)
                {
                    //Formatting Cart Product for db.
                    await _context.cartProducts.AddAsync(new CartProduct
                    {
                        ProductId = cartProduct.Product.Id,
                        OrderId = order.Id,
                        Quantity = cartProduct.Quantity,
                    });

                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostOrder(OrderDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("ModelNotValid");
                }

                //Formatting Order Product for db.
                Order order = new Order()
                {
                    Shipping = model.Shipping,
                    Total = model.Total,
                    PaymentMethod = model.PaymentMethod,
                    TransactionId = model.TransactionId
                };

                await _context.orders.AddAsync(order);
                await _context.SaveChangesAsync();

                foreach (var cartProduct in model.CartProducts)
                {
                    //Formatting Cart Product for db.
                    await _context.cartProducts.AddAsync(new CartProduct
                    {
                        ProductId = cartProduct.Product.Id,
                        OrderId = order.Id,
                        Quantity = cartProduct.Quantity,
                    });

                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var username = User.FindFirstValue(ClaimTypes.Name);

                if (username == null)
                {
                    return BadRequest("NotAuthenticated");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null)
                {
                    return BadRequest("UserNotFound");
                }

                List<Order> orders = _context.orders.Where(o => o.UserId == user.Id).ToList();

                List<OrderDto> ordersDto = new List<OrderDto>();

                foreach (var order in orders)
                {
                   

                    List<CartProductDto> cartProducstDto = new List<CartProductDto>();

                    //Add new Order formatted to display on client.
                    OrderDto orderDto = new OrderDto()
                    {
                        Id = order.Id,
                        PaymentMethod = order.PaymentMethod,
                        Shipping = order.PaymentMethod,
                        Total = order.Total,
                        TransactionId = order.TransactionId,
                        CartProducts = cartProducstDto,
                    };

                    ordersDto.Add(orderDto);
                }

                return Ok(ordersDto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("{lang}/{id}")]
        public async Task<IActionResult> GetOrder(int id, string lang)
        {
            Order order = await _context.orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                var username = User.FindFirstValue(ClaimTypes.Name);

                if(username == null)
                {
                    return BadRequest("NotAuthenticated");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if(user == null)
                {
                    return BadRequest("UserNotFound");
                }

                var orderMatchesUser = await _context.orders.FirstOrDefaultAsync(o => o.UserId == user.Id);

                if(orderMatchesUser == null)
                {
                    return BadRequest("NotAuthorized");
                }

                List<CartProduct> cartProducts = _context.cartProducts.Where(p => p.OrderId == order.Id).ToList();

                List<CartProductDto> cartProductsDto = new List<CartProductDto>();

                foreach (var cartProduct in cartProducts)
                {
                    Product product = await _context.products.FirstOrDefaultAsync(p => p.Id == cartProduct.ProductId);

                    if(lang != "en")
                    {
                        ProductTranslation productTranslation = await _context.productTranslations.FirstOrDefaultAsync(p => p.ProductId == product.Id && p.Language == lang);
                        
                        if(productTranslation != null)
                        {
                            product.Name = productTranslation.Name;
                            product.Description = productTranslation.Description; 
                        }
                    }

                    cartProductsDto.Add(new CartProductDto
                    {
                        Id = cartProduct.Id,
                        Product = product,
                        Quantity = cartProduct.Quantity
                    });
                }

                OrderDto orderDto = new OrderDto()
                {
                    Id = order.Id,
                    Shipping = order.Shipping,
                    PaymentMethod = order.PaymentMethod,
                    TransactionId = order.TransactionId,
                    Total = order.Total,
                    CartProducts = cartProductsDto,
                };

                return Ok(orderDto);
            }

            return Ok();
        }
    }
}
