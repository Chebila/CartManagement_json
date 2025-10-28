using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Http;

namespace ECommerceApp.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ProductService _productService;
        private readonly string _cartsDir;
        private readonly object _fileLock = new();

        private const string CartCookieName = "ecom_cart_id";

        public CartService(IHttpContextAccessor httpContextAccessor, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, ProductService productService)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
            _cartsDir = Path.Combine(env.ContentRootPath, "Data", "carts");
            Directory.CreateDirectory(_cartsDir);
        }

        private string GetCartId()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) throw new Exception("No HttpContext available.");

            if (ctx.Request.Cookies.TryGetValue(CartCookieName, out var id) && !string.IsNullOrEmpty(id))
            {
                return id;
            }

            // create cookie
            var newId = Guid.NewGuid().ToString();
            ctx.Response.Cookies.Append(CartCookieName, newId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return newId;
        }

        private string CartFilePath(string cartId) => Path.Combine(_cartsDir, cartId + ".json");

        private List<CartItem> LoadCart(string cartId)
        {
            var path = CartFilePath(cartId);
            lock (_fileLock)
            {
                if (!File.Exists(path)) return new List<CartItem>();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
            }
        }

        private void SaveCart(string cartId, List<CartItem> items)
        {
            var path = CartFilePath(cartId);
            lock (_fileLock)
            {
                var tmp = path + ".tmp";
                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tmp, json);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
        }

        public IReadOnlyList<(CartItem item, Product? product)> GetCartItemsWithProducts()
        {
            var cartId = GetCartId();
            var items = LoadCart(cartId);
            return items.Select(i => (i, _productService.GetById(i.ProductId))).ToList();
        }

        public void AddToCart(string productId, int quantity = 1)
        {
            if (quantity <= 0) return;
            var cartId = GetCartId();
            var items = LoadCart(cartId);
            var existing = items.FirstOrDefault(x => x.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                items.Add(new CartItem { ProductId = productId, Quantity = quantity });
            }
            SaveCart(cartId, items);
        }

        public void UpdateQuantity(string productId, int quantity)
        {
            var cartId = GetCartId();
            var items = LoadCart(cartId);
            var existing = items.FirstOrDefault(x => x.ProductId == productId);
            if (existing != null)
            {
                if (quantity <= 0)
                    items.Remove(existing);
                else
                    existing.Quantity = quantity;
                SaveCart(cartId, items);
            }
        }

        public void RemoveFromCart(string productId)
        {
            var cartId = GetCartId();
            var items = LoadCart(cartId);
            var removed = items.RemoveAll(x => x.ProductId == productId) > 0;
            if (removed) SaveCart(cartId, items);
        }

        public void ClearCart()
        {
            var cartId = GetCartId();
            SaveCart(cartId, new List<CartItem>());
        }

        public decimal CartTotal()
        {
            var list = GetCartItemsWithProducts();
            return list.Sum(x => (x.product?.Price ?? 0m) * x.item.Quantity);
        }

        public bool IsProductInAnyCart(string productId)
        {
            foreach (var file in Directory.EnumerateFiles(_cartsDir, "*.json"))
            {
                List<CartItem>? items = null;
                lock (_fileLock)
                {
                    var json = File.ReadAllText(file);
                    items = JsonSerializer.Deserialize<List<CartItem>>(json);
                }
                if (items != null && items.Any(item => item.ProductId == productId))
                    return true;
            }
            return false;
        }
    }
}
