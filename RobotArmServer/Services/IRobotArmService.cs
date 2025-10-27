using System.ServiceModel;
using System.Threading.Tasks;
using RobotArmServer.Models;

namespace RobotArmServer.Services
{
    [ServiceContract(CallbackContract = typeof(IRobotArmCallback))]
    public interface IRobotArmService
    {
        [OperationContract]
        string RegisterClient(string clientName);

        [OperationContract]
        string UnregisterClient(string clientName);

        [OperationContract]
        Task<CommandResult> ExecuteCommand(string clientName, string command);

        [OperationContract]
        RobotArmState GetCurrentState();
    }
}