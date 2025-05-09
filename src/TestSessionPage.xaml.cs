using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using static Early_Dev_vs.src.DataModels;

namespace Early_Dev_vs.src
{
    public partial class TestSessionPage : ContentPage
    {
        private readonly DbService? _dbService; // DB connection instance to grab new questions
        private Dictionary<string, bool> selectedImages = new Dictionary<string, bool>();
        private TestSessionRecord? _currentSessionRecord; // track the active test session for recording purposes

        public TestSessionPage(string dbPath, int studentId, string studentName)
        {
            InitializeComponent();
            if (App.Database == null) // more and more null checks
            {
                Debug.WriteLine("ERROR: Database not initialized in App.xaml.cs!");
                return;
            }
            _dbService = App.Database; // assign and initialise the connection to the DB

            studentNameLabel.Text = studentName;

            _ = LoadStudentNameAsync(studentId); // method to fetch student
            _ = LoadQuestion(); // load a new question
        }


        // Load question dynamically from the database
        private async Task LoadQuestion()
        {
            if (_dbService == null) // more null checks.
            {
                Debug.WriteLine("ERROR: Database service not initialized!");
                await DisplayAlert("Error", "Database connection is missing!", "OK");
                return;
            }
            QuestionModel currentQuestion = await _dbService.GetRandomQuestionAsync(); // Grab next question from DB.

            if (currentQuestion == null) // why does MAUI need so many null checks to work
            {
                await DisplayAlert("No Questions", "No questions available in the database.", "OK");
                return;
            }

            LessonQuestionLabel.Text = currentQuestion.QuestionText; // Set question text
            ImageCollectionView.ItemsSource = currentQuestion.ImageSources; // Bind images
        }

        // Handle Tapping on an Image
        private void OnImageTapped(object sender, EventArgs e)
        {
            var tappedFrame = sender as Frame;
            if (tappedFrame == null) return; // its a null check

            var tappedImageSource = tappedFrame.BindingContext as string; // Get image path from BindingContext
            if (string.IsNullOrEmpty(tappedImageSource)) return; // yep a null check

            // Toggle selection
            if (selectedImages.ContainsKey(tappedImageSource)) // allows a tap of the image boxes to select them
            {
                selectedImages[tappedImageSource] = !selectedImages[tappedImageSource];
            }
            else
            {
                selectedImages[tappedImageSource] = true;
            }

            // Change background color to indicate selection - basically a highlighter
            tappedFrame.BackgroundColor = selectedImages[tappedImageSource] ? Colors.LightBlue : Colors.LightGray;
        }

        // Need to record these against the student ID that is linked to the session, on correct, on incorrect, on no response, prompt type, etc.
        // Handle "Correct" Response
        private async void OnCorrectTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Response Recorded", "Student response:  Correct", "OK");
            SaveSessionData("Correct");
        }

        // Handle "Incorrect" Response
        private async void OnIncorrectTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Response Recorded", "Student response:  Incorrect", "OK");
            SaveSessionData("Incorrect");
        }

        // Handle "No Response"
        private async void OnNoResponseTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Response Recorded", "Student response:  No Response", "OK");
            SaveSessionData("No Response");
        }

        // Get Selected Prompt Type
        private string GetSelectedPromptType()
        {
            if (PhysicalPrompt.IsChecked) return "Physical";
            if (VerbalPrompt.IsChecked) return "Verbal";
            if (GesturePrompt.IsChecked) return "Gesture";
            if (PartialPhysicalPrompt.IsChecked) return "Partial Physical";
            if (IndependentPrompt.IsChecked) return "Independent";
            return "None";
        }

        // Handle "Retry Question"
        private async void OnRetryTapped(object sender, EventArgs e)
        {
            if (_currentSessionRecord == null) // why does this cause so many errors, there IS data in there
            {
                await DisplayAlert("Error", "No active session to update.", "OK");
                return;
            }

            // Ensure App.Database is initialized before updating
            if (App.Database == null) // more null checks
            {
                await DisplayAlert("Error", "Database is not initialized!", "OK");
                return;
            }

            _currentSessionRecord.RetryCount++;
            await App.Database.UpdateSessionRecordAsync(_currentSessionRecord); // Safe update
        }


        // Handle "End Test"
        private async void OnEndTestTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Test Completed", "Session data saved and test finalized.", "OK");
            App.ActiveStudentId = null;
            await Navigation.PopAsync(); // Return to previous page
        }

        // Save session data (Placeholder for database integration)
        private async void SaveSessionData(string responseType)
        {
            string promptType = GetSelectedPromptType();

            // Prevent saving if no prompt type is selected
            if (promptType == "None")
            {
                await DisplayAlert("Selection Required", "Please select a prompt type before saving the response.", "OK");
                return; // Stop execution
            }

            // Ensure database is initialized
            if (App.Database == null)
            {
                await DisplayAlert("Error", "Database is not initialized!", "OK");
                return;
            }

            // If no session record exists, create a new one
            if (_currentSessionRecord == null)
            {
                _currentSessionRecord = new TestSessionRecord
                {
                    StudentId = App.ActiveStudentId ?? -1, // Assign active student or default ID
                    ResponseType = responseType,
                    PromptUsed = promptType,
                    Timestamp = DateTime.UtcNow,
                    RetryCount = responseType == "Retry" ? 1 : 0 // Initialize retry count if it's a retry
                };

                await App.Database.SaveTestSessionAsync(_currentSessionRecord); // Save new session record
            }
            else if (responseType == "Retry") // If retry, update retry count
            {
                _currentSessionRecord.RetryCount++; // Increment retry count
                await App.Database.UpdateSessionRecordAsync(_currentSessionRecord); // Save updated record
            }

            Debug.WriteLine($"Session saved: Student {App.ActiveStudentId}, Response: {responseType}, Prompt: {promptType}, Retries: {_currentSessionRecord?.RetryCount}");
        }


        // move on to next question once this question is complete, or use this button to skip current question.
        private async void OnNextQuestionTapped(object sender, EventArgs e)
        {
            if (_dbService == null)
            {
                Debug.WriteLine("ERROR: Database service not initialized!");
                await DisplayAlert("Error", "Database connection is missing!", "OK");
                return;
            }

            QuestionModel nextQuestion = await _dbService.GetRandomQuestionAsync(); // Fetch next question

            if (nextQuestion == null)
            {
                await DisplayAlert("No More Questions", "There are no more questions available.", "OK");
                return;
            }

            LessonQuestionLabel.Text = nextQuestion.QuestionText;
            ImageCollectionView.ItemsSource = nextQuestion.ImageSources;
            selectedImages.Clear(); // Reset selections
        }
        // Load student name from DB
        private async Task LoadStudentNameAsync(int studentId)
        {
            if (_dbService == null)
            {
                Debug.WriteLine("ERROR: Database service not initialized!");
                await DisplayAlert("Error", "Database connection is missing!", "OK");
                return;
            }
            var student = await _dbService.GetStudentByIdAsync(studentId); // Ensure this is an async DB method
            if (student != null)
            {
                studentNameLabel.Text = student.Name;
            }
            else
            {
                await DisplayAlert("Error", "Student not found!", "OK");
                //await Navigation.PopAsync();
            }
        }
    }
    public class QuestionImageModel
    {
        public string? ImageSource { get; set; }
    }
}
