using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Platform;
using WishlistMauiApp.Pages;
using WishlistMauiApp.Services;

namespace WishlistMauiApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var sp = Handler.MauiContext!.Services;
            return new Window(sp.GetRequiredService<AppShell>());
        }
    }
}