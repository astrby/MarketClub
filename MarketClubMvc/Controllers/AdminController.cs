using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MarketClubMvc.Models;
using MarketClubMvc.Models.ModelsDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MarketClubMvc.Controllers
{
    public class AdminController : Controller
    {
        private const string ContainerName = "marketclubcontainer";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly IStringLocalizer<AdminController> _stringLocalizer;

        private readonly IConfiguration _configuration;

        private readonly HttpClient _client;

        public AdminController
        (
            BlobServiceClient blobServiceClient,
            IConfiguration configuration,
            IStringLocalizer<AdminController> stringLocalizer
        )
        {
            _client = new HttpClient();
            _configuration = configuration;
            _client.BaseAddress = new Uri(_configuration["BaseApiAddress"]!);

            _blobServiceClient = blobServiceClient;
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            _blobContainerClient.CreateIfNotExists();
            _configuration = configuration;

            _stringLocalizer = stringLocalizer;
        }

        [HttpGet]
        public IActionResult NewProduct()
        {
            var adminLoggedIn = HttpContext.Session.GetString("token");
            var userRole = HttpContext.Session.GetString("userRole");

            if (adminLoggedIn == null || userRole != "Admin")
            {
                TempData["errorMessage"] = "User not authorized";
                return RedirectToAction("AllProducts","Product");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewProduct(ProductDtoForm model)
        {
            try
            {
                var blobCient = _blobContainerClient.GetBlobClient(model.Name + ".jpg");
                var options = new BlobUploadOptions()
                {
                    HttpHeaders = new BlobHttpHeaders() { ContentType = "image/png" }
                };

                await blobCient.UploadAsync(model.Image.OpenReadStream(), options);

                var blobUrl = blobCient.Uri.AbsoluteUri;

                ProductDto productDto = new ProductDto()
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUri = new System.Uri(blobUrl),
                };

                string token = HttpContext.Session.GetString("token")!;

                string data = JsonConvert.SerializeObject(productDto);
                StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await _client.PostAsync(_client.BaseAddress + "/AdminApi/NewProduct", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["successMessage"] = _stringLocalizer["ProductAddedSuccessfully"].Value;
                    return RedirectToAction("AllProducts", "Product");
                }

                var responseDataError = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer[responseDataError].Value;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageProducts()
        {
            try
            {
                var adminLoggedIn = HttpContext.Session.GetString("token");
                var userRole = HttpContext.Session.GetString("userRole");

                if (adminLoggedIn == null || userRole != "Admin")
                {
                    TempData["errorMessage"] = _stringLocalizer["NotAuthorized"].Value;
                    return RedirectToAction("AllProducts", "Product");
                }

                List<Product> products = new List<Product>();

                var lang = "en";

                if(Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress + "/ProductApi/GetAllProducts/"+lang);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    products = JsonConvert.DeserializeObject<List<Product>>(responseData)!;
                }

                return View(products);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
            }
            return View();
        }

        [HttpGet("EditProduct/{id}")]
        public async Task<IActionResult> EditProduct(int id)
        {
            try
            {
                var adminLoggedIn = HttpContext.Session.GetString("token");
                var userRole = HttpContext.Session.GetString("userRole");

                if (adminLoggedIn == null || userRole != "Admin")
                {
                    TempData["errorMessage"] = _stringLocalizer["NotAuthorized"].Value;
                    return RedirectToAction("AllProducts", "Product");
                }

                Product product = new Product();

                var lang = "en";
                if(Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress + "/ProductApi/GetProduct/"+lang+"/"+ id);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    product = JsonConvert.DeserializeObject<Product>(responseData)!;

                    return View(product);
                }

                return RedirectToAction("ManageProducts", "Admin");
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("ManageProducts", "Admin");
            }
        }

        [HttpPost("EditProductConfirm/{id}")]
        public async Task<IActionResult> EditProductConfirm(int id,Product model, IFormFile image)
        {
            try
            {
                var adminLoggedIn = HttpContext.Session.GetString("token");
                var userRole = HttpContext.Session.GetString("userRole");

                if (adminLoggedIn == null || userRole != "Admin")
                {
                    TempData["errorMessage"] = _stringLocalizer["NotAuthorized"].Value;
                    return RedirectToAction("AllProducts", "Product");
                }

                ModelState.Remove("image");

                if (!ModelState.IsValid)
                {
                    TempData["errorMessage"] = _stringLocalizer["ModelNotValid"].Value;
                    return RedirectToAction("ManageProducts", "Admin");
                }

                ProductDto productDto = new ProductDto()
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUri = model.ImageUri,
                };

                var lang = "en";
                if (Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                if (image != null)
                {
                    var blobClient = _blobContainerClient.GetBlobClient(model.Name + ".jpg");

                    await blobClient.DeleteIfExistsAsync();

                    var blobOptions = new BlobUploadOptions()
                    {
                        HttpHeaders = new BlobHttpHeaders { ContentType = "image/png" }
                    };

                    await blobClient.UploadAsync(image.OpenReadStream(), blobOptions);

                    var blobUri = blobClient.Uri.AbsoluteUri;

                    productDto.ImageUri = new System.Uri(blobUri);
                }

                var token = HttpContext.Session.GetString("token");

                var data = JsonConvert.SerializeObject(productDto);
                StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await _client.PutAsync(_client.BaseAddress + "/AdminApi/EditProduct/"+lang+"/"+model.Id, content);

                if(lang != "en")
                {
                    ProductTranslationDto productTranslationDto = new ProductTranslationDto()
                    {
                        ProductId = model.Id,
                        Name = model.Name,
                        Description = model.Description,
                        Language = lang
                    };

                    data = JsonConvert.SerializeObject(productTranslationDto);
                    content = new StringContent(data, Encoding.UTF8, "application/json");
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    response = await _client.PutAsync(_client.BaseAddress+"/AdminApi/EditProductTranslation/"+lang+"/"+model.Id, content);
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["successMessage"] = _stringLocalizer["ProductEditedSuccessfully"].Value;
                    return RedirectToAction("ManageProducts", "Admin");
                }

                var responseDataError = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer[responseDataError].Value;

                return RedirectToAction("ManageProducts", "Admin");
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("ManageProducts", "Admin");
            }
        }

        [HttpGet("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var adminLoggedIn = HttpContext.Session.GetString("token");
                var userRole = HttpContext.Session.GetString("userRole");

                if (adminLoggedIn == null || userRole != "Admin")
                {
                    TempData["errorMessage"] = _stringLocalizer["NotAuthorized"].Value;
                    return RedirectToAction("AllProducts", "Product");
                }

                var lang = "en";
                if (Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                Product product = new Product();
                HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress + "/ProductApi/GetProduct/"+lang+ "/" + id);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    product = JsonConvert.DeserializeObject<Product>(responseData)!;

                    return View(product);
                }

                var responseDataError = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer[responseDataError].Value;

                return RedirectToAction("ManageProducts", "Admin");
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("ManageProducts", "Admin");
            }
        }

        [HttpPost("DeleteProductConfirm/{id}")]
        public async Task<IActionResult> DeleteProductConfirm(int id, string productName)
        {
            try
            {
                var lang = "en";
                if (Request.Cookies.TryGetValue("Language", out string value))
                {
                    lang = value;
                }

                var token = HttpContext.Session.GetString("token");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await _client.DeleteAsync(_client.BaseAddress + "/AdminApi/DeleteProduct/" + lang + "/" + +id);

                if (response.IsSuccessStatusCode)
                {
                    var blobClient = _blobContainerClient.GetBlobClient(productName + ".jpg");

                    await blobClient.DeleteIfExistsAsync();

                    TempData["successMessage"] = _stringLocalizer["ProductDeletedSuccessfully"].Value;

                    return RedirectToAction("AllProducts", "Product");
                }


                var responseData = await response.Content.ReadAsStringAsync();

                TempData["errorMessage"] = _stringLocalizer["ProductNotFound"].Value;

                return RedirectToAction("ManageProducts", "Admin");
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = _stringLocalizer["ExceptionError"].Value + ex.Message;
                return RedirectToAction("ManageProducts", "Admin");
            }
        }

        public IActionResult UpdateLanguageTranslation(string language)
        {
            HttpContext.Session.SetString("language",language);

            return Redirect(Request.GetTypedHeaders().Referer!.ToString());
        }
    }
}
