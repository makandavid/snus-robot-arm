using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using RobotArmServer.Models;

namespace RobotArmServer.Services
{
    public class CommandQueue
    {
        private readonly SortedSet<CommandRequest> _queue = new();
        private readonly object _lock = new();
        private readonly AutoResetEvent _signal = new(false);

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        public void Enqueue(CommandRequest request)
        {
            lock (_lock)
            {
                if (!_queue.Add(request))
                {
                    Debug.WriteLine(
                        $"WARNING: Duplicate command request detected for {request.ClientName}");
                }
                _signal.Set();
            }
        }

        public CommandRequest Dequeue()
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        var request = _queue.Min;
                        _queue.Remove(request);
                        return request;
                    }
                }

                _signal.WaitOne();
            }
        }
    }
}