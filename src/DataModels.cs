using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

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
    }
}
