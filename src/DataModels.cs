using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using Newtonsoft.Json;

namespace Early_Dev_vs.src
{
    public class DataModels
    {
        public class StudentProfile
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }  // Unique Student ID
            public string? Name { get; set; }  // Student's Full Name
            public int Age { get; set; }  // Age
            public string? BCBA { get; set; }  // Assigned BCBA Name
            public string? EducationLevel { get; set; }  // Education Level

            // Tracking student progress
            public int CompletedSessions { get; set; }  // Number of completed lessons
            public int IncompleteSessions { get; set; }  // Lessons left unfinished
            public StudentProfile() { } //add a parameterless constructor for SQLite 
        }
        // Define Question Model for TestSessionPage
        public class QuestionModel
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }  // Unique Question ID
            public string? QuestionText { get; set; } // Question Content
            public string? AnswerType { get; set; }  // Answer Type (Multiple Choice, True/False, etc.)
            public string? CorrectAnswer { get; set; }  // Correct Answer for evaluation
            public string ImageSourcesJson { get; set; } = "[]"; // Store ImageSources as a JSON string
            [Ignore]
            public List<string> ImageSources // Convert JSON to List<string> when accessing images
            {
                get => JsonConvert.DeserializeObject<List<string>>(ImageSourcesJson ?? "[]") ?? new List<string>();
                set => ImageSourcesJson = JsonConvert.SerializeObject(value);
            }
            public QuestionModel()
            {
                ImageSources = new List<string>(); // Initialize empty image list
            }
        }

        // Test sessions
        public class TestSessionRecord
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; } // Unique record ID
            [Indexed]
            public int StudentId { get; set; } // Linked student ID
            public string? ResponseType { get; set; } // Correct, Incorrect, No Response
            public string? PromptUsed { get; set; } // Selected prompt type
            public DateTime Timestamp { get; set; } // When the response was recorded
            public int RetryCount { get; set; } // model to allow recording of how many times the retry button was pressed
        }

    }
}
