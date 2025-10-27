using System.Runtime.Serialization;

namespace RobotArmServer.Models
{
    [DataContract]
    public class CommandResult
    {
        [DataMember] public string ResultMessage { get; set; }

        [DataMember] public RobotArmState State { get; set; }

        public CommandResult()
        {
        }

        public CommandResult(string message, RobotArmState state)
        {
            ResultMessage = message;
            State = state;
        }

        public override string ToString()
        {
            return $"{ResultMessage}. {State}";
        }
    }
}