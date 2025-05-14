using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using static Early_Dev_vs.src.DataModels;

namespace Early_Dev_vs.src
{
    public partial class LearnSessionPage : ContentPage
    {
        private readonly DbService? _dbService; // DB connection instance to grab new questions
        private Dictionary<string, bool> selectedImages = new Dictionary<string, bool>();
        private TestSessionRecord? _currentSessionRecord; // track the active test session for recording purposes
        private int _currentStudentId; // Store the current student ID

        public LearnSessionPage(string dbPath, int studentId, string studentName)
        {
            InitializeComponent();
            if (App.Database == null) // more and more null checks
            {
                Debug.WriteLine("ERROR: Database not initialized in App.xaml.cs!");
                return;
            }
            _dbService = App.Database; // assign and initialise the connection to the DB
            _currentStudentId = studentId; // Store student ID for later use

            studentNameLabel.Text = studentName;

            // Initialize tasks in sequence to ensure session is created before loading questions
            _ = InitializeAndLoadDataAsync(studentId);
        }

        // Method to properly sequence our async operations
        private async Task InitializeAndLoadDataAsync(int studentId)
        {
            // First initialize the session
            await InitializeSessionRecordAsync();

            // Then load student data
            await LoadStudentNameAsync(studentId);

            // Finally load the first question
            await LoadQuestion();
        }

        // Initialize the session record early to ensure it's available for all operations
        private async Task InitializeSessionRecordAsync()
        {
            if (App.Database == null)
            {
                Debug.WriteLine("ERROR: Database not initialized!");
                await DisplayAlert("Error", "Database connection is missing!", "OK");
                return;
            }

            // Create a new session record on page initialization
            _currentSessionRecord = new TestSessionRecord
            {
                StudentId = _currentStudentId,
                ResponseType = "Session Started",
                PromptUsed = "None",
                Timestamp = DateTime.UtcNow,
                CurrentQuestionId = -1 // Will be updated when first question loads
            };

            // Save the session record to get its ID
            await App.Database.SaveTestSessionAsync(_currentSessionRecord);
            Debug.WriteLine($"New session initialized with ID: {_currentSessionRecord.Id} for student {_currentStudentId}");
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

            // Reset image selections for new question
            selectedImages.Clear();

            if (_currentSessionRecord != null)
            {
                _currentSessionRecord.CurrentQuestionId = currentQuestion.Id;
                // Update the session record in the database with the new question ID
                if (App.Database != null)
                {
                    await App.Database.UpdateSessionRecordAsync(_currentSessionRecord);
                    Debug.WriteLine($"New Question Loaded! Question ID: {currentQuestion.Id} in Session ID: {_currentSessionRecord.Id}");
                }
                else
                {
                    Debug.WriteLine("ERROR: Cannot update session record - Database is null");
                }
            }
            else
            {
                Debug.WriteLine("Warning: No active session record found! Question ID could not be set.");
                // Try to initialize a session record if it doesn't exist
                await InitializeSessionRecordAsync();
            }
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
            await RecordResponse("Correct");
        }

        // Handle "Incorrect" Response
        private async void OnIncorrectTapped(object sender, EventArgs e)
        {
            await RecordResponse("Incorrect");
        }

        // Handle "No Response"
        private async void OnNoResponseTapped(object sender, EventArgs e)
        {
            await RecordResponse("No Response");
        }

        // Consolidated method to record responses and handle UI feedback
        private async Task RecordResponse(string responseType)
        {
            await SaveSessionData(responseType);
            await DisplayAlert("Response Recorded", $"Student response: {responseType}", "OK");
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
            await RecordRetry();
        }

