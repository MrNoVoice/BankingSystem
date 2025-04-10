using System;
using System.Text.RegularExpressions; // Add this for Regex
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

        // Static method to add user to the database
        public static void AddUsersToDatabase(MySqlConnection conn, Users newUser)
        {
            try
            {
                // Validate input
                if (!ValidationHelper.IsValidEmail(newUser.email))
                {
                    Console.WriteLine("Invalid email format.");
                    return;
                }

                if (!ValidationHelper.IsValidPhone(newUser.phone))
                {
                    Console.WriteLine("Invalid phone number format.");
                    return;
                }

                if (!ValidationHelper.IsValidName(newUser.fullname))
                {
                    Console.WriteLine("Invalid name format.");
                    return;
                }

                // SQL query to insert a new user into the Users table
                string query = "INSERT INTO Users (FullName, Email, phone, Password, CreatedAt) " +
                               "VALUES (@FullName, @Email, @phone ,  @Password, @CreatedAt)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", newUser.fullname);
                cmd.Parameters.AddWithValue("@Email", newUser.email);
                cmd.Parameters.AddWithValue("@phone", newUser.phone);
                cmd.Parameters.AddWithValue("@Password", newUser.password); // Ideally hash it before storing
                cmd.Parameters.AddWithValue("@CreatedAt", newUser.createdAt);

                cmd.ExecuteNonQuery();
                Console.WriteLine("User added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user: {ex.Message}");
            }
        }
    }

    class Accounts
    {
        // Properties
        public int AccountID { get; set; }
        public string HolderName { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }  // 'Savings' or 'Current'
        public string Status { get; set; }  // 'Active' or 'Inactive'
        public int UserID { get; set; }  // FK from Users table
        public DateTime CreatedAt { get; set; }

        // Constructor
        public Accounts(string holderName, decimal balance, string accountType, int userID, string status = "Active")
        {
            HolderName = holderName;
            Balance = balance;
            AccountType = accountType;
            UserID = userID;
            Status = status;
            CreatedAt = DateTime.Now;
        }

        // Static method to add an account to the database
        public static void AddAccountToDatabase(MySqlConnection conn, Accounts newAccount)
        {
            try
            {
                // SQL query to insert a new account into the Accounts table
                string query = "INSERT INTO Accounts (HolderName, Balance, AccountType, Status, UserID, CreatedAt) " +
                               "VALUES (@HolderName, @Balance, @AccountType, @Status, @UserID, @CreatedAt)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@HolderName", newAccount.HolderName);
                cmd.Parameters.AddWithValue("@Balance", newAccount.Balance);
                cmd.Parameters.AddWithValue("@AccountType", newAccount.AccountType);
                cmd.Parameters.AddWithValue("@Status", newAccount.Status);
                cmd.Parameters.AddWithValue("@UserID", newAccount.UserID);
                cmd.Parameters.AddWithValue("@CreatedAt", newAccount.CreatedAt);

                cmd.ExecuteNonQuery();
                Console.WriteLine("Account added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding account: {ex.Message}");
            }
        }
    }

    // Helper class for validations
    class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        public static bool IsValidPhone(string phone)
        {
            string phonePattern = @"^\d{10}$"; // Only accepts 10 digits
            return Regex.IsMatch(phone, phonePattern);
        }

        public static bool IsValidName(string name)
        {
            string namePattern = @"^[a-zA-Z\s]+$"; // Allows letters and spaces only
            return Regex.IsMatch(name, namePattern);
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

                // Greet the user and inform them to create a user
                Console.WriteLine("Welcome to the Banking System!");
                Console.WriteLine("Please create a new user profile before creating an account.");
                Console.WriteLine("Let's get started...");

                // Ask user for name, email, phone, and password
                string fullname = GetUserInput("\nEnter your name (only letters and spaces): ", ValidationHelper.IsValidName);
                string email = GetUserInput("\nEnter your email (example: user@example.com): ", ValidationHelper.IsValidEmail);
                string phone = GetUserInput("\nEnter your phone number (10 digits only): ", ValidationHelper.IsValidPhone);
                Console.Write("\nEnter your password: ");
                string password = Console.ReadLine();

                // Create a new user object with the entered data
                Users newUser = new Users(
                    userID: "",  // Empty because MySQL will auto-increment it
                    fullname: fullname,
                    email: email,
                    phone: phone,
                    password: password,  // Ideally hash it before storing
                    createdAt: DateTime.Now
                );

                // Add the user to the database
                Users.AddUsersToDatabase(conn, newUser);
            }

            Console.ReadLine();
        }

        // Helper method to get valid user input
        private static string GetUserInput(string prompt, Func<string, bool> validation)
        {
            string input = "";
            while (string.IsNullOrEmpty(input) || !validation(input))
            {
                Console.Write(prompt);
                input = Console.ReadLine();
            }
            return input;
        }
    }
}
