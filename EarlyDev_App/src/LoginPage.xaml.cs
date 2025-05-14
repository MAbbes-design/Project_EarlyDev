using Early_Dev_vs.src;
using Microsoft.Maui.Controls;

namespace Early_Dev_vs.src
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent(); // This links the XAML UI elements to the code-behind
        }
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;

            // Temporary hardcoded credentials
            string validUsername = "admin";
            string validPassword = "password123";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter both username and password.", "OK");
                return;
            }

            if (username == validUsername && password == validPassword)
            {
                await DisplayAlert("Success", $"Welcome, {username}!", "OK");
                await Navigation.PushAsync(new MainPage()); //send me to the mainpage on successfull login
            }
            else
            {
                await DisplayAlert("Error", "Invalid username or password.", "OK");
            }
        }

    }
}


