using Early_Dev_vs.src;

namespace Early_Dev_vs.src
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnStudentProfilesTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
            await Navigation.PushAsync(new StudentProfilesPage());
        }

        private async void OnLessonStartTapped(object sender, EventArgs e)
        {
            if (!App.ActiveStudentId.HasValue) //  Check if a student is selected
            {
                await DisplayAlert("Select a Student", "Please choose a student before starting!", "OK");
                await Navigation.PushAsync(new StudentProfilesPage()); //  Redirect to selection page
                return;
            }

            int studentId = App.ActiveStudentId.Value; // Safely retrieve value

            if (App.Database == null) //  Check if the database is initialized
            {
                await DisplayAlert("Error", "Database is not initialized!", "OK");
                return;
            }

            var student = await App.Database.GetStudentByIdAsync(studentId); // Fetch student data safely

            if (student != null && !string.IsNullOrWhiteSpace(student.Name))
            {
                await Navigation.PushAsync(new TestSessionPage(App.dbPath, studentId, student.Name)); //  Navigate to session
            }
            else
            {
                await DisplayAlert("Error", "Selected student has no name saved in the database!", "OK");
                await Navigation.PushAsync(new StudentProfilesPage()); //  Redirect to selection if student is missing
            }
        }

        private async void OnReportsTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ReportsPage());
            //await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }

        private async void OnTestManagementTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TestManagementPage(App.Database!));
            //await Navigation.PushAsync(new TestManagementPage());
            //await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }

        private async void OnSettingsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }


    }
}
