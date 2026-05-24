using System;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace BankingSystem
{
    class DatabaseConnection
    {
        private string connectionString = "Data Source=BankingSystem.db";

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(connectionString);
        }

        public void InitializeDatabase()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullName TEXT NOT NULL,
                        Email TEXT UNIQUE NOT NULL,
                        Phone TEXT,
                        Password TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS Accounts (
                        AccountID INTEGER PRIMARY KEY AUTOINCREMENT,
                        HolderName TEXT NOT NULL,
                        Balance DECIMAL(10,2) DEFAULT 0.00,
                        AccountType TEXT CHECK(AccountType IN ('Savings', 'Current')),
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Status TEXT DEFAULT 'Active',
                        UserID INTEGER,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    );

                    CREATE TABLE IF NOT EXISTS Transactions (
                        TransactionID TEXT PRIMARY KEY,
                        AccountID INTEGER,
                        Type TEXT CHECK(Type IN ('Deposit', 'Withdraw', 'Transfer Out', 'Transfer In')),
                        Amount DECIMAL(10,2),
                        Description TEXT,
                        TransactionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID)
                    );";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
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

        public static int AddUserToDatabase(SqliteConnection conn, User newUser)
        {
            try
            {
                string query = @"INSERT INTO Users 
                        (FullName, Email, Phone, Password, CreatedAt) 
                        VALUES 
                        (@FullName, @Email, @Phone, @Password, @CreatedAt);
                        SELECT last_insert_rowid();";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", newUser.FullName);
                    cmd.Parameters.AddWithValue("@Email", newUser.Email);
                    cmd.Parameters.AddWithValue("@Phone", newUser.Phone);
                    cmd.Parameters.AddWithValue("@Password", newUser.Password);
                    cmd.Parameters.AddWithValue("@CreatedAt", newUser.CreatedAt);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
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

        public static bool AddAccountToDatabase(SqliteConnection conn, Account newAccount)
        {
            try
            {
                string query = @"INSERT INTO Accounts 
                        (HolderName, Balance, AccountType, Status, UserID, CreatedAt) 
                        VALUES 
                        (@HolderName, @Balance, @AccountType, @Status, @UserID, @CreatedAt)";

                using (var cmd = new SqliteCommand(query, conn))
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

        public static bool UpdateBalance(SqliteConnection conn, int accountId, decimal amount, SqliteTransaction transaction = null)
        {
            try
            {
                // If amount is negative (withdrawal/transfer out), we check for sufficient funds within the query.
                string query = amount >= 0
                    ? "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountID = @AccountID"
                    : "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountID = @AccountID AND Balance >= ABS(@Amount)";

                using (var cmd = new SqliteCommand(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@AccountID", accountId);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating balance: {ex.Message}");
                return false;
            }
        }

        public static decimal GetBalance(SqliteConnection conn, int accountId)
        {
            string query = "SELECT Balance FROM Accounts WHERE AccountID = @AccountID";
            using (var cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@AccountID", accountId);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : -1;
            }
        }
    }

    public class Transactions
    {
        public string TransactionID { get; set; }
        public int AccountID { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }

        public Transactions(string transactionID, int accountID, string transactionType, decimal amount, DateTime transactionDate)
        {
            TransactionID = transactionID;
            AccountID = accountID;
            TransactionType = transactionType;
            Amount = amount;
            TransactionDate = transactionDate;
        }

        public static bool AddTransactionsToDatabase(SqliteConnection conn, Transactions newTransaction, SqliteTransaction transaction = null)
        {
            try
            {
                string query = @"INSERT INTO Transactions (TransactionID, AccountID, Type, Amount, TransactionDate)
                             VALUES (@TransactionID, @AccountID, @Type, @Amount, @TransactionDate)";

                using (var cmd = new SqliteCommand(query, conn, transaction))
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

        public static void ViewTransactions(SqliteConnection conn, int accountId)
        {
            string query = "SELECT * FROM Transactions WHERE AccountID = @accountId ORDER BY TransactionDate DESC";
            using (var cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@accountId", accountId);

                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\nTransaction History:");
                    Console.WriteLine("Date                | Type         | Amount");
                    Console.WriteLine("-------------------------------------------");
                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["TransactionDate"],-19} | {reader["Type"],-12} | {reader["Amount"],10:C}");
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
                if (input == null || !validation(input))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }
            } while (input == null || !validation(input));

            return input;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Super Secure Bank!");

            var dbConn = new DatabaseConnection();
            dbConn.InitializeDatabase();

            Console.WriteLine("Please create a new user profile before creating an account.");
            Console.WriteLine("Let's get started...");

            using (var conn = dbConn.GetConnection())
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

                while (true)
                {
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

                    if (choice == "0") break;

                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine("\nYou chose Deposit!");
                            Console.Write("Enter your Account ID: ");
                            if (int.TryParse(Console.ReadLine(), out int depositAccountId))
                            {
                                Console.Write("Enter amount to deposit: ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal depositAmount) && depositAmount > 0)
                                {
                                    if (Account.UpdateBalance(conn, depositAccountId, depositAmount))
                                    {
                                        var depositTx = new Transactions(Guid.NewGuid().ToString(), depositAccountId, "Deposit", depositAmount, DateTime.Now);
                                        Transactions.AddTransactionsToDatabase(conn, depositTx);
                                        Console.WriteLine("Deposit successful!");
                                    }
                                    else Console.WriteLine("Account not found.");
                                }
                                else Console.WriteLine("Invalid amount.");
                            }
                            else Console.WriteLine("Invalid Account ID.");
                            break;

                        case "2":
                            Console.WriteLine("\nYou chose Withdraw!");
                            Console.Write("Enter your Account ID: ");
                            if (int.TryParse(Console.ReadLine(), out int withdrawAccountId))
                            {
                                Console.Write("Enter amount to withdraw: ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal withdrawAmount) && withdrawAmount > 0)
                                {
                                    if (Account.UpdateBalance(conn, withdrawAccountId, -withdrawAmount))
                                    {
                                        var withdrawTx = new Transactions(Guid.NewGuid().ToString(), withdrawAccountId, "Withdraw", withdrawAmount, DateTime.Now);
                                        Transactions.AddTransactionsToDatabase(conn, withdrawTx);
                                        Console.WriteLine("Withdrawal successful!");
                                    }
                                    else Console.WriteLine("Withdrawal failed. Check Account ID and Balance.");
                                }
                                else Console.WriteLine("Invalid amount.");
                            }
                            else Console.WriteLine("Invalid Account ID.");
                            break;

                        case "3":
                            Console.WriteLine("\nYou chose Transfer!");
                            Console.Write("Enter your Account ID: ");
                            if (!int.TryParse(Console.ReadLine(), out int fromAccount)) { Console.WriteLine("Invalid ID."); break; }

                            Console.Write("Enter recipient Account ID: ");
                            if (!int.TryParse(Console.ReadLine(), out int toAccount)) { Console.WriteLine("Invalid ID."); break; }

                            Console.Write("Enter amount: ");
                            if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0) { Console.WriteLine("Invalid amount."); break; }

                            using (var transaction = conn.BeginTransaction())
                            {
                                try
                                {
                                    if (Account.UpdateBalance(conn, fromAccount, -amount, transaction))
                                    {
                                        if (Account.UpdateBalance(conn, toAccount, amount, transaction))
                                        {
                                            var txOut = new Transactions(Guid.NewGuid().ToString(), fromAccount, "Transfer Out", amount, DateTime.Now);
                                            var txIn = new Transactions(Guid.NewGuid().ToString(), toAccount, "Transfer In", amount, DateTime.Now);

                                            Transactions.AddTransactionsToDatabase(conn, txOut, transaction);
                                            Transactions.AddTransactionsToDatabase(conn, txIn, transaction);

                                            transaction.Commit();
                                            Console.WriteLine("Transfer successful!");
                                        }
                                        else
                                        {
                                            transaction.Rollback();
                                            Console.WriteLine("Transfer failed. Recipient account not found.");
                                        }
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        Console.WriteLine("Transfer failed. Insufficient funds or invalid source account.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    Console.WriteLine($"Error during transfer: {ex.Message}");
                                }
                            }
                            break;

                        case "4":
                            Console.WriteLine("\nYou chose Transaction History!");
                            Console.Write("Enter Account ID: ");
                            if (int.TryParse(Console.ReadLine(), out int histAccountId))
                            {
                                Transactions.ViewTransactions(conn, histAccountId);
                            }
                            else Console.WriteLine("Invalid Account ID.");
                            break;

                        default:
                            Console.WriteLine("\nInvalid choice! Try again.");
                            break;
                    }
                }

                Console.WriteLine("Thank you for choosing our bank.");
                Console.WriteLine("\nPress any key to exit...");
            }
        }
    }
}
