using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using WishlistMauiApp.Services;

namespace WishlistMauiApp.PageModels
{
    public partial class LoginPageModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        public LoginPageModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        public bool isNotBusy = true;

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (!IsNotBusy) return;

            try
            {
                IsNotBusy = false;

                await _authService.LoginAsync(Username, Password);

                await _navigationService.GoToAsync("//MainPage");
            }
            catch (UnauthorizedAccessException)
            {
                //ErrorMessage = "Incorrect email or password";
            }
            catch (Exception)
            {
                //ErrorMessage = "Unknown error";
            }
            finally
            {
                IsNotBusy = true;
            }
        }
    }
}
