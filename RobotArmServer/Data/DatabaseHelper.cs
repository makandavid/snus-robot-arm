using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using RobotArmServer.Models;

namespace RobotArmServer.Data
{
    public class DatabaseHelper
    {
        private readonly string _dbPath;
        private readonly string _logPath;

        public DatabaseHelper()
        {
            // Get the base directory where the service is running
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var appDataPath = Path.Combine(baseDir, "App_Data");
            _dbPath = Path.Combine(appDataPath, "appdata.db");
            _logPath = Path.Combine(appDataPath, "log.txt");

            // Ensure App_Data directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        public void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(_dbPath))
                {
                    SQLiteConnection.CreateFile(_dbPath);
                    Log($"Created database file at {_dbPath}");
                }

                using var connection = GetConnection();
                const string createLogsTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Logs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ClientName TEXT NOT NULL,
                        Command TEXT NOT NULL,
                        Result TEXT NOT NULL,
                        X INTEGER NOT NULL,
                        Y INTEGER NOT NULL,
                        Rotation INTEGER NOT NULL,
                        Timestamp TEXT NOT NULL
                    );";
                using var command = new SQLiteCommand(createLogsTableQuery, connection);
                command.ExecuteNonQuery();
                Log("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Log($"Database init failed: {ex.Message}");
                Debug.WriteLine($"Database init failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private SQLiteConnection GetConnection()
        {
            var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            return connection;
        }

        public RobotArmState GetLastArmState()
        {
            try
            {
                const string sql = @"
                SELECT X, Y, Rotation
                FROM Logs
                WHERE Result = 'Success'
                ORDER BY Timestamp DESC
                LIMIT 1
                ";

                using var conn = GetConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int x = Convert.ToInt32(reader["X"]);
                    int y = Convert.ToInt32(reader["Y"]);
                    int angle = Convert.ToInt32(reader["Rotation"]);
                    return new RobotArmState(x, y, angle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetLastArmState failed: {ex.Message}");
                Log($"GetLastArmState failed: {ex.Message}");
            }

            return null;
        }

        public void LogOperation(string clientName, string command, string result, int x, int y, int rotation)
        {
            try
            {
                using var connection = GetConnection();
                using var cmd = new SQLiteCommand(
                    "INSERT INTO Logs (ClientName, Command, Result, X, Y, Rotation, Timestamp) VALUES (@client, @cmd, @res, @x, @y, @rot, @time)",
                    connection);

                cmd.Parameters.AddWithValue("@client", clientName);
                cmd.Parameters.AddWithValue("@cmd", command);
                cmd.Parameters.AddWithValue("@res", result);
                cmd.Parameters.AddWithValue("@x", x);
                cmd.Parameters.AddWithValue("@y", y);
                cmd.Parameters.AddWithValue("@rot", rotation);
                cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
                Debug.WriteLine($"Logged operation: {clientName} - {command} - {result}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogOperation failed: {ex.Message}\n{ex.StackTrace}");
                Log($"LogOperation failed: {ex.Message}");
                // Don't throw - logging failures shouldn't crash the service
            }
        }
    }
}