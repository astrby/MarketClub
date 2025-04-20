using MarketClubApi.Data;
using MarketClubApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MarketClubApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{lang}")]
        public async Task<IActionResult> GetAllProducts(string lang)
        {
            try
            { 
                List<Product> products = await _context.products.ToListAsync();

                if (products != null)
                {
                    if (lang != "en")
                    {
                        List<ProductTranslation> productTranslations = await _context.productTranslations.Where(p => p.Language == lang).ToListAsync();

                        foreach (var p in products)
                        {
                            foreach(var l in productTranslations)
                            {
                                if (p.Id == l.ProductId)
                                {
                                    p.Name = l.Name;
                                    p.Description = l.Description;
                                }
                            }
                        }
                    }
                    return Ok(products);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{lang}/{search}")]
        public async Task<IActionResult> GetProductResults(string lang, string search)
        {
            try
            {
                if (lang == "en")
                {
                    List<Product> products = await _context.products.Where(p => p.Name.Contains(search)).ToListAsync();

                    return Ok(products);
                }

                List<Product> productsTranslated = new List<Product>();

                List<ProductTranslation> productTranslations = await _context.productTranslations.Where(p => p.Name.Contains(search) && p.Language == lang).ToListAsync();

                foreach (var pt in productTranslations)
                {
                    var product = await _context.products.FirstOrDefaultAsync(p => p.Id == pt.ProductId);

                    product.Name = pt.Name;
                    product.Description = pt.Description;

                    productsTranslated.Add(product);
                }

                return Ok(productsTranslated);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{lang}/{id}")]
        public async Task<IActionResult> GetProduct(int id, string lang)
        {
            var product = await _context.products.FirstOrDefaultAsync(i => i.Id == id);


            if(product != null)
            {
                ProductTranslation productTranslation = await _context.productTranslations.FirstOrDefaultAsync(p => p.ProductId == id && p.Language == lang);

                if(productTranslation != null)
                {
                    product.Name = productTranslation.Name;
                    product.Description = productTranslation.Description;
                }

                return Ok(product);
            }

            return Ok();
        }

        [HttpGet("{language}/{id}")]
        public async Task<IActionResult> GetProductTranslation(int id, string language)
        {
            ProductTranslation productTranslation = await _context.productTranslations.FirstOrDefaultAsync(p => p.ProductId == id && p.Language == language);

            if (productTranslation == null)
            {
                productTranslation = new ProductTranslation
                {
                    Id = 0,
                    ProductId = id,
                    Language = language,
                    Description = "",
                    Name = "",
                };
            }

            return Ok(productTranslation);
        }
    }

}
