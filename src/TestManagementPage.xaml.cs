using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using static Early_Dev_vs.src.DataModels;

namespace Early_Dev_vs.src
{
    public partial class TestManagementPage : ContentPage
    {
        private readonly DbService _dbService; // database instance to interact with the DB
        private ObservableCollection<QuestionModel> _questions; // collection of questions that allows for automatic updates of the UI with a list of our questions.
        private ObservableCollection<string> _selectedImages; // collection that allows the tracking of selected images for any specific question.
        private QuestionModel? _currentEditingQuestion; // holds the data for the question for which the edit button is clicked.
        private bool _isEditMode = false; // need this to avoid errors, it tracks whether I am in edit mode or not for any question.

        // constructor
        public TestManagementPage(DbService dbService)
        {
            // initialisers for all components needed in this page. 
            InitializeComponent();
            _dbService = dbService;
            _questions = new ObservableCollection<QuestionModel>();
            _selectedImages = new ObservableCollection<string>();

            // Set up collection views
            QuestionsCollectionView.ItemsSource = _questions;
            SelectedImagesCollection.ItemsSource = _selectedImages;

            // Load questions on page appearance
            LoadQuestions();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing(); // ensures the original clean version of this method is called everytime this method is invoked.
            LoadQuestions(); // loads all questions from the Db to populate the collection view.
        }

        // Loads all saved questions from the database and updates the UI.
        private async void LoadQuestions()
        {
                var allQuestions = await _dbService.GetAllQuestionsAsync(); // grab all the questions from the DB
                _questions.Clear(); // clear the page of any old questions.
                foreach (var question in allQuestions) // loop through all the questions and add them to the collection
            {
                    _questions.Add(question); // update the UI with each question that is found in the DB
                }
        }

        // Resets the form fields.
        private void ResetForm()
        {
            QuestionTypePicker.SelectedIndex = -1; // makes the question type blank
            QuestionTextEditor.Text = string.Empty; // clears the question text box.
            CorrectAnswerEntry.Text = string.Empty; // clears the correct answer text box.
            _selectedImages.Clear(); // clears images from the collection which resets it so I can start again with a new form
            _isEditMode = false; // sets edit mode back to off.
            _currentEditingQuestion = null; // resets the reference to the current question being edited.
        }

        // Handles saving or updating a question when the Save button is tapped.
        private async void OnSaveQuestionClicked(object sender, EventArgs e)
        {
            // validates that question text is not empty.
            if (string.IsNullOrWhiteSpace(QuestionTextEditor.Text))
            {
                await DisplayAlert("Validation Error", "Question text is required.", "OK");
                return;
            }
            // validates that the question type is picked.
            if (QuestionTypePicker.SelectedIndex == -1)
            {
                await DisplayAlert("Validation Error", "Please select a question type.", "OK");
                return;
            }
            // validates that the correct answer is not empty.
            if (string.IsNullOrWhiteSpace(CorrectAnswerEntry.Text))
            {
                await DisplayAlert("Validation Error", "Correct answer is required.", "OK");
                return;
            }
            // validates that the question type is selected.
            if (QuestionTypePicker.SelectedItem == null)
            {
                await DisplayAlert("Validation Error", "Please select a question type.", "OK");
                return;
            }

            if (_isEditMode && _currentEditingQuestion != null)
            {
                // Update existing question
                _currentEditingQuestion.QuestionText = QuestionTextEditor.Text;
                _currentEditingQuestion.AnswerType = QuestionTypePicker.SelectedItem.ToString();
                _currentEditingQuestion.CorrectAnswer = CorrectAnswerEntry.Text;
                _currentEditingQuestion.ImageSources = new List<string>(_selectedImages); // storing images in a list

                // Update the question in the database  
                await _dbService.UpdateQuestionAsync(_currentEditingQuestion);
                await DisplayAlert("Success", "Question updated successfully!", "OK");
            }
            else
            {
            // Create new question (so we are not in edit mode, this is a brand new question)
            // Create a new question object for a new entry.
                var newQuestion = new QuestionModel
                {
                    QuestionText = QuestionTextEditor.Text,
                    AnswerType = QuestionTypePicker.SelectedItem?.ToString() ?? "Multiple Choice",
                    CorrectAnswer = CorrectAnswerEntry.Text,
                    ImageSources = new List<string>(_selectedImages)
                };

                // Add the new question to the database
                await _dbService.AddQuestionAsync(newQuestion);
                await DisplayAlert("Success", "Question saved successfully!", "OK");
            }

            // Reset form and reload questions
            ResetForm();
            LoadQuestions();
        }

