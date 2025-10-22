using RobotArmServer.Models;
using System;
using System.Collections.Generic;

namespace RobotArmServer.Services
{
    public class RobotArmService : IRobotArmService
    {
        private static readonly RobotArm _arm = new RobotArm();

        private readonly Dictionary<string, string[]> _permissions = new()
        {
            { "Client1", new[] { "LEFT", "RIGHT", "UP", "DOWN", "ROTATE" } },
            { "Client2", new[] { "LEFT", "RIGHT", "UP", "DOWN" } },
            { "Client3", new[] { "ROTATE" } }
        };

        public string ExecuteCommand(string clientName, string command)
        {
            string cmd = command.ToUpper();
            if (!_permissions.ContainsKey(clientName) )
                return "Unknown client.";

            if (Array.IndexOf(_permissions[clientName], cmd) == -1) 
                return "Permission denied.";

            bool success = cmd switch
            {
                "LEFT" => _arm.MoveLeft(),
                "RIGHT" => _arm.MoveRight(),
                "UP" => _arm.MoveUp(),
                "DOWN" => _arm.MoveDown(),
                "ROTATE" => _arm.Rotate(),
                _ => false,
            };

            if (!success)
                return "Out of bounds.";

            return $"Success. {_arm}";
        }
    }
}