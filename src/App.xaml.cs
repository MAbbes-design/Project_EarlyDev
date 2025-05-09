using Early_Dev_vs.src;

namespace Early_Dev_vs
{
    public partial class App : Application
    {
        public static DbService? Database { get; private set; }
        public static string dbPath { get; private set; } = string.Empty;
        public static int? ActiveStudentId { get; set; } // Store the ID of the currently active student

        public static void SetActiveStudent(int studentId)
        {
            ActiveStudentId = studentId;
        }
        public App()
        {
            InitializeComponent();

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "EarlyDevDB.db");
            Database = new DbService(dbPath);  // Initialize DbService

            // Set the initial page to LoginPage
            MainPage = new NavigationPage(new src.LoginPage());
        }
    }
}
