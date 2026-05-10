using System;

namespace PaletixDesktop.Services
{
    public sealed class NavigationService
    {
        public event EventHandler<string>? NavigationRequested;

        public void RequestNavigation(string route)
        {
            NavigationRequested?.Invoke(this, route);
        }
    }
}
