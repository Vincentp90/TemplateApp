using WishlistMauiApp.PageModels;
using WishlistMauiApp.Services;

namespace WishlistMauiApp
{
    public partial class AppShell : Shell
    {
        public AppShell(IAuthService authService)
        {
            InitializeComponent();

            _ = InitializeAsync(authService);
        }

        private async Task InitializeAsync(IAuthService authService)
        {
            if (await authService.IsAuthenticatedAsync())
                await GoToAsync("//MainPage");
            else
                await GoToAsync("//login");
        }
    }
}
