using System;
using System.Threading.Tasks;

namespace RobotArmServer.Models
{
    public class CommandRequest : IComparable<CommandRequest>
    {
        public string ClientName { get; set; }
        public string Command { get; set; }
        public DateTime Timestamp { get; set; }
        public TaskCompletionSource<CommandResult> CompletionSource { get; set; }
        
        private int Priority => ClientName switch
        {
            "Client1" => 1,
            "Client2" => 2,
            "Client3" => 2,
            _ => 999
        };
        
        public int CompareTo(CommandRequest other)
        {
            int priorityCompare = Priority.CompareTo(other.Priority);
            return priorityCompare != 0 ? priorityCompare : Timestamp.CompareTo(other.Timestamp);
        }        
    }
}