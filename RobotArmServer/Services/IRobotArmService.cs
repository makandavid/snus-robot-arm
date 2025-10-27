using RobotArmServer.Models;
using System.ServiceModel;
using System.Threading.Tasks;

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
        Task<CommandResult> ExecuteCommand(string clientName, string encryptedCommand);

        [OperationContract]
        RobotArmState GetCurrentState();
    }
}