using System;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Mysqlx.Expr;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace BankingSystem
{
    class DatabaseConnection
    {
        private string connectionString = "server=127.0.0.1;user=root;database=BankingSystem;port=3306;password=Guddusah.";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }       
    }

    class Accounts
    {
        
    }

    class Transactions
    {

    }

    class Users
    {
        // properties
        public string userID { get; set; }
        public string fullname { get; set; }
        public string email { get; set; }
        public string phone { get; set; }

        public string password { get; set; }
        public DateTime createdAt { get; set; }

        // Constructor
        public Users(string userID, string fullname, string email,string phone, string password, DateTime createdAt)
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
                // SQL query to insert a new user into the Users table
                string query = "INSERT INTO Users (FullName, Email, Password, CreatedAt) " +
                               "VALUES (@FullName, @Email, @Password, @CreatedAt)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", newUser.fullname);
                cmd.Parameters.AddWithValue("@Email", newUser.email);
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
            var conn = new DatabaseConnection().GetConnection();
            conn.Open();

            // Ask user for name, email, and password
            Console.Write("Enter your name: ");
            string fullname = Console.ReadLine()!;

            Console.Write("Enter your email: ");
            string email = Console.ReadLine()!;

            Console.Write("Enter your Phone number: ");
            string phone = Console.ReadLine()!;

            Console.Write("Enter your password: ");
            string password = Console.ReadLine()!;

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

            conn.Close();

            Console.WriteLine("User added successfully!");
            Console.ReadLine();
        }


    }
}
