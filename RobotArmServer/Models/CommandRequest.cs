using System;
using System.Threading;
using System.Threading.Tasks;

namespace RobotArmServer.Models
{
    public class CommandRequest : IComparable<CommandRequest>
    {
        private static long _sequenceCounter;

        public string ClientName { get; set; }
        public string Command { get; set; }
        public DateTime Timestamp { get; set; }
        public TaskCompletionSource<CommandResult> CompletionSource { get; set; }
        private long Sequence { get; }

        public CommandRequest()
        {
            Sequence = Interlocked.Increment(ref _sequenceCounter);
        }

        private int Priority => ClientName switch
        {
            "Client1" => 1,
            "Client2" => 2,
            "Client3" => 2,
            _ => 999
        };

        public int CompareTo(CommandRequest other)
        {
            // First, compare by priority
            int priorityCompare = Priority.CompareTo(other.Priority);
            if (priorityCompare != 0) return priorityCompare;

            // Then by timestamp
            int timeCompare = Timestamp.CompareTo(other.Timestamp);

            // Finally by sequence (guarantees uniqueness)
            return timeCompare != 0 ? timeCompare : Sequence.CompareTo(other.Sequence);
        }
    }
}