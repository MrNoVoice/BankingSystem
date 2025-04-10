using System;
using System.Text.RegularExpressions;  // Add this for Regex
using MySql.Data.MySqlClient;

namespace BankingSystem
{
    class DatabaseConnection
    {
        // Connection string for MySQL
        private string connectionString = $"server=127.0.0.1;user=root;database=BankingSystem;port=3306;password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }

    class Users
    {
        // Properties
        public string userID { get; set; }
        public string fullname { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string password { get; set; }
        public DateTime createdAt { get; set; }

        // Constructor
        public Users(string userID, string fullname, string email, string phone, string password, DateTime createdAt)
        {
            this.userID = userID;
            this.fullname = fullname;
            this.email = email;
            this.phone = phone;
            this.password = password;
            this.createdAt = createdAt;
        }

        // Method to validate email
        private bool IsValidEmail(string email)
        {
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        // Method to validate phone number (only digits, e.g., 10 digits)
        private bool IsValidPhone(string phone)
        {
            string phonePattern = @"^\d{10}$";  // Only accepts 10 digits
            return Regex.IsMatch(phone, phonePattern);
        }

        // Method to validate name (only letters and spaces)
        private bool IsValidName(string name)
        {
            string namePattern = @"^[a-zA-Z\s]+$";  // Allows letters and spaces only
            return Regex.IsMatch(name, namePattern);
        }

        // Static method to add user to the database
        public static void AddUsersToDatabase(MySqlConnection conn, Users newUser)
        {
            try
            {
                // Validate email, phone, and name before inserting
                if (!newUser.IsValidEmail(newUser.email))
                {
                    Console.WriteLine("Invalid email format. Please enter a valid email.");
                    return;
                }

                if (!newUser.IsValidPhone(newUser.phone))
                {
                    Console.WriteLine("Invalid phone number. Please enter a valid 10-digit phone number.");
                    return;
                }

                if (!newUser.IsValidName(newUser.fullname))
                {
                    Console.WriteLine("Invalid name. Please enter a valid name with only letters and spaces.");
                    return;
                }

                // SQL query to insert a new user into the Users table
                string query = "INSERT INTO Users (FullName, Email, phone, Password, CreatedAt) " +
                               "VALUES (@FullName, @Email, @phone ,  @Password, @CreatedAt)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", newUser.fullname);
                cmd.Parameters.AddWithValue("@Email", newUser.email);
                cmd.Parameters.AddWithValue("@phone", newUser.phone);
                cmd.Parameters.AddWithValue("@Password", newUser.password);
                cmd.Parameters.AddWithValue("@CreatedAt", newUser.createdAt);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Use "using" block to automatically close the connection after usage
            using (var conn = new DatabaseConnection().GetConnection())
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database connection error: {ex.Message}");
                    return; // Exit if connection fails
                }

                // Ask user for name, email, phone, and password
                string fullname = "";
                string email = "";
                string phone = "";
                string password = "";

                while (string.IsNullOrEmpty(fullname) || !IsValidName(fullname))
                {
                    Console.Write("Enter your name (only letters and spaces): ");
                    fullname = Console.ReadLine();
                }

                while (string.IsNullOrEmpty(email) || !IsValidEmail(email))
                {
                    Console.Write("Enter your email (example: user@example.com): ");
                    email = Console.ReadLine();
                }

                while (string.IsNullOrEmpty(phone) || !IsValidPhone(phone))
                {
                    Console.Write("Enter your phone number (10 digits only): ");
                    phone = Console.ReadLine();
                }

                Console.Write("Enter your password: ");
                password = Console.ReadLine();

                // Create a new user object with the entered data
                Users newUser = new Users(
                    userID: "",  // Empty because MySQL will auto-increment it
                    fullname: fullname,
                    email: email,
                    phone: phone,
                    password: password,  // You can hash it here for security
                    createdAt: DateTime.Now
                );

                // Add the user to the database
                Users.AddUsersToDatabase(conn, newUser);

                Console.WriteLine("User added successfully!");
            }

            Console.ReadLine();
        }

        // Helper method to validate email
        private static bool IsValidEmail(string email)
        {
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        // Helper method to validate phone number
        private static bool IsValidPhone(string phone)
        {
            string phonePattern = @"^\d{10}$";
            return Regex.IsMatch(phone, phonePattern);
        }

        // Helper method to validate name
        private static bool IsValidName(string name)
        {
            string namePattern = @"^[a-zA-Z\s]+$";
            return Regex.IsMatch(name, namePattern);
        }
    }
}
