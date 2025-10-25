using RobotArmServer.Data;
using RobotArmServer.Models;
using System;
using System.Collections.Generic;

namespace RobotArmServer.Services
{
    public class RobotArmService : IRobotArmService
    {
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private static readonly RobotArm _arm = new RobotArm();

        private readonly Dictionary<string, string[]> _permissions = new()
        {
            { "Client1", new[] { "LEFT", "RIGHT", "UP", "DOWN", "ROTATE" } },
            { "Client2", new[] { "LEFT", "RIGHT", "UP", "DOWN" } },
            { "Client3", new[] { "ROTATE" } }
        };

        static RobotArmService()
        {
            try
            {
                DatabaseHelper.InitializeDatabase();
                System.Diagnostics.Debug.WriteLine("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database init failed: {ex.Message}");
            }
        }

        public string ExecuteCommand(string clientName, string command)
        {
            string cmdUpper = command.ToUpper();
            string result;

            if (!_permissions.ContainsKey(clientName))
            {
                result = "Unknown client";
            }
            else if (Array.IndexOf(_permissions[clientName], cmdUpper) == -1)
            {
                result = "Permission denied";
            }
            else
            {
                bool success = cmdUpper switch
                {
                    "LEFT" => _arm.MoveLeft(),
                    "RIGHT" => _arm.MoveRight(),
                    "UP" => _arm.MoveUp(),
                    "DOWN" => _arm.MoveDown(),
                    "ROTATE" => _arm.Rotate(),
                    _ => false,
                };

                result = success ? $"Success" : "Out of bounds";
            }

            _dbHelper.LogOperation(clientName, command, result, _arm.X, _arm.Y, _arm.Angle);

            return $"{result}. Position=({_arm.X},{_arm.Y}), Rotation={_arm.Angle}°";
        }

    }
}