using Early_Dev_vs.src;

namespace Early_Dev_vs
{
    public partial class App : Application
    {
        public static DbService? Database { get; private set; }
        public App()
        {
            InitializeComponent();

            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "EarlyDevDB.db");
            Database = new DbService(dbPath);  // Initialize DbService

            // Set the initial page to LoginPage
            MainPage = new NavigationPage(new src.LoginPage());
        }
    }
}