        // Handles the Cancel button click event to reset the form.
        private void OnCancelClicked(object sender, EventArgs e)
        {
            ResetForm();
        }

        // Handles image selection when the Upload Image button is tapped or clicked.
        private async void OnUploadImageClicked(object sender, EventArgs e)
        {
            // Check if the platform supports file picking
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                //{ DevicePlatform.iOS, new[] { "public.image" } }, // IOS support, but I can't build for it at the moment.
                { DevicePlatform.Android, new[] { "image/*" } }, // android support.
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif" } }, // windows support
                //{ DevicePlatform.MacCatalyst, new[] { "public.image" } } // who builds MAUI apps for macs.
            });

            // Sets up file picker options and supported file types.
            var options = new PickOptions
            {
                PickerTitle = "Please select an image",
                FileTypes = customFileType, // picks the file format from the options above depending on your platform.
            };

            // Open the file picker and get the selected file once a user selects it. 
            var result = await FilePicker.PickAsync(options);
            if (result != null) // yes, its another null check.
            {
                // Add the file path to our collection
                _selectedImages.Add(result.FullPath);
            }
        }

        // Gives support to allow handling of image removal from the selected images list for a question.
        private void OnRemoveImageClicked(object sender, EventArgs e)
        {
            // Check if the sender is a button and has a CommandParameter, aka finds the image path that was selected and lets you delete it.
            if (sender is Button button && button.CommandParameter is string imagePath)
            {
                //deletes the selected image
                _selectedImages.Remove(imagePath);
            }
        }

        // a refresh button to reload the questions from the database.
        private void OnRefreshQuestionsClicked(object sender, EventArgs e)
        {
            LoadQuestions(); // reloads the questions from the database.
        }

        // handles question editing, and is invoked whent he edit button is pressed.
        private async void OnEditQuestionClicked(object sender, EventArgs e)
        {
            // Validates that the button was pressed and grabs the associated question.
            if (sender is Button button && button.CommandParameter is int questionId)
            {
                // Find the question to edit, using the provided questionId.
                var questionToEdit = _questions.FirstOrDefault(q => q.Id == questionId);
                if (questionToEdit != null) // more null checks that break my code constantly. these are a headache
                {
                    // Set the current editing question
                    _currentEditingQuestion = questionToEdit;
                    // turns on edit mode
                    _isEditMode = true;

                    // Populate form with question data
                    QuestionTextEditor.Text = questionToEdit.QuestionText;
                    CorrectAnswerEntry.Text = questionToEdit.CorrectAnswer;

                    // Set the picker to the correct index
                    for (int i = 0; i < QuestionTypePicker.Items.Count; i++)
                    {
                        if (QuestionTypePicker.Items[i] == questionToEdit.AnswerType)
                        {
                            QuestionTypePicker.SelectedIndex = i;
                            break;
                        }
                    }

                    // Load images that belong to the question we are editing.
                    _selectedImages.Clear();
                    foreach (var imagePath in questionToEdit.ImageSources)
                    {
                        _selectedImages.Add(imagePath);
                    }

                    // Scroll to the top of the page when the edit button is pressed, essentially sending us to the top of the form.
                    await pageScrollView.ScrollToAsync(0, 0, true);
                }
            }
        }

        // handles the delete question process. When the delete button is pressed for a question it deletes it from the system (db)
        private async void OnDeleteQuestionClicked(object sender, EventArgs e)
        {
            // Validates that the button was pressed and grabs the associated question.
            if (sender is Button button && button.CommandParameter is int questionId)
            {
                // Confirm deletion with the user, so sends up a popup window to confirm it.
                bool confirm = await DisplayAlert("Confirm Delete",
                    "Are you sure you want to delete this question? This action cannot be undone.",
                    "Yes, Delete", "Cancel");
                // if the user confirms the deletion, it will delete the question.
                if (confirm)
                {
                    // Find the question to delete using the provided questionId.
                    var questionToDelete = _questions.FirstOrDefault(q => q.Id == questionId);
                    if (questionToDelete != null) // its another null check my friends.
                    {
                        // Delete the question from the database, handles the actual delete.
                        await _dbService.DeleteQuestionAsync(questionToDelete);
                        // Remove the question from the collection.
                        _questions.Remove(questionToDelete);
                        // Reset the form after deletion. Also notifies the system user that the question was deleted successfully.
                        await DisplayAlert("Success", "Question deleted successfully!", "OK");
                    }
                }
            }
        }

        // sends the user back to the dashboard page when the back button is pressed.
        private async void OnBackToDashboardClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // closes this page and returns to the previous page.
        }
    }
}