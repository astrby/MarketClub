using Azure.Storage.Blobs;
using MarketClubMvc.Models.StripeSettings;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);


//Configuration for language options.
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("es"),
    };
    options.DefaultRequestCulture = new RequestCulture("es");
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
});

builder.Services.AddSingleton(x => new BlobServiceClient(builder.Configuration.GetConnectionString("StorageAccount")));

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//Use language localization.
app.UseRequestLocalization();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

//Retrieve language from cookies.
app.Use(async (context, next) =>
{
    string cookie = string.Empty;
    if(context.Request.Cookies.TryGetValue("Language", out cookie!))
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cookie);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(cookie);
    }
    await next.Invoke();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=AllProducts}/{id?}");

app.Run();