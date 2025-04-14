using MarketClubApi.Data;
using MarketClubApi.Models;
using MarketClubApi.Models.ModelsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketClubApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AdminApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminApiController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost]
        public async Task<IActionResult> NewProduct(ProductDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("ModelNotValid");
                }

                var username = User.FindFirstValue(ClaimTypes.Name);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null)
                {
                    return BadRequest("NotAuthenticated");
                }

                Product product = new Product()
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUri = model.ImageUri,
                };

                await _context.products.AddAsync(product);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPut("{lang}/{id}")]
        public async Task<IActionResult> EditProduct(int id, string lang, ProductDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("ModelNotValid");
                }

                var username = User.FindFirstValue(ClaimTypes.Name);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null)
                {
                    return BadRequest("NotAuthenticated");
                }

                var product = await _context.products.FirstOrDefaultAsync(i => i.Id == id);

                if (product == null)
                {
                    return BadRequest("ProductNotFound");
                }

                if(lang == "en")
                {
                    product.Name = model.Name;
                    product.Description = model.Description;
                }

                product.Price = model.Price;
                product.ImageUri = model.ImageUri;

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPut("{lang}/{id}")]
        public async Task<IActionResult> EditProductTranslation(int id, string lang, ProductTranslationDto model)
        {
            ProductTranslation productTranslation = await _context.productTranslations.FirstOrDefaultAsync(p => p.ProductId == id && p.Language == lang);

            if(productTranslation != null)
            {
                productTranslation.Name = model.Name;
                productTranslation.Description = model.Description;

                await _context.SaveChangesAsync();
            }
            else
            {
                ProductTranslation productTranslationDb = new ProductTranslation()
                {
                    Name = model.Name,
                    Description = model.Description,
                    Language = lang,
                    ProductId = id
                };

                await _context.productTranslations.AddAsync(productTranslationDb);
                await _context.SaveChangesAsync();
            }
                return Ok();
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpDelete("{lang}/{id}")]
        public async Task<IActionResult> DeleteProduct(int id, string lang)
        {
            try
            {
                var username = User.FindFirstValue(ClaimTypes.Name);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null)
                {
                    return BadRequest("NotAuthenticated");
                }

                var product = await _context.products.FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return BadRequest("ProductNotFound");
                }

                _context.Remove(product);
                await _context.SaveChangesAsync();

                if(lang != "en")
                {
                    List<ProductTranslation> productTranslations = _context.productTranslations.Where(p => p.ProductId == id).ToList();

                    if(productTranslations != null)
                    {
                        _context.productTranslations.RemoveRange(productTranslations);
                        await _context.SaveChangesAsync();
                    } 
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
