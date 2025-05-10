using System;
using System.Linq;
using System.Text;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static Early_Dev_vs.src.DataModels;
using System.Diagnostics;

namespace Early_Dev_vs.src
{
    public class DbService
    {
        private readonly SQLiteAsyncConnection _database;

        public DbService(string dbPath)
        {
            Debug.WriteLine($"Initializing Database at Path: {dbPath}");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<StudentProfile>().Wait();  // Initialize StudentProfiles table
            _database.CreateTableAsync<QuestionModel>().Wait();  // Initialize Questions table
            _database.CreateTableAsync<QuestionRetryRecord>().Wait(); // Ensure retry records table exists
        }

        // ===================== Student CRUD =====================

        // Add a new student profile
        public Task<int> AddStudentAsync(StudentProfile student)
        {
            return _database.InsertAsync(student);
        }

        // list all students
        public Task<List<StudentProfile>> GetAllStudentsAsync()
        {
            return _database.Table<StudentProfile>().ToListAsync();
        }

        // Get a single students profile by ID
        public Task<StudentProfile> GetStudentByIdAsync(int studentId)
        {
            return _database.Table<StudentProfile>().Where(s => s.Id == studentId).FirstOrDefaultAsync();
        }

        // Update student profile
        public Task<int> UpdateStudentAsync(StudentProfile student)
        {
            return _database.UpdateAsync(student);
        }

        // delete a student profile, and for goodness sake dont forget to add a delete button to the app for this
        public Task<int> DeleteStudentAsync(StudentProfile student)
        {
            return _database.DeleteAsync(student);
        }

        // ===================== Question CRUD =====================

        // Create a new test question
        public async Task<int> AddQuestionAsync(QuestionModel question)
        {
            question.ImageSourcesString = string.Join(",", question.ImageSources); // Ensure string is updated
            return await _database.InsertAsync(question);
        }

        // Get all stored questions
        public Task<List<QuestionModel>> GetAllQuestionsAsync()
        {
            return _database.Table<QuestionModel>().ToListAsync();
        }

        // need this to get a question by ID, to allow me to search for a random question to show but not repeat in the same session
        private HashSet<int> _askedQuestionIds = new HashSet<int>();
        // Retrieve a random question for test sessions
        public async Task<QuestionModel> GetRandomQuestionAsync()
        {
            var questions = await _database.Table<QuestionModel>().ToListAsync();

            // Ensure there are available questions
            var availableQuestions = questions.Where(q => !_askedQuestionIds.Contains(q.Id)).ToList();
            if (!availableQuestions.Any()) return new QuestionModel
            {
                QuestionText = "You have reached the end of this test, congratulations! please press End Test to save your progress",
                AnswerType = "Default",
                CorrectAnswer = "",
                ImageSources = new List<string>()
            };

            // Select a random question
            var selectedQuestion = availableQuestions[new Random().Next(availableQuestions.Count)];
            _askedQuestionIds.Add(selectedQuestion.Id); // Mark as asked

            // Convert ImageSourcesString to list
            selectedQuestion.ImageSources = selectedQuestion.ImageSourcesString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            return selectedQuestion;
        }

        // Update an existing test question
        public async Task<int> UpdateQuestionAsync(QuestionModel question)
        {
            return await _database.UpdateAsync(question);
        }

        // Delete a test question
        public Task<int> DeleteQuestionAsync(QuestionModel question)
        {
            return _database.DeleteAsync(question);
        }

        // Reset function for the test session, helps to clear the asked questions list.
        public void ResetTestSession()
        {
            _askedQuestionIds.Clear(); // Clears the list of asked questions for the next test session
            Debug.WriteLine("Test session reset: Questions will start fresh.");
        }

        // ===================== Test Session Management =====================

        // Save a new test session record
        public async Task SaveTestSessionAsync(TestSessionRecord sessionRecord)
        {
            await _database.CreateTableAsync<TestSessionRecord>(); //  Ensure table exists, OR creates a new table for this data
            await _database.InsertAsync(sessionRecord); //  Save session record to the database
        }

        // Retrieve all test sessions
        public Task<List<TestSessionRecord>> GetAllTestSessionsAsync()
        {
            return _database.Table<TestSessionRecord>().ToListAsync();
        }

        // Update an existing test session record (e.g., retry count updates)
        public async Task<int> UpdateSessionRecordAsync(TestSessionRecord sessionRecord)
        {
            return await _database.UpdateAsync(sessionRecord); // Save updated session record, including retry count
        }

        // ===================== Question Retry Management =====================

        // Add a retry record for a question in a test session
        public Task<int> AddRetryRecordAsync(QuestionRetryRecord retryRecord)
        {
            return _database.InsertAsync(retryRecord);
        }

        // Updates an existing retry record for a question in a test session.
        public Task<int> UpdateRetryRecordAsync(QuestionRetryRecord retryRecord)
        {
            return _database.UpdateAsync(retryRecord);
        }

        // Grabs the data for specific question retries.
        public Task<QuestionRetryRecord> GetRetryRecordAsync(int sessionId, int questionId)
        {
            return _database.Table<QuestionRetryRecord>()
                .Where(r => r.SessionId == sessionId && r.QuestionId == questionId)
                .FirstOrDefaultAsync();
        }
    }
}
