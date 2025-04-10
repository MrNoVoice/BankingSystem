using System;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace BankingSystem
{
    class DatabaseConnection
    {
        private string connectionString = $"server=127.0.0.1;user=root;database=BankingSystem;port=3306;password={Environment.GetEnvironmentVariable("DB_PASSWORD")};SslMode=Preferred";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }

    class User
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }

        public User(string fullName, string email, string phone, string password)
        {
            FullName = fullName;
            Email = email;
            Phone = phone;
            Password = password;
            CreatedAt = DateTime.Now;
        }

        public static int AddUserToDatabase(MySqlConnection conn, User newUser)
        {
            try
            {
                if (newUser.Password.Length > 255)
                {
                    Console.WriteLine("Error: Password exceeds maximum length");
                    return -1;
                }

                string query = @"INSERT INTO Users 
                        (FullName, Email, Phone, Password, CreatedAt) 
                        VALUES 
                        (@FullName, @Email, @Phone, @Password, @CreatedAt)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@FullName", MySqlDbType.VarChar, 100).Value = newUser.FullName;
                    cmd.Parameters.Add("@Email", MySqlDbType.VarChar, 255).Value = newUser.Email;
                    cmd.Parameters.Add("@Phone", MySqlDbType.VarChar, 15).Value = newUser.Phone;
                    cmd.Parameters.Add("@Password", MySqlDbType.VarChar, 255).Value = newUser.Password;
                    cmd.Parameters.Add("@CreatedAt", MySqlDbType.DateTime).Value = newUser.CreatedAt;

                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT LAST_INSERT_ID()";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (MySqlException ex) when (ex.Number == 1406)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return -1;
            }
        }
    }

    class Account
    {
        public string HolderName { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }
        public string Status { get; set; }
        public int UserID { get; set; }
        public DateTime CreatedAt { get; set; }

        public Account(string holderName, decimal balance, string accountType, int userID, string status = "Active")
        {
            HolderName = holderName;
            Balance = balance;
            AccountType = accountType;
            UserID = userID;
            Status = status;
            CreatedAt = DateTime.Now;
        }

        public static bool AddAccountToDatabase(MySqlConnection conn, Account newAccount)
        {
            try
            {
                string query = @"INSERT INTO Accounts 
                        (HolderName, Balance, AccountType, Status, UserID, CreatedAt) 
                        VALUES 
                        (@HolderName, @Balance, @AccountType, @Status, @UserID, @CreatedAt)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@HolderName", newAccount.HolderName);
                    cmd.Parameters.AddWithValue("@Balance", newAccount.Balance);
                    cmd.Parameters.AddWithValue("@AccountType", newAccount.AccountType);
                    cmd.Parameters.AddWithValue("@Status", newAccount.Status);
                    cmd.Parameters.AddWithValue("@UserID", newAccount.UserID);
                    cmd.Parameters.AddWithValue("@CreatedAt", newAccount.CreatedAt);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding account: {ex.Message}");
                return false;
            }
        }
    }

    static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            string phonePattern = @"^\d{10}$";
            return Regex.IsMatch(phone, phonePattern);
        }

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string namePattern = @"^[a-zA-Z\s]+$";
            return Regex.IsMatch(name, namePattern);
        }

        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 8 && password.Length <= 255;
        }
    }

    class Program
    {
        static string GetUserInput(string prompt, Func<string, bool> validation)
        {
            string input;
            do
            {
                Console.Write(prompt);
                input = Console.ReadLine();
                if (!validation(input))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }
            } while (!validation(input));

            return input;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Banking System!");
            Console.WriteLine("Please create a new user profile before creating an account.");
            Console.WriteLine("Let's get started...");

            using (var conn = new DatabaseConnection().GetConnection())
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database connection error: {ex.Message}");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                // User creation
                string fullName = GetUserInput("\nEnter your full name (letters and spaces only): ", ValidationHelper.IsValidName);
                string email = GetUserInput("Enter your email (example@domain.com): ", ValidationHelper.IsValidEmail);
                string phone = GetUserInput("Enter your phone number (10 digits): ", ValidationHelper.IsValidPhone);
                string password = GetUserInput("Enter your password (8-255 characters): ", ValidationHelper.IsValidPassword);

                var newUser = new User(fullName, email, phone, password);
                int userId = User.AddUserToDatabase(conn, newUser);

                if (userId == -1)
                {
                    Console.WriteLine("User creation failed. Exiting...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"\nUser created successfully! Your ID: {userId}");

                // Account creation
                Console.WriteLine("\n──────────────────────────────────");
                Console.WriteLine("Account Opening Section");
                Console.WriteLine("──────────────────────────────────");

                Console.Write("\nDo you want to create an account? (yes/no): ");
                string createAccountChoice = Console.ReadLine()?.ToLower().Trim();

                if (createAccountChoice == "yes")
                {
                    string accountType = GetUserInput("\nEnter account type (Savings/Current): ",
                        input => input.Equals("Savings", StringComparison.OrdinalIgnoreCase) ||
                                input.Equals("Current", StringComparison.OrdinalIgnoreCase));

                    decimal initialBalance;
                    Console.Write("\nEnter initial balance: ");
                    while (!decimal.TryParse(Console.ReadLine(), out initialBalance) || initialBalance < 0)
                    {
                        Console.WriteLine("Invalid amount. Please enter a positive number.");
                        Console.Write("Enter initial balance: ");
                    }

                    var newAccount = new Account(fullName, initialBalance, accountType, userId);

                    if (Account.AddAccountToDatabase(conn, newAccount))
                    {
                        Console.WriteLine("\n════════ Account Created Successfully ════════");
                        Console.WriteLine($"• Account Holder: {fullName}");
                        Console.WriteLine($"• Account Type: {accountType}");
                        Console.WriteLine($"• Initial Balance: {initialBalance:C}");
                        Console.WriteLine($"• Account Status: Active");
                        Console.WriteLine("═══════════════════════════════════════");
                    }
                    else
                    {
                        Console.WriteLine("Failed to create account.");
                    }
                }
                else
                {
                    Console.WriteLine("Account creation skipped.");
                }

                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}