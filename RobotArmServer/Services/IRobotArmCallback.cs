using System.ServiceModel;
using RobotArmServer.Models;

namespace RobotArmServer.Services
{
    [ServiceContract]
    public interface IRobotArmCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnStateChanged(RobotArmState state);
    }
}