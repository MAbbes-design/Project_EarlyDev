using System;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using static Early_Dev_vs.src.DataModels;
using System.Diagnostics;
using System.Text;

namespace Early_Dev_vs.src
{
    public partial class ReportsPage : ContentPage
    {
        public ReportsPage()
        {
            InitializeComponent();
            LoadActiveStudent();
        }

        // Load Active Student details (set when a search is completed in StudentProfilesPage)
        private async void LoadActiveStudent()
        {
            // Assuming App.ActiveStudent is set from StudentProfilesPage via App.SetActiveStudent.
            StudentProfile? activeStudent = App.ActiveStudent;
            if (activeStudent == null && App.ActiveStudentId.HasValue && App.Database != null)
            {
                // Optionally load the student from the DB if only the ID is stored.
                activeStudent = await App.Database.GetStudentByIdAsync(App.ActiveStudentId.Value);
                App.ActiveStudent = activeStudent;
            }

            if (activeStudent != null)
            {
                ActiveStudentNameLabel.Text = $"Active Student: {activeStudent.Name}";
                ActiveStudentIdLabel.Text = $"ID: {activeStudent.Id}";
            }
            else
            {
                ActiveStudentNameLabel.Text = "No Active Student Selected";
                ActiveStudentIdLabel.Text = "";
            }
        }

        // Detailed Report Tile tap handler
        private async void OnDetailedReportTapped(object sender, EventArgs e)
        {
            Debug.WriteLine($"ActiveStudentId: {App.ActiveStudentId}");
            Debug.WriteLine($"ActiveStudent Name: {App.ActiveStudent?.Name}");

            // Ensure the active student and database are available.
            if (App.ActiveStudentId == null || App.Database == null)
            {
                await DisplayAlert("Error", "No active student selected or database not available.", "OK");
                return;
            }

            int activeStudentId = App.ActiveStudentId.Value;

            // Query test session records for the active student.
            List<TestSessionRecord> allTestSessions = await App.Database.GetAllTestSessionsAsync();
            List<TestSessionRecord> studentTestSessions = allTestSessions.Where(ts => ts.StudentId == activeStudentId).ToList();

            // Compute total questions, correct and incorrect answers.
            int totalQuestions = studentTestSessions.Count;
            int correctAnswers = studentTestSessions.Count(ts => ts.ResponseType != null &&ts.ResponseType.Equals("Correct", System.StringComparison.OrdinalIgnoreCase));
            int incorrectAnswers = studentTestSessions.Count(ts => ts.ResponseType != null &&ts.ResponseType.Equals("Incorrect", System.StringComparison.OrdinalIgnoreCase));

            // Group test session records by prompt type for a pie chart view.
            var promptBreakdown = studentTestSessions.GroupBy(ts => ts.PromptUsed).Select(group => new {PromptUsed = group.Key,Count = group.Count()}).ToList();

            // Get all retry records.
            List<QuestionRetryRecord> allRetryRecords = await App.Database.GetAllRetryRecordsAsync();

            // Filter retry records that belong to this student's test sessions.
            var studentRetryRecords = allRetryRecords.Where(rr => studentTestSessions.Any(ts => ts.Id == rr.SessionId)).ToList();

            // Group retry records by question to count the total retries per question.
            var retriesPerQuestion = studentRetryRecords.GroupBy(rr => rr.QuestionId).Select(g => new {QuestionId = g.Key,TotalRetries = g.Sum(rr => rr.RetryCount)}).ToList();

            // For retries by question type, join retry records with questions.
            List<QuestionModel> allQuestions = await App.Database.GetAllQuestionsAsync();
            var retriesByQuestionType = studentRetryRecords.Select(rr =>
            {
                var question = allQuestions.FirstOrDefault(q => q.Id == rr.QuestionId);
                return question?.AnswerType ?? "Unknown";
            }).GroupBy(answerType => answerType).Select(g => new {QuestionType = g.Key,TotalRetries = g.Sum(_ => 1)}).ToList();

            // Construct the report summary.
            var reportSummary = new StringBuilder();
            reportSummary.AppendLine("Student Detailed Report:");
            reportSummary.AppendLine($"Total Questions Answered: {totalQuestions}");
            reportSummary.AppendLine($"Answered Correctly: {correctAnswers}");
            reportSummary.AppendLine($"Answered Incorrectly: {incorrectAnswers}");
            reportSummary.AppendLine();
            reportSummary.AppendLine("Answers by Prompt Type:");
            foreach (var item in promptBreakdown)
            {
                string prompt = item.PromptUsed ?? "None";
                reportSummary.AppendLine($" - {prompt}: {item.Count}");
            }
            reportSummary.AppendLine();
            reportSummary.AppendLine("Retries per Question:");
            foreach (var r in retriesPerQuestion)
            {
                reportSummary.AppendLine($" - Question {r.QuestionId}: {r.TotalRetries} retries");
            }
            reportSummary.AppendLine();
            reportSummary.AppendLine("Retries by Question Type:");
            foreach (var r in retriesByQuestionType)
            {
                reportSummary.AppendLine($" - {r.QuestionType}: {r.TotalRetries} retries");
            }

            // Display the summary - this will be used to create charts or graphs in the future.
            await DisplayAlert("Detailed Report", reportSummary.ToString(), "OK");
        }


        // Filtering Options Tile tap handler
        private void OnFilteringOptionsTapped(object sender, EventArgs e)
        {
            // Toggle the visibility of the filter panel.
            FilterPanel.IsVisible = !FilterPanel.IsVisible;
        }

        // Progress Graphs Tile tap handler
        private async void OnProgressGraphsTapped(object sender, EventArgs e)
        {
            // Prepare and display graph data (using a charting library, etc.)
            await DisplayAlert("Progress Graphs", "Displaying student progress trends...", "OK");
        }

        // More Options Tile tap handler
        private async void OnMoreOptionsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("More Options", "More reporting options coming soon.", "OK");
        }

        // Apply Filter Button tap handler
        private async void OnApplyFilterClicked(object sender, EventArgs e)
        {
            var selectedType = QuestionTypePicker.SelectedItem?.ToString() ?? "All";
            // Use the selected question type to filter report data from the database.
            await DisplayAlert("Filter Applied", $"Filtering reports by: {selectedType}", "OK");
        }
    }
}
