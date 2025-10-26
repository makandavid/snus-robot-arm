namespace RobotArmClient
{
    public static class ConsoleUi
    {
        public static void DrawGrid(int armX, int armY, int angle)
        {
            Console.Clear();
            Console.WriteLine("=== Robot Arm Control ===");
            Console.WriteLine($"Current Position: ({armX}, {armY})");
            Console.WriteLine($"Current Rotation: {angle}°");

            for (var y = 0; y < 5; ++y)
            {
                for (var x = 0; x < 5; ++x)
                {
                    if (x == armX && y == armY)
                    {
                        Console.Write($"[{GetArmSymbol(angle)}] ");
                    }
                    else
                    {
                        Console.Write("[ ] ");
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine("\nAvailable Commands:");
        }

        public static void ShowCommands(string[] allowedCommands)
        {
            foreach (var cmd in allowedCommands)
            {
                Console.WriteLine($"  - {cmd}");
            }
            Console.WriteLine("  - EXIT (to quit)");
            Console.Write("\nEnter command: ");
        }

        private static char GetArmSymbol(int angle)
        {
            return angle switch
            {
                0 => '→',
                90 => '↓',
                180 => '←',
                270 => '↑',
                _ => 'R'
            };
        }
    }
}