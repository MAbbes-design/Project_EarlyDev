// Using Xamarin and Nunit for UI testing
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace EarlyDevUITests
{
    public class MainPageUITests
    {
        private IApp _app;
        private Platform _platform = Platform.Android; // Change to iOS if needed

        [SetUp]
        public void StartApp()
        {
            _app = ConfigureApp.Android // Use iOS if needed
                .ApkFile("path/to/your.apk") // Provide actual APK path
                .StartApp();
        }

        [Test]
        public void ClickingLessonStartNavigatesToLearnSessionPage()
        {
            _app.Tap("LessonStartButton"); // Ensure the button has an AutomationId in UI
            AppResult[] results = _app.Query(x => x.Marked("LearnSessionPageTitle"));

            Assert.IsTrue(results.Any(), "LearnSessionPage did not appear after button tap.");
        }

        [Test]
        public void ClickingStudentProfilesNavigatesToStudentProfilesPage()
        {
            _app.Tap("StudentProfilesButton");
            AppResult[] results = _app.Query(x => x.Marked("StudentProfilesPageTitle"));

            Assert.IsTrue(results.Any(), "StudentProfilesPage did not appear after button tap.");
        }

        [Test]
        public void ClickingReportsNavigatesToReportsPage()
        {
            _app.Tap("ReportsButton");
            AppResult[] results = _app.Query(x => x.Marked("ReportsPageTitle"));

            Assert.IsTrue(results.Any(), "ReportsPage did not appear after button tap.");
        }

        [Test]
        public void ClickingTestManagementNavigatesToQuestionManagementPage()
        {
            _app.Tap("TestManagementButton");
            AppResult[] results = _app.Query(x => x.Marked("QuestionManagementPageTitle"));

            Assert.IsTrue(results.Any(), "QuestionManagementPage did not appear after button tap.");
        }
    }
}