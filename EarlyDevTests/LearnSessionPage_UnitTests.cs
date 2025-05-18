using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Early_Dev_vs.src;
using static Early_Dev_vs.src.DataModels;

namespace EarlyDevTests
{
    // This page performs unit tests on the learn session page. I implemented this by extracting the core logic from that file and putting it here to test it as MAUI requires UI elements to function which blocks me from performing tests properly.
    public class LearnSessionLogicAdapter
    {
        private readonly DbService _dbService;
        private Dictionary<string, bool> _selectedImages = new Dictionary<string, bool>();
        private TestSessionRecord _currentSessionRecord;
        private int _currentStudentId;
        private int _currentQuestionId = -1;

        public LearnSessionLogicAdapter(DbService dbService, int studentId)
        {
            _dbService = dbService;
            _currentStudentId = studentId;
        }

        // ===================== Extracted methods and classes from the learn Sessions Page. =====================

        public async Task InitializeSessionRecord()
        {
            _currentSessionRecord = new TestSessionRecord
            {
                StudentId = _currentStudentId,
                ResponseType = "Session Started",
                PromptUsed = "None",
                Timestamp = DateTime.UtcNow,
                CurrentQuestionId = -1
            };

            await _dbService.SaveTestSessionAsync(_currentSessionRecord);
        }

        public async Task<QuestionModel> LoadQuestion()
        {
            QuestionModel currentQuestion = await _dbService.GetRandomQuestionAsync();

            if (currentQuestion == null)
            {
                return null;
            }

            _currentQuestionId = currentQuestion.Id;
            _selectedImages.Clear();

            if (_currentSessionRecord != null)
            {
                _currentSessionRecord.CurrentQuestionId = currentQuestion.Id;
                await _dbService.UpdateSessionRecordAsync(_currentSessionRecord);
            }

            return currentQuestion;
        }

        public async Task<bool> RecordResponse(string responseType, string promptType)
        {
            if (promptType == "None")
            {
                return false; // Prompt type required
            }

            if (_currentSessionRecord == null)
            {
                _currentSessionRecord = new TestSessionRecord
                {
                    StudentId = _currentStudentId,
                    ResponseType = responseType,
                    PromptUsed = promptType,
                    Timestamp = DateTime.UtcNow,
                    CurrentQuestionId = _currentQuestionId
                };

                await _dbService.SaveTestSessionAsync(_currentSessionRecord);
            }
            else
            {
                _currentSessionRecord.ResponseType = responseType;
                _currentSessionRecord.PromptUsed = promptType;
                _currentSessionRecord.Timestamp = DateTime.UtcNow;
                await _dbService.UpdateSessionRecordAsync(_currentSessionRecord);
            }

            return true;
        }

        public async Task<int> RecordRetry(string promptType)
        {
            if (_currentSessionRecord == null || _currentSessionRecord.Id == 0)
            {
                return -1; // No active session
            }

            if (promptType == "None")
            {
                return -2; // Prompt type required
            }

            if (_currentSessionRecord.CurrentQuestionId <= 0)
            {
                return -3; // No active question
            }

            // Get existing retry record for this question in the session
            var retryRecord = await _dbService.GetRetryRecordAsync(_currentSessionRecord.Id, _currentSessionRecord.CurrentQuestionId);

            if (retryRecord == null)
            {
                // Create a new retry entry if none exists
                retryRecord = new QuestionRetryRecord
                {
                    SessionId = _currentSessionRecord.Id,
                    QuestionId = _currentSessionRecord.CurrentQuestionId,
                    RetryCount = 1
                };

                await _dbService.AddRetryRecordAsync(retryRecord);
            }
            else
            {
                // Increment retry count for this question
                retryRecord.RetryCount++;
                await _dbService.UpdateRetryRecordAsync(retryRecord);
            }

            // Update the current session record with "Retry" response type
            _currentSessionRecord.ResponseType = "Retry";
            _currentSessionRecord.PromptUsed = promptType;
            _currentSessionRecord.Timestamp = DateTime.UtcNow;
            await _dbService.UpdateSessionRecordAsync(_currentSessionRecord);

            return retryRecord.RetryCount;
        }

        // ===================== STATE ACCESSORS =====================

        public TestSessionRecord GetCurrentSessionRecord() => _currentSessionRecord;
        public int GetCurrentStudentId() => _currentStudentId;
        public int GetCurrentQuestionId() => _currentQuestionId;
    }

    public class LearnSessionUnitTests
    {
        // ===================== SESSION INITIALIZATION TESTS =====================

        [Fact]
        public async Task InitializeSessionRecord_ShouldCreateValidRecord()
        {
            // Creates a new session record with correct defaults
            var mockDb = new Mock<DbService>("test.db");
            TestSessionRecord capturedRecord = null;

            mockDb.Setup(db => db.SaveTestSessionAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => capturedRecord = record)
                .Returns(Task.CompletedTask);

            var adapter = new LearnSessionLogicAdapter(mockDb.Object, 1);

            await adapter.InitializeSessionRecord();

            Assert.NotNull(capturedRecord);
            Assert.Equal(1, capturedRecord.StudentId);
            Assert.Equal("Session Started", capturedRecord.ResponseType);
            Assert.Equal("None", capturedRecord.PromptUsed);
            Assert.Equal(-1, capturedRecord.CurrentQuestionId);
        }

        // ===================== QUESTION LOADING TESTS =====================

