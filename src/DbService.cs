using System;
using System.Linq;
using System.Text;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Early_Dev_vs.src.DataModels;

namespace Early_Dev_vs.src
{
    public class DbService
    {
        private readonly SQLiteAsyncConnection _database;

        public DbService(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<StudentProfile>().Wait();  // Initialize StudentProfiles table
        }

        // Create a new student profile
        public Task<int> AddStudentAsync(StudentProfile student)
        {
            return _database.InsertAsync(student);
        }

        // Get all student profiles
        public Task<List<StudentProfile>> GetAllStudentsAsync()
        {
            return _database.Table<StudentProfile>().ToListAsync();
        }

        // Get a single student by ID
        public Task<StudentProfile> GetStudentByIdAsync(int id)
        {
            return _database.Table<StudentProfile>().Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        // Update student profile
        public Task<int> UpdateStudentAsync(StudentProfile student)
        {
            return _database.UpdateAsync(student);
        }

        // Delete student profile
        public Task<int> DeleteStudentAsync(StudentProfile student)
        {
            return _database.DeleteAsync(student);
        }
    }
}
