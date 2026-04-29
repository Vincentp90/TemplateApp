using WishlistMauiApp.PageModels;

namespace WishlistMauiApp.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginPageModel pm)
	{
		InitializeComponent();
        BindingContext = pm;
    }
}