using System.ServiceModel;

namespace RobotArmServer.Services
{
    [ServiceContract]
    public interface IRobotArmService
    {
        [OperationContract]
        string ExecuteCommand(string clientName, string command);
    }
}