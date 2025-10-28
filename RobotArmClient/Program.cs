using RobotArmClient.RobotArmService;
using System.ServiceModel;
using System.Text;

namespace RobotArmClient
{
    internal class Program : IRobotArmServiceCallback
    {
        private static int _currentX = 2;
        private static int _currentY = 2;
        private static int _currentAngle;
        private static string? _clientName;
        private static string[] _allowedCommands = [];

        public static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Select Client Type:");
            Console.WriteLine("1 - Client1 (All permissions, highest priority)");
            Console.WriteLine("2 - Client2 (Movement only)");
            Console.WriteLine("3 - Client3 (Rotation only)");
            Console.Write("Enter choice (1-3): ");

            var choice = Console.ReadLine() ?? "";
            _clientName = choice switch
            {
                "1" => "Client1",
                "2" => "Client2",
                "3" => "Client3",
                _ => null
            };

            if (_clientName == null)
            {
                Console.WriteLine("Invalid choice!");
                Console.ReadKey();
                return;
            }

            _allowedCommands = _clientName switch
            {
                "Client1" => ["LEFT", "RIGHT", "UP", "DOWN", "ROTATE"],
                "Client2" => ["LEFT", "RIGHT", "UP", "DOWN"],
                "Client3" => ["ROTATE"],
                _ => _allowedCommands
            };

            try
            {
                var instanceContext = new InstanceContext(new Program());
                var binding = new WSDualHttpBinding();
                var address = new EndpointAddress("http://localhost:54319/RobotArmService.svc");
                var factory = new DuplexChannelFactory<IRobotArmService>(instanceContext, binding, address);
                var client = factory.CreateChannel();

                ((IClientChannel)client).Open();

                Console.WriteLine($"\nConnecting as {_clientName}...");
                var response = client.RegisterClientAsync(_clientName).GetAwaiter().GetResult();
                if (response.StartsWith("ERROR"))
                {
                    Console.WriteLine($"Registration failed: {response}");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Connected successfully!");

                // Get initial state
                var state = client.GetCurrentStateAsync().GetAwaiter().GetResult();
                UpdateState(state);

                // Command loop
                while (true)
                {
                    ConsoleUi.DrawGrid(_currentX, _currentY, _currentAngle);
                    ConsoleUi.ShowCommands(_allowedCommands);

                    var command = Console.ReadLine()?.ToUpper() ?? "";

                    if (command == "EXIT")
                    {
                        client.UnregisterClientAsync(_clientName).GetAwaiter().GetResult();
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    try
                    {
                        // Encrypt the command before sending
                        var encryptedCommand = ClientRsaEncryptionHelper.Encrypt(command); // returns Base64 string
                        if (string.IsNullOrEmpty(encryptedCommand))
                        {
                            Console.WriteLine("Encryption failed!");
                            continue;
                        }


                        var commandResult = client.ExecuteCommandAsync(_clientName, encryptedCommand).GetAwaiter().GetResult();
                        Console.WriteLine($"\nServer response: {commandResult.ResultMessage}");
                        if (commandResult.State != null)
                        {
                            Console.WriteLine(
                                $"Position: ({commandResult.State.X},{commandResult.State.Y}), Rotation={commandResult.State.Angle}°");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nError: {ex.Message}");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                }

                // Close the channel/factory properly
                try
                {
                    ((IClientChannel)client).Close();
                    factory.Close();
                }
                catch
                {
                    ((IClientChannel)client).Abort();
                    factory.Abort();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        public void OnStateChanged(RobotArmState state)
        {
            UpdateState(state);
            ConsoleUi.DrawGrid(_currentX, _currentY, _currentAngle);
            ConsoleUi.ShowCommands(_allowedCommands);
        }

        private static void UpdateState(RobotArmState state)
        {
            _currentX = state.X;
            _currentY = state.Y;
            _currentAngle = state.Angle;
        }
    }
}
