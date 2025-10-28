using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ECommerceApp.Models;
using ECommerceApp.Services;

namespace ECommerceApp.ViewModels
{
    public class CartViewModel : INotifyPropertyChanged
    {
        private readonly CartService _cartService;
        private IReadOnlyList<(CartItem item, Product? product)> _items;

        public IReadOnlyList<(CartItem item, Product? product)> Items
        {
            get => _items;
            private set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        public CartViewModel(CartService cartService)
        {
            _cartService = cartService;
            _items = new List<(CartItem, Product?)>();
            LoadCart();
        }

        public void LoadCart()
        {
            Items = _cartService.GetCartItemsWithProducts();
        }

        public void UpdateQuantity(string productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);
            LoadCart();
        }

        public void Remove(string productId)
        {
            _cartService.RemoveFromCart(productId);
            LoadCart();
        }

        public void Clear()
        {
            _cartService.ClearCart();
            LoadCart();
        }

        public decimal CartTotal => _cartService.CartTotal();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}