        // Dedicated method to handle retry logic
        private async Task RecordRetry()
        {
            if (_currentSessionRecord == null || _currentSessionRecord.Id == 0)
            {
                await DisplayAlert("Error", "No active session to update.", "OK");
                return;
            }

            if (App.Database == null)
            {
                await DisplayAlert("Error", "Database is not initialized!", "OK");
                return;
            }

            string promptType = GetSelectedPromptType();

            // Prevent saving if no prompt type is selected
            if (promptType == "None")
            {
                await DisplayAlert("Selection Required", "Please select a prompt type before retrying the question.", "OK");
                return;
            }

            // Ensure we have a valid question ID stored in the session record
            if (_currentSessionRecord.CurrentQuestionId <= 0)
            {
                await DisplayAlert("Error", "No active question found for retry.", "OK");
                return;
            }

            // Get existing retry record for this question in the session
            var retryRecord = await App.Database.GetRetryRecordAsync(_currentSessionRecord.Id, _currentSessionRecord.CurrentQuestionId);

            if (retryRecord == null)
            {
                // Create a new retry entry if none exists
                retryRecord = new QuestionRetryRecord
                {
                    SessionId = _currentSessionRecord.Id,
                    QuestionId = _currentSessionRecord.CurrentQuestionId,
                    RetryCount = 1
                };

                await App.Database.AddRetryRecordAsync(retryRecord);
            }
            else
            {
                // Increment retry count for this question
                retryRecord.RetryCount++;
                await App.Database.UpdateRetryRecordAsync(retryRecord);
            }

            // Update the current session record with "Retry" response type
            _currentSessionRecord.ResponseType = "Retry";
            _currentSessionRecord.PromptUsed = promptType;
            _currentSessionRecord.Timestamp = DateTime.UtcNow;

            // Null check before accessing App.Database
            if (App.Database != null)
            {
                await App.Database.UpdateSessionRecordAsync(_currentSessionRecord);
            }
            else
            {
                Debug.WriteLine("ERROR: Cannot update session record - Database is null");
                await DisplayAlert("Error", "Database connection is missing!", "OK");
                return;
            }

            Debug.WriteLine($"Retry recorded! Session ID: {_currentSessionRecord.Id}, Question ID: {_currentSessionRecord.CurrentQuestionId}, Current Retry Count: {retryRecord.RetryCount}, Prompt Type: {promptType}");
            await DisplayAlert("Retry Recorded", $"Question retry count: {retryRecord.RetryCount}", "OK");
        }

        // Handle "End Test"
        private async void OnEndTestTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Test Completed", "Session data saved and test finalized.", "OK");
            ResetTestSession();
            App.ActiveStudentId = null;
            await Navigation.PopAsync(); // Return to previous page
        }

        // Save session data (Placeholder for database integration)
        private async Task SaveSessionData(string responseType)
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
                    StudentId = _currentStudentId,
                    ResponseType = responseType,
                    PromptUsed = promptType,
                    Timestamp = DateTime.UtcNow,
                    CurrentQuestionId = GetActiveQuestionId() // Store the active question ID
                };

                await App.Database.SaveTestSessionAsync(_currentSessionRecord); // Save new session record
                Debug.WriteLine($"Created new session record with ID: {_currentSessionRecord.Id}");
            }
            else
            {
                // Update existing session record
                _currentSessionRecord.ResponseType = responseType;
                _currentSessionRecord.PromptUsed = promptType;
                _currentSessionRecord.Timestamp = DateTime.UtcNow;

                // Null check before accessing App.Database
                if (App.Database != null)
                {
                    await App.Database.UpdateSessionRecordAsync(_currentSessionRecord);
                    Debug.WriteLine($"Updated session record ID: {_currentSessionRecord.Id}");
                }
                else
                {
                    Debug.WriteLine("ERROR: Cannot update session record - Database is null");
                    await DisplayAlert("Error", "Database connection is missing!", "OK");
                    return;
                }
            }

            // Debugging output for validation
            Debug.WriteLine($"Session data saved: Student {_currentStudentId}, Response: {responseType}, Prompt: {promptType}, Question ID: {_currentSessionRecord.CurrentQuestionId}");
        }

        private int GetActiveQuestionId()
        {
            // Ensure the current session has an active question
            if (_currentSessionRecord != null && _currentSessionRecord.CurrentQuestionId > 0)
            {
                return _currentSessionRecord.CurrentQuestionId; // Return stored question ID
            }

            Debug.WriteLine("Warning: No active question found! Defaulting to -1.");
            return -1; // Return a default value if no question is active
        }

        // move on to next question once this question is complete, or use this button to skip current question.
        private async void OnNextQuestionTapped(object sender, EventArgs e)
        {
            await LoadQuestion(); // Use the existing LoadQuestion method to get the next question
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

        // Handle Auto-Saves
        private async void OnAutoSaveTapped(object sender, EventArgs e)
        {
            await SaveSessionData("Auto-Save");
            await DisplayAlert("Auto-Save", "Session data saved successfully!", "OK");
        }

        // Clear the test session and reset the question list.
        private void ResetTestSession()
        {
            if (_dbService != null)
            {
                _dbService?.ResetTestSession();
                Debug.WriteLine("Test session has been reset. Questions will start fresh.");
            }
        }

    }
}