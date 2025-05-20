using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Early_Dev_vs.src;
using static Early_Dev_vs.src.DataModels;
using Early_Dev_vs.src.LogicAdapters;

namespace Early_Dev_vs.src.LogicAdapters
{
    // This class extracts logic from StudentProfilesPage so it can be tested separately from UI elements in MAUI.
    public class StudentProfilesLogicAdapter
    {
        private readonly DbService _dbService;

        public StudentProfilesLogicAdapter(DbService dbService)
        {
            _dbService = dbService;
        }

        // ===================== Extracted Methods from StudentProfilesPage =====================

        // Creates and saves a student profile to the "database".
        public async Task<bool> SaveStudentAsync(string name, int age, string bcba, string educationLevel)
        {
            if (string.IsNullOrWhiteSpace(name) || age <= 0)
            {
                return false; // Validation failed
            }

            var student = new StudentProfile
            {
                Name = name,
                Age = age,
                BCBA = bcba,
                EducationLevel = educationLevel,
                CompletedSessions = 0,
                IncompleteSessions = 0
            };

            await _dbService.AddStudentAsync(student);

            return true;
        }

        // Searches for a student profile by name.
        public async Task<StudentProfile?> FindStudentAsync(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return null; // Invalid search query
            }

            var students = await _dbService.GetAllStudentsAsync();
            return students.FirstOrDefault(s => !string.IsNullOrEmpty(s.Name) && s.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        // Retrieves a student's profile by ID.
        public async Task<StudentProfile?> GetStudentByIdAsync(int studentId)
        {
            return await _dbService.GetStudentByIdAsync(studentId);
        }

        // Validates the student database state.
        public async Task<bool> IsDatabaseInitializedAsync()
        {
            var students = await _dbService.GetAllStudentsAsync();
            return students != null && students.Count > 0;
        }
    }
}

namespace EarlyDevTests
{
    // This unit test suite verifies the functionality of StudentProfiles logic adapter above, using mock objects.
    public class StudentProfilesPage_UnitTests
    {
        private readonly Mock<DbService> _mockDbService;
        private readonly StudentProfilesLogicAdapter _adapter;

        public StudentProfilesPage_UnitTests()
        {
            _mockDbService = new Mock<DbService>("test.db");
            _adapter = new StudentProfilesLogicAdapter(_mockDbService.Object);
        }

        // ===================== STUDENT CREATION TEST =====================

        // Tests if a valid student profile is successfully saved in the database.
        [Fact]
        public async Task SaveStudentAsync_WithValidData_ShouldSaveStudent()
        {
            _mockDbService.Setup(db => db.AddStudentAsync(It.IsAny<StudentProfile>())).ReturnsAsync(1);

            var result = await _adapter.SaveStudentAsync("Cabbage", 5, "Nana", "Early school");

            Assert.True(result);
            _mockDbService.Verify(db => db.AddStudentAsync(It.IsAny<StudentProfile>()), Times.Once);
        }

        // Tests if saving a student with invalid data is prevented.
        [Fact]
        public async Task SaveStudentAsync_WithInvalidData_ShouldFail()
        {
            var result = await _adapter.SaveStudentAsync("", -5, "", "");

            Assert.False(result);
            _mockDbService.Verify(db => db.AddStudentAsync(It.IsAny<StudentProfile>()), Times.Never);
        }

        // ===================== STUDENT SEARCH TEST =====================

        // Tests if searching for a valid student by name returns the correct profile.
        [Fact]
        public async Task FindStudentAsync_WithMatchingName_ShouldReturnStudent()
        {
            var students = new List<StudentProfile>
            {
                new StudentProfile { Id = 1, Name = "Cabbage" },
                new StudentProfile { Id = 2, Name = "Cobaiste" }
            };
            _mockDbService.Setup(db => db.GetAllStudentsAsync()).ReturnsAsync(students);

            var result = await _adapter.FindStudentAsync("Cabbage");

            Assert.NotNull(result);
            Assert.Equal("Cabbage", result.Name);
        }

        // Tests if searching for a non-existing student returns null.
        [Fact]
        public async Task FindStudentAsync_WithNonMatchingName_ShouldReturnNull()
        {
            var students = new List<StudentProfile>
            {
                new StudentProfile { Id = 1, Name = "Cabbage" }
            };
            _mockDbService.Setup(db => db.GetAllStudentsAsync()).ReturnsAsync(students);

            var result = await _adapter.FindStudentAsync("Nonexistent Name");

            Assert.Null(result);
        }

        // ===================== STUDENT RETRIEVAL TEST =====================

        // Tests if retrieving a student by a valid ID returns the correct profile.
        [Fact]
        public async Task GetStudentByIdAsync_WithValidId_ShouldReturnStudent()
        {
            var student = new StudentProfile { Id = 1, Name = "Cabbage" };
            _mockDbService.Setup(db => db.GetStudentByIdAsync(1)).ReturnsAsync(student);

            var result = await _adapter.GetStudentByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Cabbage", result.Name);
        }

        // Tests if retrieving a student by an invalid ID returns null.
        [Fact]
        public async Task GetStudentByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            _mockDbService.Setup(db => db.GetStudentByIdAsync(It.IsAny<int>())).ReturnsAsync((StudentProfile)null);

            var result = await _adapter.GetStudentByIdAsync(999);

            Assert.Null(result);
        }

        // ===================== DATABASE INITIALIZATION TEST =====================

        // Tests if the database is correctly initialized with student records.
        [Fact]
        public async Task IsDatabaseInitializedAsync_WhenStudentsExist_ShouldReturnTrue()
        {
            var students = new List<StudentProfile> { new StudentProfile { Id = 1, Name = "Test Student" } };
            _mockDbService.Setup(db => db.GetAllStudentsAsync()).ReturnsAsync(students);

            var result = await _adapter.IsDatabaseInitializedAsync();

            Assert.True(result);
        }

        // Tests if the database correctly detects when no student records exist.
        [Fact]
        public async Task IsDatabaseInitializedAsync_WhenNoStudentsExist_ShouldReturnFalse()
        {
            _mockDbService.Setup(db => db.GetAllStudentsAsync()).ReturnsAsync(new List<StudentProfile>());

            var result = await _adapter.IsDatabaseInitializedAsync();

            Assert.False(result);
        }
    }
}
