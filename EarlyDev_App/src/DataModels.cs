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
        // Student profile model for the database
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
        // Define Question Model for TestSessionPage - database model for questions
        public class QuestionModel
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }  // Unique Question ID
            public string? QuestionText { get; set; } // Question Content
            public string? AnswerType { get; set; }  // Answer Type (Multiple Choice, True/False, etc.)
            public string? CorrectAnswer { get; set; }  // Correct Answer for evaluation
            public string ImageSourcesString { get; set; } = ""; // Store ImageSources as a comma-separated string

            [Ignore]
            public List<string> ImageSources // Convert comma-separated string to List<string> when accessing images
            {
                get => (ImageSourcesString ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                set => ImageSourcesString = string.Join(",", value);
            }

            public QuestionModel()
            {
                ImageSourcesString = ""; // Initialize empty string to prevent null issues
                ImageSources = new List<string>(); // Ensure the list is initialized
            }
        }

        // Test sessions database model
        public class TestSessionRecord
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; } // Unique record ID
            [Indexed]
            public int StudentId { get; set; } // Linked student ID
            public string? ResponseType { get; set; } // Correct, Incorrect, No Response
            public string? PromptUsed { get; set; } // Selected prompt type
            public DateTime Timestamp { get; set; } // When the response was recorded
            public int CurrentQuestionId { get; set; } // Track the active question being answered
        }

        // Question retry count model for a separate table in the db
        public class QuestionRetryRecord
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; } // Unique retry record ID
            public int SessionId { get; set; } // Link to the TestSessionRecord
            public int QuestionId { get; set; } // Link to the Question
            public int RetryCount { get; set; } // Number of retries for this question
        }

    }
}
