namespace EarlyDevTests
{
    using Xunit;
    using System.Threading.Tasks;
    using Early_Dev_vs.src;
    using System.Collections.Generic;
    using static Early_Dev_vs.src.DataModels;

    public class DbServiceUnitTests
    {
        private readonly DbService _TestDb;

        // ===================== Setup =====================

        // Initializes an in-memory SQLite database for isolated testing
        public DbServiceUnitTests()
        {
            _TestDb = new DbService(":memory:");
        }

        // Clears all tables before running each test
        public async Task ResetDatabaseAsync()
        {
            await _TestDb.ResetDatabaseAsync();
        }

        // ===================== Student CRUD Tests =====================

        [Fact]
        public async Task AddStudent_ShouldInsertRecord()
        {
            // Test that a new student profile can be inserted into the in-memory database
            await ResetDatabaseAsync();
            var student = new StudentProfile { Name = "Cabbage", Age = 5 };
            await _TestDb.AddStudentAsync(student);
            var retrievedStudent = await _TestDb.GetStudentByIdAsync(student.Id);
            Assert.NotNull(retrievedStudent);
            Assert.Equal("Cabbage", retrievedStudent.Name);
        }

        [Fact]
        public async Task GetStudentById_ShouldReturnCorrectStudent()
        {
            // Test that retrieving a student by ID returns the correct student
            await ResetDatabaseAsync();
            var student = new StudentProfile { Name = "Caramel", Age = 2 };
            await _TestDb.AddStudentAsync(student);
            var retrievedStudent = await _TestDb.GetStudentByIdAsync(student.Id);
            Assert.NotNull(retrievedStudent);
            Assert.Equal("Caramel", retrievedStudent.Name);
        }

        [Fact]
        public async Task UpdateStudent_ShouldModifyRecord()
        {
            // Test that updating a student's profile correctly modifies the stored record
            await ResetDatabaseAsync();
            var student = new StudentProfile { Name = "Kakarot", Age = 6 };
            await _TestDb.AddStudentAsync(student);
            student.Name = "Goku";
            await _TestDb.UpdateStudentAsync(student);
            var updatedStudent = await _TestDb.GetStudentByIdAsync(student.Id);
            Assert.Equal("Goku", updatedStudent.Name);
        }

        [Fact]
        public async Task DeleteStudent_ShouldRemoveRecord()
        {
            // Test that deleting a student profile successfully removes it from the database
            await ResetDatabaseAsync();
            var student = new StudentProfile { Name = "Vegeta", Age = 7 };
            await _TestDb.AddStudentAsync(student);
            await _TestDb.DeleteStudentAsync(student);
            var deletedStudent = await _TestDb.GetStudentByIdAsync(student.Id);
            Assert.Null(deletedStudent);
        }

        // ===================== Question CRUD Tests =====================

        [Fact]
        public async Task AddQuestion_ShouldInsertRecord()
        {
            // Test that a new question can be inserted into the database
            await ResetDatabaseAsync();
            var question = new QuestionModel { QuestionText = "What is 2+2?", AnswerType = "Multiple Choice", CorrectAnswer = "4" };
            await _TestDb.AddQuestionAsync(question);
            var retrievedQuestion = await _TestDb.GetAllQuestionsAsync();
            Assert.NotEmpty(retrievedQuestion);
        }

        [Fact]
        public async Task GetRandomQuestion_ShouldReturnUniqueQuestions()
        {
            // Test that `GetRandomQuestionAsync` returns unique questions without repetition
            await ResetDatabaseAsync();
            await _TestDb.AddQuestionAsync(new QuestionModel { QuestionText = "Q1" });
            await _TestDb.AddQuestionAsync(new QuestionModel { QuestionText = "Q2" });
            var firstQuestion = await _TestDb.GetRandomQuestionAsync();
            var secondQuestion = await _TestDb.GetRandomQuestionAsync();
            Assert.NotEqual(firstQuestion.Id, secondQuestion.Id);
        }

        // ===================== Test Session Tests =====================

        [Fact]
        public async Task SaveTestSession_ShouldStoreSessionRecord()
        {
            // Test that a new test session record is successfully stored in the database
            await ResetDatabaseAsync();
            var sessionRecord = new TestSessionRecord { StudentId = 1, ResponseType = "Correct", Timestamp = System.DateTime.Now };
            await _TestDb.SaveTestSessionAsync(sessionRecord);
            var sessions = await _TestDb.GetAllTestSessionsAsync();
            Assert.NotEmpty(sessions);
        }

        [Fact]
        public async Task UpdateSessionRecord_ShouldModifySession()
        {
            // Test that modifying an existing test session record updates correctly
            await ResetDatabaseAsync();
            var sessionRecord = new TestSessionRecord { StudentId = 1, ResponseType = "Incorrect", Timestamp = System.DateTime.Now };
            await _TestDb.SaveTestSessionAsync(sessionRecord);
            sessionRecord.ResponseType = "Correct";
            await _TestDb.UpdateSessionRecordAsync(sessionRecord);
            var updatedSession = await _TestDb.GetAllTestSessionsAsync();
            Assert.Contains(updatedSession, s => s.ResponseType == "Correct");
        }

        [Fact]
        // Test that a saved test session remains stored and can be retrieved later.
        public async Task SaveTestSession_ShouldPersistAcrossRetrievals()
        {
            await ResetDatabaseAsync();
            var session = new TestSessionRecord { StudentId = 1, ResponseType = "Correct", Timestamp = System.DateTime.Now };
            await _TestDb.SaveTestSessionAsync(session);

            var retrievedSessions = await _TestDb.GetAllTestSessionsAsync();
            Assert.Contains(retrievedSessions, s => s.StudentId == session.StudentId);
        }

        // ===================== Question Retry Tests =====================

        [Fact]
        public async Task AddRetryRecord_ShouldStoreDataCorrectly()
        {
            // Test that a retry record for a question is successfully added
            await ResetDatabaseAsync();
            var retryRecord = new QuestionRetryRecord { SessionId = 1, QuestionId = 101, RetryCount = 3 };
            await _TestDb.AddRetryRecordAsync(retryRecord);
            var storedRecord = await _TestDb.GetRetryRecordAsync(1, 101);
            Assert.NotNull(storedRecord);
            Assert.Equal(3, storedRecord.RetryCount);
        }

        [Fact]
        public async Task GetAllRetryRecords_ShouldReturnAllEntries()
        {
            // Test that retrieving all retry records returns the correct data
            await ResetDatabaseAsync();
            await _TestDb.AddRetryRecordAsync(new QuestionRetryRecord { SessionId = 1, QuestionId = 101, RetryCount = 2 });
            await _TestDb.AddRetryRecordAsync(new QuestionRetryRecord { SessionId = 2, QuestionId = 102, RetryCount = 1 });
            var retryRecords = await _TestDb.GetAllRetryRecordsAsync();
            Assert.True(retryRecords.Count >= 2);
        }
    }
}
