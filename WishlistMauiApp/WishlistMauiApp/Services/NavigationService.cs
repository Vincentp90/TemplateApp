using System;
using System.Collections.Generic;
using System.Text;

namespace WishlistMauiApp.Services
{
    public interface INavigationService
    {
        Task GoToAsync(string route);
    }

    public class NavigationService : INavigationService
    {
        public Task GoToAsync(string route)
            => Shell.Current.GoToAsync(route);// TODO review is navigation service pointless? just use shell directly
    }
}
