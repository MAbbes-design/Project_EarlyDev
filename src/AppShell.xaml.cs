using Microsoft.Maui.Controls;

namespace Early_Dev_vs.src
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register Routes for Navigation
            Routing.RegisterRoute(nameof(src.LoginPage), typeof(src.LoginPage));
            Routing.RegisterRoute(nameof(src.MainPage), typeof(src.MainPage));
        }
    }
}
