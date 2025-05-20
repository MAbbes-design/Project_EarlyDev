using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Early_Dev_vs.src;
using static Early_Dev_vs.src.DataModels;

namespace EarlyDevTests
{
    // Define the QuestionManagementLogicAdapter class in the same file
    public class QuestionManagementLogicAdapter
    {
        private readonly DbService _dbService;
        private ObservableCollection<QuestionModel> _questions;
        private ObservableCollection<string> _selectedImages;
        private QuestionModel? _currentEditingQuestion;
        private bool _isEditMode;

        public QuestionManagementLogicAdapter(DbService dbService)
        {
            _dbService = dbService;
            _questions = new ObservableCollection<QuestionModel>();
            _selectedImages = new ObservableCollection<string>();
        }

        // ===================== Extracted Methods from QuestionManagementPage =====================

        public async Task LoadQuestionsAsync()
        {
            var allQuestions = await _dbService.GetAllQuestionsAsync();
            _questions.Clear();
            foreach (var question in allQuestions)
            {
                _questions.Add(question);
            }
        }

        public void ResetForm()
        {
            _isEditMode = false;
            _currentEditingQuestion = null;
            _selectedImages.Clear();
        }

        public async Task<bool> SaveOrUpdateQuestionAsync(string questionText, string answerType, string correctAnswer)
        {
            if (string.IsNullOrWhiteSpace(questionText) || string.IsNullOrWhiteSpace(correctAnswer) || string.IsNullOrEmpty(answerType))
            {
                return false; // Validation failed
            }

            if (_isEditMode && _currentEditingQuestion != null)
            {
                _currentEditingQuestion.QuestionText = questionText;
                _currentEditingQuestion.AnswerType = answerType;
                _currentEditingQuestion.CorrectAnswer = correctAnswer;
                _currentEditingQuestion.ImageSources = new List<string>(_selectedImages);

                await _dbService.UpdateQuestionAsync(_currentEditingQuestion);
            }
            else
            {
                var newQuestion = new QuestionModel
                {
                    QuestionText = questionText,
                    AnswerType = answerType,
                    CorrectAnswer = correctAnswer,
                    ImageSources = new List<string>(_selectedImages)
                };

                await _dbService.AddQuestionAsync(newQuestion);
            }

            ResetForm();
            await LoadQuestionsAsync();
            return true;
        }

        public void UploadImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                _selectedImages.Add(imagePath);
            }
        }

        public void RemoveImage(string imagePath)
        {
            _selectedImages.Remove(imagePath);
        }

        public void StartEditingQuestion(int questionId)
        {
            var questionToEdit = _questions.FirstOrDefault(q => q.Id == questionId);
            if (questionToEdit != null)
            {
                _currentEditingQuestion = questionToEdit;
                _isEditMode = true;
                _selectedImages.Clear();
                foreach (var image in questionToEdit.ImageSources)
                {
                    _selectedImages.Add(image);
                }
            }
        }

        public virtual async Task<bool> DeleteQuestionAsync(int questionId)
        {
            var questionToDelete = _questions.FirstOrDefault(q => q.Id == questionId);
            if (questionToDelete != null)
            {
                await _dbService.DeleteQuestionAsync(questionToDelete);
                _questions.Remove(questionToDelete);
                return true;
            }
            return false;
        }

        // ===================== STATE ACCESSORS =====================

        public ObservableCollection<QuestionModel> GetQuestions() => _questions;
        public ObservableCollection<string> GetSelectedImages() => _selectedImages;
        public QuestionModel? GetEditingQuestion() => _currentEditingQuestion;
        public bool IsEditMode() => _isEditMode;
    }

    public class QuestionManagementPage_UnitTests
    {
        private readonly Mock<DbService> _mockDbService;
        private readonly QuestionManagementLogicAdapter _adapter;

        public QuestionManagementPage_UnitTests()
        {
            _mockDbService = new Mock<DbService>("test.db");
            _adapter = new QuestionManagementLogicAdapter(_mockDbService.Object);
        }

        // ===================== QUESTION LOADING TESTS =====================
        [Fact]
        public async Task LoadQuestions_ShouldPopulateCollection()
        {
            _mockDbService.Setup(db => db.GetAllQuestionsAsync()).ReturnsAsync(new List<QuestionModel>
            {
                new QuestionModel { Id = 1, QuestionText = "Question 1" },
                new QuestionModel { Id = 2, QuestionText = "Question 2" }
            });

            await _adapter.LoadQuestionsAsync();

            Assert.NotEmpty(_adapter.GetQuestions());
            Assert.Equal(2, _adapter.GetQuestions().Count);
        }

        // ===================== QUESTION CREATION TESTS =====================
        [Fact]
        public async Task SaveOrUpdateQuestionAsync_WithValidData_ShouldAddQuestion()
        {
            _mockDbService.Setup(db => db.AddQuestionAsync(It.IsAny<QuestionModel>())).ReturnsAsync(1);
            _mockDbService.Setup(db => db.GetAllQuestionsAsync()).ReturnsAsync(new List<QuestionModel>());

            var result = await _adapter.SaveOrUpdateQuestionAsync("What is 2+2?", "Multiple Choice", "4");

            Assert.True(result);
            _mockDbService.Verify(db => db.AddQuestionAsync(It.IsAny<QuestionModel>()), Times.Once);
        }

        // ===================== IMAGE SELECTION TESTS =====================
        [Fact]
        public void UploadImage_ShouldAddImageToCollection()
        {
            _adapter.UploadImage("test_image.png");

            Assert.Contains("test_image.png", _adapter.GetSelectedImages());
        }

        [Fact]
        public void RemoveImage_ShouldRemoveImage()
        {
            _adapter.UploadImage("test_image.png");
            _adapter.RemoveImage("test_image.png");

            Assert.DoesNotContain("test_image.png", _adapter.GetSelectedImages());
        }

        // ===================== QUESTION EDITING TESTS =====================
        [Fact]
        public void StartEditingQuestion_ShouldSetEditModeAndLoadImages()
        {
            var testQuestion = new QuestionModel
            {
                Id = 5,
                QuestionText = "Edit Me",
                ImageSources = new List<string> { "img1.png", "img2.png" }
            };

            _mockDbService.Setup(db => db.GetAllQuestionsAsync())
                .ReturnsAsync(new List<QuestionModel> { testQuestion });

            // First load the questions into the adapter
            _adapter.LoadQuestionsAsync().Wait();

            // Then start editing
            _adapter.StartEditingQuestion(5);

            Assert.True(_adapter.IsEditMode());
            Assert.Equal(2, _adapter.GetSelectedImages().Count);
        }

        // ===================== QUESTION DELETION TESTS =====================
        [Fact]
        public virtual async Task DeleteQuestion_ShouldRemoveQuestion()
        {
            var testQuestion = new QuestionModel { Id = 1, QuestionText = "To Be Deleted" };

            // Setup the mock to return our test question
            _mockDbService.Setup(db => db.GetAllQuestionsAsync())
                .ReturnsAsync(new List<QuestionModel> { testQuestion });

            // Load questions first so there's something to delete
            await _adapter.LoadQuestionsAsync();

            // Setup DeleteQuestionAsync to return 1 to indicate success
            _mockDbService.Setup(db => db.DeleteQuestionAsync(It.IsAny<QuestionModel>()))
                .ReturnsAsync(1);

            var result = await _adapter.DeleteQuestionAsync(1);

            Assert.True(result);
            _mockDbService.Verify(db => db.DeleteQuestionAsync(It.IsAny<QuestionModel>()), Times.Once);
        }
    }
}