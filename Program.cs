using ECommerceApp.Services;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register IHttpContextAccessor so CartService can access HttpContext/cookies
builder.Services.AddHttpContextAccessor();

//register application services as sinletons (file-backed)
builder.Services.AddSingleton<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ProductViewModel>();
builder.Services.AddScoped<CartViewModel>();

var app = builder.Build();

// Ensure Data folders exist
var dataDir = Path.Combine(app.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(Path.Combine(dataDir, "carts"));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
