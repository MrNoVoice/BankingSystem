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

    class Users
    {
        public string UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }

        public Users(string fullName, string email, string phone, string password)
        {
            FullName = fullName;
            Email = email;
            Phone = phone;
            Password = password;
            CreatedAt = DateTime.Now;
        }

        public static int AddUserToDatabase(MySqlConnection conn, Users newUser)
        {
            try
            {
                if (!ValidationHelper.IsValidEmail(newUser.Email))
                {
                    Console.WriteLine("Invalid email format.");
                    return -1;
                }

                if (!ValidationHelper.IsValidPhone(newUser.Phone))
                {
                    Console.WriteLine("Invalid phone number format.");
                    return -1;
                }

                if (!ValidationHelper.IsValidName(newUser.FullName))
                {
                    Console.WriteLine("Invalid name format.");
                    return -1;
                }

                string query = "INSERT INTO Users (FullName, Email, Phone, Password, CreatedAt) VALUES (@FullName, @Email, @Phone, @Password, @CreatedAt)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", newUser.FullName);
                    cmd.Parameters.AddWithValue("@Email", newUser.Email);
                    cmd.Parameters.AddWithValue("@Phone", newUser.Phone);
                    cmd.Parameters.AddWithValue("@Password", newUser.Password);
                    cmd.Parameters.AddWithValue("@CreatedAt", newUser.CreatedAt);

                    cmd.ExecuteNonQuery();

                    string userIdQuery = "SELECT LAST_INSERT_ID()";
                    using (var userIdCmd = new MySqlCommand(userIdQuery, conn))
                    {
                        return Convert.ToInt32(userIdCmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user: {ex.Message}");
                return -1;
            }
        }
    }

    class Accounts
    {
        public string HolderName { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }
        public string Status { get; set; }
        public int UserID { get; set; }
        public DateTime CreatedAt { get; set; }

        public Accounts(string holderName, decimal balance, string accountType, int userID, string status = "Active")
        {
            HolderName = holderName;
            Balance = balance;
            AccountType = accountType;
            UserID = userID;
            Status = status;
            CreatedAt = DateTime.Now;
        }

        public static void AddAccountToDatabase(MySqlConnection conn, Accounts newAccount)
        {
            try
            {
                string query = "INSERT INTO Accounts (HolderName, Balance, AccountType, Status, UserID, CreatedAt) VALUES (@HolderName, @Balance, @AccountType, @Status, @UserID, @CreatedAt)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@HolderName", newAccount.HolderName);
                    cmd.Parameters.AddWithValue("@Balance", newAccount.Balance);
                    cmd.Parameters.AddWithValue("@AccountType", newAccount.AccountType);
                    cmd.Parameters.AddWithValue("@Status", newAccount.Status);
                    cmd.Parameters.AddWithValue("@UserID", newAccount.UserID);
                    cmd.Parameters.AddWithValue("@CreatedAt", newAccount.CreatedAt);

                    cmd.ExecuteNonQuery();
                    Console.WriteLine("\nAccount added successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding account: {ex.Message}");
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
            } while (!validation(input));

            return input;
        }

        static void Main(string[] args)
        {
            using (var conn = new DatabaseConnection().GetConnection())
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database connection error: {ex.Message}");
                    return;
                }

                Console.WriteLine("Welcome to the Banking System!");
                Console.WriteLine("Please create a new user profile before creating an account.");
                Console.WriteLine("Let's get started...");

                string fullName = GetUserInput("\nEnter your name (only letters and spaces): ", ValidationHelper.IsValidName);
                string email = GetUserInput("\nEnter your email (example: user@example.com): ", ValidationHelper.IsValidEmail);
                string phone = GetUserInput("\nEnter your phone number (10 digits only): ", ValidationHelper.IsValidPhone);
                Console.Write("\nEnter your password: ");
                string password = Console.ReadLine();

                Users newUser = new Users(fullName, email, phone, password);
                int userID = Users.AddUserToDatabase(conn, newUser);

                if (userID == -1)
                {
                    Console.WriteLine("User creation failed. Exiting...");
                    return;
                }

                Console.WriteLine("\n──────────────────────────────────");
                Console.WriteLine("Now entering the account opening section.");
                Console.WriteLine("Please follow the instructions below:");
                Console.WriteLine("──────────────────────────────────");

                Console.Write("\nDo you want to create an account (yes/no)? ");
                string createAccountChoice = Console.ReadLine().ToLower();

                if (createAccountChoice == "yes")
                {
                    string accountType = GetUserInput("\nEnter the type of account (Savings/Current): ",
                        input => input.Equals("Savings", StringComparison.OrdinalIgnoreCase) ||
                                input.Equals("Current", StringComparison.OrdinalIgnoreCase));

                    decimal initialBalance;
                    Console.Write("\nEnter the initial balance for the account: ");
                    while (!decimal.TryParse(Console.ReadLine(), out initialBalance) || initialBalance < 0)
                    {
                        Console.WriteLine("Invalid balance. Please enter a positive number.");
                        Console.Write("Enter the initial balance for the account: ");
                    }

                    Accounts newAccount = new Accounts(
                        holderName: fullName,
                        balance: initialBalance,
                        accountType: accountType,
                        userID: userID
                    );

                    Accounts.AddAccountToDatabase(conn, newAccount);

                    Console.WriteLine("\n════════ Account Created Successfully ════════");
                    Console.WriteLine($"• Account Holder: {fullName}");
                    Console.WriteLine($"• Account Type: {accountType}");
                    Console.WriteLine($"• Initial Balance: {initialBalance:C}");
                    Console.WriteLine($"• Account Status: Active");
                    Console.WriteLine("═══════════════════════════════════════");
                    Console.WriteLine("\nPress any key to exit...");

                    Console.ReadKey();  // This will keep the window open until user presses a key
                }
            }
        }
    }
}