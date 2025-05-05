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
            await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }

        private async void OnReportsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }

        private async void OnTestManagementTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }

        private async void OnSettingsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
        }


    }
}
