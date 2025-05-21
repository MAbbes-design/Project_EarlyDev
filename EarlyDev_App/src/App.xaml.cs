using Early_Dev_vs.src;
using Microcharts;
using static Early_Dev_vs.src.DataModels;

namespace Early_Dev_vs
{
    public partial class App : Application
    {
        public static DbService? Database { get; private set; }
        public static string dbPath { get; private set; } = string.Empty;
        public static int? ActiveStudentId { get; set; } // Store the ID of the currently active student
        public static StudentProfile? ActiveStudent { get; set; } // Store the currently active student profile
        public static List<TestSessionRecord>? FilteredTestSessions { get; set; }
        public static List<ChartEntry>? FilteredChartEntries { get; set; }

        public static void SetActiveStudent(int studentId, StudentProfile? student = null)
        {
            ActiveStudentId = studentId;
            if (student != null)
                ActiveStudent = student;
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
