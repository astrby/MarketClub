using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MarketClubMvc.Models;
using System.Globalization;

namespace MarketClubMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }



    public IActionResult ChangeLanguage(string lang)
    {
        //Save language in cookies.
        Response.Cookies.Append("Language",lang);

        return Redirect(Request.GetTypedHeaders().Referer!.ToString());
    }
}
