-- Create the BankingSystem database
CREATE DATABASE BankingSystem;

-- Use the BankingSystem database
USE BankingSystem;

-- Create the Users table first (it will be referenced by Accounts)
CREATE TABLE Users (
    UserID INT AUTO_INCREMENT PRIMARY KEY,   -- Automatically incremented UserID
    FullName VARCHAR(100),                    -- Full name of the user
    Email VARCHAR(100) UNIQUE,                -- Email address (unique to each user)
    Phone INT,                                -- Phone number (you might want to add validation for format)
    Password VARCHAR(255),                    -- Password (can be hashed later)
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP  -- Timestamp when the user is created (default to current time)
);

-- Create the Accounts table next (which references Users via UserID)
CREATE TABLE Accounts (
    AccountID INT AUTO_INCREMENT PRIMARY KEY,   -- Automatically incremented AccountID
    HolderName VARCHAR(100),                    -- Name of the account holder
    Balance DECIMAL(10,2) DEFAULT 0.00,         -- Balance of the account (defaults to 0)
    AccountType ENUM('Savings', 'Current'),     -- Account type (Savings or Current)
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP, -- Timestamp when the account is created (default to current time)
    Status VARCHAR(20) DEFAULT 'Active',       -- Account status (Active by default)
    UserID INT,                                -- UserID column to reference Users table
    FOREIGN KEY (UserID) REFERENCES Users(UserID)  -- Foreign key referencing the Users table
);

-- Create the Transactions table
CREATE TABLE Transactions (
    TransactionID INT AUTO_INCREMENT PRIMARY KEY,   -- Automatically incremented TransactionID
    AccountID INT,                                  -- AccountID referencing Accounts table
    Type ENUM('Deposit', 'Withdraw', 'Transfer'),   -- Transaction type (Deposit, Withdraw, or Transfer)
    Amount DECIMAL(10,2),                           -- Transaction amount
    Description TEXT,                               -- Description of the transaction
    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP, -- Timestamp of when the transaction happens
    FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID)  -- Foreign key referencing the Accounts table
);

-- Create the Admins table
CREATE TABLE Admins (
    AdminID INT AUTO_INCREMENT PRIMARY KEY,   -- Automatically incremented AdminID
    Username VARCHAR(50) UNIQUE,               -- Username of the admin (unique)
    Password VARCHAR(255)                      -- Admin password (hashed later)
);
 