        [Fact]
        public async Task LoadQuestion_ShouldUpdateSessionWithQuestionId()
        {
            // Loads a question and updates session record with question ID
            var mockDb = new Mock<DbService>("test.db");
            TestSessionRecord capturedRecord = null;

            mockDb.Setup(db => db.GetRandomQuestionAsync())
                .ReturnsAsync(new QuestionModel
                {
                    Id = 5,
                    QuestionText = "Test Question",
                    ImageSources = new List<string> { "image1.png", "image2.png" }
                });

            mockDb.Setup(db => db.UpdateSessionRecordAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => capturedRecord = record)
                .ReturnsAsync(1);

            var adapter = new LearnSessionLogicAdapter(mockDb.Object, 1);
            await adapter.InitializeSessionRecord();

            var question = await adapter.LoadQuestion();

            Assert.NotNull(question);
            Assert.Equal("Test Question", question.QuestionText);
            Assert.Equal(5, adapter.GetCurrentQuestionId());
            Assert.NotNull(capturedRecord);
            Assert.Equal(5, capturedRecord.CurrentQuestionId);
        }

        // ===================== RESPONSE RECORDING TESTS =====================

        [Fact]
        public async Task RecordResponse_WithValidPrompt_ShouldReturnTrue()
        {
            // Successfully records a response with a valid prompt type
            var mockDb = new Mock<DbService>("test.db");
            TestSessionRecord capturedRecord = null;

            mockDb.Setup(db => db.SaveTestSessionAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => capturedRecord = record)
                .Returns(Task.CompletedTask);

            mockDb.Setup(db => db.UpdateSessionRecordAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => capturedRecord = record)
                .ReturnsAsync(1);

            var adapter = new LearnSessionLogicAdapter(mockDb.Object, 1);
            await adapter.InitializeSessionRecord();

            var result = await adapter.RecordResponse("Correct", "Verbal");

            Assert.True(result);
            Assert.NotNull(capturedRecord);
            Assert.Equal("Correct", capturedRecord.ResponseType);
            Assert.Equal("Verbal", capturedRecord.PromptUsed);
        }

        [Fact]
        public async Task RecordResponse_WithNoPrompt_ShouldReturnFalse()
        {
            // Rejects recording a response when no prompt type is provided
            var mockDb = new Mock<DbService>("test.db");
            var adapter = new LearnSessionLogicAdapter(mockDb.Object, 1);
            await adapter.InitializeSessionRecord();

            var result = await adapter.RecordResponse("Correct", "None");

            Assert.False(result);
        }

        // ===================== RETRY MANAGEMENT TESTS =====================

        [Fact]
        public virtual async Task RecordRetry_ShouldIncrementRetryCount()
        {
            // Creates a new retry record when none exists
            var mockDb = new Mock<DbService>("test.db");
            QuestionRetryRecord capturedRetryRecord = null;
            TestSessionRecord capturedSessionRecord = null;

            mockDb.Setup(db => db.SaveTestSessionAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => {
                    capturedSessionRecord = record;
                    record.Id = 10; // Assign an ID for testing
                })
                .Returns(Task.CompletedTask);

            mockDb.Setup(db => db.GetRandomQuestionAsync())
                .ReturnsAsync(new QuestionModel { Id = 5 });

            mockDb.Setup(db => db.UpdateSessionRecordAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => capturedSessionRecord = record)
                .ReturnsAsync(1);

            mockDb.Setup(db => db.GetRetryRecordAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((QuestionRetryRecord)null);

            mockDb.Setup(db => db.AddRetryRecordAsync(It.IsAny<QuestionRetryRecord>()))
                .Callback<QuestionRetryRecord>(record => capturedRetryRecord = record)
                .ReturnsAsync(1);

            var adapter = new LearnSessionLogicAdapter(mockDb.Object, 1);
            await adapter.InitializeSessionRecord();
            await adapter.LoadQuestion();

            var retryCount = await adapter.RecordRetry("Verbal");

            Assert.Equal(1, retryCount);
            Assert.NotNull(capturedRetryRecord);
            Assert.Equal(10, capturedRetryRecord.SessionId);
            Assert.Equal(5, capturedRetryRecord.QuestionId);
            Assert.Equal(1, capturedRetryRecord.RetryCount);
            Assert.Equal("Retry", capturedSessionRecord.ResponseType);
            Assert.Equal("Verbal", capturedSessionRecord.PromptUsed);
        }

        [Fact]
        public virtual async Task RecordRetry_WithExistingRetry_ShouldIncrementExistingCount()
        {
            // Increments an existing retry record's count
            var mockDb = new Mock<DbService>("test.db");
            QuestionRetryRecord capturedRetryRecord = null;

            mockDb.Setup(db => db.SaveTestSessionAsync(It.IsAny<TestSessionRecord>()))
                .Callback<TestSessionRecord>(record => record.Id = 10)
                .Returns(Task.CompletedTask);

            mockDb.Setup(db => db.GetRandomQuestionAsync())
                .ReturnsAsync(new QuestionModel { Id = 5 });

            mockDb.Setup(db => db.GetRetryRecordAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new QuestionRetryRecord
                {
                    SessionId = 10,
                    QuestionId = 5,
                    RetryCount = 2
                });

            mockDb.Setup(db => db.UpdateRetryRecordAsync(It.IsAny<QuestionRetryRecord>()))
                .Callback<QuestionRetryRecord>(record => capturedRetryRecord = record)
                .ReturnsAsync(1);

            var adapter = new LearnSessionLogicAdapter(mockDb.Object, 1);
            await adapter.InitializeSessionRecord();
            await adapter.LoadQuestion();

            var retryCount = await adapter.RecordRetry("Verbal");

            Assert.Equal(3, retryCount);
            Assert.NotNull(capturedRetryRecord);
            Assert.Equal(3, capturedRetryRecord.RetryCount);
        }
    }
}