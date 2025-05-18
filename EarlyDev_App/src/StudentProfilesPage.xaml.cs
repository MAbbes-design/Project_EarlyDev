using Early_Dev_vs.src;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using static Early_Dev_vs.src.DataModels;

namespace Early_Dev_vs.src
{
    public partial class StudentProfilesPage : ContentPage
    {
        public StudentProfilesPage()
        {
            InitializeComponent();
        }

        // Handle "Create New Student Profile" tap
        private void OnCreateStudentTapped(object sender, EventArgs e)
        {
            CreateStudentForm.IsVisible = !CreateStudentForm.IsVisible; // Toggle visibility
        }

        // Handle "Search Student Profiles" tap
        private void OnSearchStudentTapped(object sender, EventArgs e)
        {
            SearchStudentForm.IsVisible = !SearchStudentForm.IsVisible; // Toggle visibility
            StudentDetailsSection.IsVisible = false; // hide all the tiles after you tap search button again
            LetsLearnTile.IsVisible = false;
            ReportsTile.IsVisible = false;
            IAPTile.IsVisible = false;
        }

        // Handle Save Student Profile
        private async void OnSaveStudent(object sender, EventArgs e)
        {
            // Create a new student profile from form inputs
            var student = new StudentProfile
            {
                Name = StudentNameEntry.Text,
                Age = int.Parse(StudentAgeEntry.Text),
                BCBA = BCBANameEntry.Text,
                EducationLevel = EducationLevelEntry.Text,
                CompletedSessions = 0, // initialise this, ill need it later
                IncompleteSessions = 0 // initialise this also so I can bump the number later in the test sessions page.. I'll need more metric gathering but possibly in the other pages
            };

            // Save to SQLite Database using DbService
            if (App.Database != null)
            {
                await App.Database.AddStudentAsync(student);

                //  Check if the student exists after saving
                var savedStudent = await App.Database.GetStudentByIdAsync(student.Id);
                Debug.WriteLine(savedStudent != null
                    ? $"Student Verified in DB - ID: {savedStudent.Id}, Name: {savedStudent.Name}"
                    : $"Student NOT found in DB immediately after save!");

                App.SetActiveStudent(student.Id);
            }
            else
            {
                await DisplayAlert("Error", "Database not initialized!", "OK");
            }

            await DisplayAlert("Success", $"Student {student.Name} added!", "OK");

            // Clear the form after saving
            StudentNameEntry.Text = string.Empty;
            StudentAgeEntry.Text = string.Empty;
            BCBANameEntry.Text = string.Empty;
            EducationLevelEntry.Text = string.Empty;
            CreateStudentForm.IsVisible = false; // Hide form
        }

        // Handle Cancel Student Profile Creation
        private void OnCancelStudent(object sender, EventArgs e)
        {
            CreateStudentForm.IsVisible = false; // Hide form
        }

        // Handle Search for Student Profile
        private async void OnFindStudent(object sender, EventArgs e)
        {
            string searchQuery = SearchStudentEntry.Text?.Trim() ?? string.Empty; // Get user input

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await DisplayAlert("Error", "Please enter a name to search!", "OK");
                return;
            }

            var students = await (App.Database?.GetAllStudentsAsync() ?? Task.FromResult(new List<StudentProfile>()));
            if (students == null || students.Count == 0)
            {
                await DisplayAlert("Error", "No students found in the database!", "OK");
                return;
            }

            var matchedStudent = students.FirstOrDefault(s => !string.IsNullOrEmpty(s.Name) && s.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

            if (matchedStudent != null)
            {
                App.SetActiveStudent(matchedStudent.Id, matchedStudent); // Set active student

                // Ensure `studentName` is never null by providing a default value
                string studentName = matchedStudent.Name ?? "Unknown Student";

                StudentDetailsSection.IsVisible = true;
                ShowStudentProfile(studentName, matchedStudent.Age.ToString(), matchedStudent.BCBA ?? "Not Provided", matchedStudent.EducationLevel ?? "Not Specified");
            }
            else
            {
                await DisplayAlert("Error", "Student not found!", "OK");
            }
            SearchStudentEntry.Text = string.Empty;
        }


        // Display Student Profile (Example placeholder)
        public void ShowStudentProfile(string studentName, string age, string bcba, string education)
        {
            StudentDetailsSection.IsVisible = true;
            LetsLearnTile.IsVisible = true;
            ReportsTile.IsVisible = true;
            IAPTile.IsVisible = true;

            StudentNameLabel.Text = $"Name: {studentName}";
            StudentAgeLabel.Text = $"Age: {age}";
            BCBANameLabel.Text = $"Assigned BCBA: {bcba}";
            EducationLevelLabel.Text = $"Education Level: {education}";
        }

        // Navigation to Let's Learn Page
        private async void OnLetsLearnTapped(object sender, EventArgs e)
        {
            if (!App.ActiveStudentId.HasValue) //  Ensure a student ID is available
            {
                await DisplayAlert("Select a Student", "Please choose a student before starting!", "OK");
                return;
            }

            int studentId = App.ActiveStudentId.Value; // Safely retrieve value

            if (App.Database == null) //  Ensure the database is initialized
            {
                await DisplayAlert("Error", "Database is not initialized!", "OK");
                return;
            }

            var student = await App.Database.GetStudentByIdAsync(studentId); //  Fetch student safely

            if (student != null && !string.IsNullOrWhiteSpace(student.Name))
            {
                await Navigation.PushAsync(new LearnSessionPage(App.dbPath, studentId, student.Name));
            }
            else
            {
                await DisplayAlert("Error", "Selected student has no name saved in the database!", "OK");
            }
        }


        // Navigation to Reports Page
        private async void OnReportsTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
            await Navigation.PushAsync(new ReportsPage());
        }

        // Navigation to IAP Page
        private async void OnIAPTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Feature Unavailable", "This feature has not been implemented yet.", "OK");
            //await DisplayAlert("IAP", "Individualized Action Plan editing is not implemented yet.", "OK");
        }
        // Close user profiles button
        private void OnCloseProfile(object sender, EventArgs e)
        {
            StudentDetailsSection.IsVisible = false;
            LetsLearnTile.IsVisible = false;
            ReportsTile.IsVisible = false;
            IAPTile.IsVisible = false;
        }
        // deprecated thus method out, but not deleting it yet, i may need it later.
        private async void OnStudentSelected(int studentId, string studentName)
        {
            App.SetActiveStudent(studentId); // Set active student
            Debug.WriteLine($"Active Student Set: {studentId}, Name: {studentName}");

            await Navigation.PushAsync(new LearnSessionPage(App.dbPath, studentId, studentName)); // Pass student name to the test session
        }

    }
}
