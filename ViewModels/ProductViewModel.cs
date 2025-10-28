using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ECommerceApp.Models;
using ECommerceApp.Services;

namespace ECommerceApp.ViewModels
{
    public class ProductViewModel : INotifyPropertyChanged
    {
        private readonly ProductService _productService;
        private readonly CartService _cartService;

        private List<Product> _products;
        public List<Product> Products
        {
            get => _products;
            private set
            {
                _products = value;
                OnPropertyChanged();
            }
        }

        public Product NewProduct { get; set; } = new Product();
        public Product? EditingProduct { get; set; }
        public string? ErrorMessage { get; set; }

        public ProductViewModel(ProductService productService, CartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
            _products = new List<Product>();
            LoadProducts();
        }

        public void LoadProducts()
        {
            Products = _productService.GetAll();
        }

        public void AddProduct()
        {
            _productService.Add(NewProduct);
            NewProduct = new Product();
            LoadProducts();
        }

        public void StartEdit(string id)
        {
            EditingProduct = _productService.GetById(id);
        }

        public void SaveEdit()
        {
            if (EditingProduct != null)
            {
                _productService.Update(EditingProduct);
                LoadProducts();
                EditingProduct = null;
            }
        }

        public void CancelEdit()
        {
            EditingProduct = null;
        }

        public bool Delete(string id)
        {
            if (_cartService.IsProductInAnyCart(id))
            {
                ErrorMessage = "Cannot delete product: It exists in cart";
                return false;
            }
            _productService.Delete(id);
            LoadProducts();
            ErrorMessage = null;
            return true;
        }

        public void AddToCart(string productId)
        {
            _cartService.AddToCart(productId);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}