using Microsoft.Maui.Controls;

namespace Early_Dev_vs.src
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register Routes for Navigation
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(StudentProfilesPage), typeof(StudentProfilesPage));
            Routing.RegisterRoute(nameof(TestSessionPage), typeof(TestSessionPage));
            Routing.RegisterRoute(nameof(TestManagementPage), typeof(TestManagementPage));
            //Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
            //Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        }
    }
}

