using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Early_Dev_vs.src;
using static Early_Dev_vs.src.DataModels;
using Early_Dev_vs.src.LogicAdapters;

namespace Early_Dev_vs.src.LogicAdapters
{
    // This class extracts logic from ReportsPage so it can be tested separately from UI elements in MAUI.
    public class ReportsPageLogicAdapter
    {
        private readonly DbService _dbService;
        private int _activeStudentId;
        private StudentProfile _activeStudent;

        public ReportsPageLogicAdapter(DbService dbService, int studentId)
        {
            _dbService = dbService;
            _activeStudentId = studentId;
        }

        // ===================== Extracted Methods from ReportsPage =====================

        // Loads the active student profile from the database.
        public async Task<StudentProfile> LoadActiveStudentAsync()
        {
            _activeStudent = await _dbService.GetStudentByIdAsync(_activeStudentId);
            return _activeStudent;
        }

        // Retrieves all test sessions for the active student.
        public async Task<List<TestSessionRecord>> GetStudentTestSessionsAsync()
        {
            var allTestSessions = await _dbService.GetAllTestSessionsAsync();
            return allTestSessions.Where(ts => ts.StudentId == _activeStudentId).ToList();
        }

        // Counts the total number of answered questions.
        public async Task<int> ComputeTotalQuestionsAsync()
        {
            var sessions = await GetStudentTestSessionsAsync();
            return sessions.Count;
        }

        // Counts the number of correctly answered questions.
        public async Task<int> ComputeCorrectAnswersAsync()
        {
            var sessions = await GetStudentTestSessionsAsync();
            return sessions.Count(ts => ts.ResponseType != null && ts.ResponseType.Equals("Correct", StringComparison.OrdinalIgnoreCase));
        }

        // Counts the number of incorrectly answered questions.
        public async Task<int> ComputeIncorrectAnswersAsync()
        {
            var sessions = await GetStudentTestSessionsAsync();
            return sessions.Count(ts => ts.ResponseType != null && ts.ResponseType.Equals("Incorrect", StringComparison.OrdinalIgnoreCase));
        }

        // Retrieves all retry records associated with the student's test sessions.
        public async Task<List<QuestionRetryRecord>> GetRetryRecordsAsync()
        {
            var allRetryRecords = await _dbService.GetAllRetryRecordsAsync();
            var studentSessions = await GetStudentTestSessionsAsync();
            return allRetryRecords.Where(rr => studentSessions.Any(ts => ts.Id == rr.SessionId)).ToList();
        }

        // Fetches all questions from the database.
        public async Task<List<QuestionModel>> GetAllQuestionsAsync()
        {
            return await _dbService.GetAllQuestionsAsync();
        }

        // Groups retry records by question type for analytical breakdown.
        public async Task<List<string>> GetRetriesByQuestionTypeAsync()
        {
            var retryRecords = await GetRetryRecordsAsync();
            var allQuestions = await GetAllQuestionsAsync();

            return retryRecords
                .Select(rr =>
                {
                    var question = allQuestions.FirstOrDefault(q => q.Id == rr.QuestionId);
                    return question?.AnswerType ?? "Unknown";
                })
                .ToList();
        }

        // ===================== STATE ACCESSORS =====================

        //public int GetActiveStudentId() => _activeStudentId;
        //public StudentProfile GetActiveStudent() => _activeStudent;
    }
}

namespace EarlyDevTests
{
    // This unit test suite verifies the functionality of ReportsPageLogicAdapter using mock objects.
    public class ReportsPage_UnitTests
    {
        private readonly Mock<DbService> _mockDbService;
        private readonly ReportsPageLogicAdapter _adapter;

        public ReportsPage_UnitTests()
        {
            _mockDbService = new Mock<DbService>("test.db");
            _adapter = new ReportsPageLogicAdapter(_mockDbService.Object, 1);
        }

        // ===================== STUDENT LOADING TEST =====================

        [Fact]
        public async Task LoadActiveStudent_ShouldRetrieveStudentById()
        {
            var student = new StudentProfile { Id = 1, Name = "Test Student" };
            _mockDbService.Setup(db => db.GetStudentByIdAsync(1)).ReturnsAsync(student);

            var retrievedStudent = await _adapter.LoadActiveStudentAsync();

            Assert.NotNull(retrievedStudent);
            Assert.Equal("Test Student", retrievedStudent.Name);
        }

