using System;
using System.Data.SQLite;
using System.IO;
using System.Web;

namespace RobotArmServer.Data
{
    public class DatabaseHelper
    {
        public static void Log(string message)
        {
            File.AppendAllText(HttpContext.Current.Server.MapPath("~/App_Data/log.txt"),
                                $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        public static void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(HttpContext.Current.Server.MapPath("~/App_Data/appdata.db")))
                {
                    SQLiteConnection.CreateFile(HttpContext.Current.Server.MapPath("~/App_Data/appdata.db"));
                }
                using var connection = GetConnection();
                string createLogsTableQuery = @"
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
                using var command = new SQLiteCommand(connection);
                command.CommandText = createLogsTableQuery;
                command.ExecuteNonQuery();
                Log("Database initialized successfully.");
            } catch (Exception ex)
            {
                Log("Database init failed: " + ex.Message);
                throw;
            }
        }

        public static SQLiteConnection GetConnection()
        {
            string dbPath = HttpContext.Current.Server.MapPath("~/App_Data/appdata.db");
            var connection = new SQLiteConnection($"Data Source={dbPath}");
            connection.Open();
            return connection;
        }

        public void LogOperation(string clientName, string command, string result, int x, int y, int rotation)
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
        }

    }
}