using RobotArmServiceReference;

namespace RobotArmClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting to RobotArmService...");

            try
            {
                var client = new RobotArmServiceClient();
                var response = client.ExecuteCommand("Client1", "LEFT");
                Console.WriteLine($"Service response: {response}");
                response = client.ExecuteCommand("Client1", "UP");
                Console.WriteLine($"Service response: {response}");
                response = client.ExecuteCommand("Client3", "ROTATE");
                Console.WriteLine($"Service response: {response}");
                response = client.ExecuteCommand("Client12", "RIGHT");
                Console.WriteLine($"Service response: {response}");
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
