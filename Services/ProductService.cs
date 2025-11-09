using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using ECommerceApp.Models;
using System.Threading;

namespace ECommerceApp.Services
{
    // file-backed product store
    public class ProductService
    {
        private readonly string _filePath;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private List<Product> _products = new();

        public ProductService(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "Data");
            _filePath = Path.Combine(dataDir, "products.json");
            LoadOrCreateDefault();
        }

        private void LoadOrCreateDefault()
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _products = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                }
                else
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _products = new List<Product>
                        {
                            new Product { Name = "t-shirt", Description = "comfortable t-shirt", Price = 70.50m, ImageUrl = "/images/tshirt.jpeg" },
                            new Product { Name = "pants", Description = "comfortable pants", Price = 80.50m, ImageUrl = "/images/pants.png" },
                            new Product { Name = "jacket", Description = "comfortable jacket", Price = 200.50m, ImageUrl = "/images/jacket.jpeg" },
                        };
                        Save();
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private void Save()
        {
            var tmp = _filePath + ".tmp";
            var json = JsonSerializer.Serialize(_products, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmp, json);
            File.Copy(tmp, _filePath, true);
            File.Delete(tmp);
        }

        public List<Product> GetAll()
        {
            _lock.EnterReadLock();
            try { return _products.Select(p => Clone(p)).ToList(); }
            finally { _lock.ExitReadLock(); }
        }

        public Product? GetById(string id)
        {
            _lock.EnterReadLock();
            try { return _products.FirstOrDefault(p => p.Id == id) is Product p ? Clone(p) : null; }
            finally { _lock.ExitReadLock(); }
        }

        public Product Add(Product p)
        {
            _lock.EnterWriteLock();
            try
            {
                p.Id = System.Guid.NewGuid().ToString();
                _products.Add(p);
                Save();
                return Clone(p);
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Update(Product p)
        {
            _lock.EnterWriteLock();
            try
            {
                var existing = _products.FirstOrDefault(x => x.Id == p.Id);
                if (existing == null) return false;
                existing.Name = p.Name;
                existing.Description = p.Description;
                existing.Price = p.Price;
                existing.ImageUrl = p.ImageUrl;
                Save();
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Delete(string id)
        {
            _lock.EnterWriteLock();
            try
            {
                var removed = _products.RemoveAll(x => x.Id == id) > 0;
                if (removed) Save();
                return removed;
            }
            finally { _lock.ExitWriteLock(); }
        }

        private static Product Clone(Product p) =>
            new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl
            };
    }
}