        // ===================== REPORT GENERATION TEST =====================

        [Fact]
        public async Task ComputeCorrectAnswers_ShouldCountCorrectResponses()
        {
            var testSessions = new List<TestSessionRecord>
            {
                new TestSessionRecord { StudentId = 1, ResponseType = "Correct" },
                new TestSessionRecord { StudentId = 1, ResponseType = "Incorrect" },
                new TestSessionRecord { StudentId = 1, ResponseType = "Correct" }
            };
            _mockDbService.Setup(db => db.GetAllTestSessionsAsync()).ReturnsAsync(testSessions);

            var correctAnswers = await _adapter.ComputeCorrectAnswersAsync();

            Assert.Equal(2, correctAnswers);
        }

        // ===================== RETRY RECORD TEST =====================

        [Fact]
        public async Task GetRetryRecords_ShouldFilterRetriesForStudent()
        {
            var testSessions = new List<TestSessionRecord>
            {
                new TestSessionRecord { Id = 100, StudentId = 1 },
                new TestSessionRecord { Id = 101, StudentId = 1 }
            };

            var retryRecords = new List<QuestionRetryRecord>
            {
                new QuestionRetryRecord { SessionId = 100, QuestionId = 10, RetryCount = 2 },
                new QuestionRetryRecord { SessionId = 101, QuestionId = 15, RetryCount = 3 }
            };

            _mockDbService.Setup(db => db.GetAllTestSessionsAsync()).ReturnsAsync(testSessions);
            _mockDbService.Setup(db => db.GetAllRetryRecordsAsync()).ReturnsAsync(retryRecords);

            var studentRetryRecords = await _adapter.GetRetryRecordsAsync();

            Assert.Equal(2, studentRetryRecords.Count);
        }

        // ===================== REPORT FILTER TEST =====================

        [Fact]
        public async Task GetRetriesByQuestionType_ShouldMatchQuestionType()
        {
            var testSessions = new List<TestSessionRecord>
            {
                new TestSessionRecord { Id = 100, StudentId = 1 },
                new TestSessionRecord { Id = 101, StudentId = 1 }
            };

                    var retryRecords = new List<QuestionRetryRecord>
            {
                new QuestionRetryRecord { SessionId = 100, QuestionId = 10 },
                new QuestionRetryRecord { SessionId = 101, QuestionId = 15 }
            };

                    var questions = new List<QuestionModel>
            {
                new QuestionModel { Id = 10, AnswerType = "Multiple Choice" },
                new QuestionModel { Id = 15, AnswerType = "True/False" }
            };

            _mockDbService.Setup(db => db.GetAllTestSessionsAsync()).ReturnsAsync(testSessions);
            _mockDbService.Setup(db => db.GetAllRetryRecordsAsync()).ReturnsAsync(retryRecords);
            _mockDbService.Setup(db => db.GetAllQuestionsAsync()).ReturnsAsync(questions);

            var retriesByQuestionType = await _adapter.GetRetriesByQuestionTypeAsync();

            Assert.NotNull(retriesByQuestionType);
            Assert.NotEmpty(retriesByQuestionType);
            Assert.Contains("Multiple Choice", retriesByQuestionType);
            Assert.Contains("True/False", retriesByQuestionType);
        }

        // ===================== GRAPH FUNCTION TEST =====================

        [Fact]
        public async Task ComputeIncorrectAnswers_ShouldCountIncorrectResponses()
        {
            var testSessions = new List<TestSessionRecord>
            {
                new TestSessionRecord { StudentId = 1, ResponseType = "Correct" },
                new TestSessionRecord { StudentId = 1, ResponseType = "Incorrect" },
                new TestSessionRecord { StudentId = 1, ResponseType = "Incorrect" }
            };
            _mockDbService.Setup(db => db.GetAllTestSessionsAsync()).ReturnsAsync(testSessions);

            var incorrectAnswers = await _adapter.ComputeIncorrectAnswersAsync();

            Assert.Equal(2, incorrectAnswers);
        }
    }
}
