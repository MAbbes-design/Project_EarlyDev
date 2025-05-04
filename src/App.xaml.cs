namespace Early_Dev_vs
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set the initial page to LoginPage
            MainPage = new NavigationPage(new src.LoginPage());
        }
    }
}
