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

    public class Transactions
    {
        public string TransactionID { get; set; }
        public string AccountID { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }

        public Transactions(string transactionID, string accountID, string transactionType, decimal amount, DateTime transactionDate)
        {
            TransactionID = transactionID;
            AccountID = accountID;
            TransactionType = transactionType;
            Amount = amount;
            TransactionDate = transactionDate;
        }

        public static bool AddTransactionsToDatabase(MySqlConnection conn, Transactions newTransaction)
        {
            try
            {
                string query = @"INSERT INTO transactions (TransactionID, AccountID, Type, Amount, TransactionDate)
                             VALUES (@TransactionID, @AccountID, @Type, @Amount, @TransactionDate)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TransactionID", newTransaction.TransactionID);
                    cmd.Parameters.AddWithValue("@AccountID", newTransaction.AccountID);
                    cmd.Parameters.AddWithValue("@Type", newTransaction.TransactionType);
                    cmd.Parameters.AddWithValue("@Amount", newTransaction.Amount);
                    cmd.Parameters.AddWithValue("@TransactionDate", newTransaction.TransactionDate);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in process of transaction: {ex.Message}");
                return false;
            }
        }

        public static void ViewTransactions(MySqlConnection conn, string accountId)
        {
            string query = "SELECT * FROM transactions WHERE AccountID = @accountId";
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@accountId", accountId);

                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\nTransaction History:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["TransactionDate"]} | " +
                                        $"{reader["Type"]} | " +
                                        $"{reader["Amount"]}");
                    }
                }
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
            Console.WriteLine("Welcome to the Super Secure Bank!");
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
                        Console.WriteLine("\nInvalid amount. Please enter a positive number.");
                        Console.Write("\nEnter initial balance: ");
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

                // Transaction section
                Console.WriteLine();
                Console.WriteLine("===================================");
                Console.WriteLine(" Welcome to Super Secure Bank ");
                Console.WriteLine("===================================");
                Console.WriteLine("Please choose a transaction:");
                Console.WriteLine("1. Deposit");
                Console.WriteLine("2. Withdraw");
                Console.WriteLine("3. Transfer");
                Console.WriteLine("4. View Transaction History");
                Console.WriteLine("0. Exit");
                Console.WriteLine("===================================");

                Console.Write("\nEnter your choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("You chose Deposit!");
                        Console.Write("Enter your Account ID: ");
                        string depositAccountId = Console.ReadLine();

                        Console.Write("\nEnter amount to deposit: ");
                        decimal depositAmount;
                        while (!decimal.TryParse(Console.ReadLine(), out depositAmount) || depositAmount <= 0)
                        {
                            Console.Write("Invalid amount. Enter a positive number: ");
                        }
                        break;

                    case "2":
                        Console.WriteLine("\nYou chose Withdraw!");
                        Console.Write("\nEnter your Account ID: ");
                        string withdrawAccountId = Console.ReadLine();

                        Console.Write("\nEnter amount to withdraw: ");
                        decimal withdrawAmount;
                        while (!decimal.TryParse(Console.ReadLine(), out withdrawAmount) || withdrawAmount <= 0)
                        {
                            Console.Write("\nInvalid amount. Enter a positive number: ");
                        }
                        break;

                    case "3":
                        Console.WriteLine("\nYou chose Transfer!");
                        Console.Write("Enter your Account ID: ");
                        string fromAccount = Console.ReadLine();

                        Console.Write("Enter recipient Account ID: ");
                        string toAccount = Console.ReadLine();

                        Console.Write("Enter amount: ");
                        decimal amount = decimal.Parse(Console.ReadLine());

                        // Record transfer-out transaction
                        var transferOut = new Transactions(
                            Guid.NewGuid().ToString(),
                            fromAccount,
                            "Transfer Out",
                            amount,
                            DateTime.Now
                        );

                        // Record transfer-in transaction
                        var transferIn = new Transactions(
                            Guid.NewGuid().ToString(),
                            toAccount,
                            "Transfer In",
                            amount,
                            DateTime.Now
                        );

                        Transactions.AddTransactionsToDatabase(conn, transferOut);
                        Transactions.AddTransactionsToDatabase(conn, transferIn);

                        Console.WriteLine("Transfer recorded!");
                        break;

                    case "4":
                        Console.WriteLine("\nYou chose Transaction History!");
                        Console.Write("Enter Account ID: ");
                        string accountId = Console.ReadLine();
                        Transactions.ViewTransactions(conn, accountId);
                        break;

                    case "0":
                        Console.WriteLine("\nExiting... Goodbye!");
                        break;

                    default:
                        Console.WriteLine("\nInvalid choice! Try again.");
                        break;
                }

                Console.WriteLine("Thank you for choosing our bank.");

                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}