using RobotArmServer.Data;
using RobotArmServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;

namespace RobotArmServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class RobotArmService : IRobotArmService
    {
        private readonly DatabaseHelper _dbHelper = new();
        private readonly RobotArm _arm = new();
        private readonly object _armLock = new();
        private readonly CommandQueue _requests = new();
        private readonly ConcurrentDictionary<string, IRobotArmCallback> _callbacks = new();
        private readonly SessionManager _sessionManager = new();

        private static readonly Dictionary<string, string[]> Permissions = new()
        {
            { "Client1", new[] { "LEFT", "RIGHT", "UP", "DOWN", "ROTATE" } },
            { "Client2", new[] { "LEFT", "RIGHT", "UP", "DOWN" } },
            { "Client3", new[] { "ROTATE" } }
        };

        public RobotArmService()
        {
            try
            {
                _dbHelper.InitializeDatabase();
                Debug.WriteLine("Database initialized successfully.");
                InitFromDb();

                // Start the command processor
                Task.Factory.StartNew(ProcessCommands, TaskCreationOptions.LongRunning);
                Debug.WriteLine("Command processor task started.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database init failed: {ex.Message}");
            }
        }

        private void InitFromDb()
        {
            try
            {
                var lastState = _dbHelper.GetLastArmState();
                if (lastState != null)
                {
                    lock (_armLock)
                    {
                        _arm.InitFromDb(lastState);
                    }

                    Debug.WriteLine(
                        $"Initialized arm state from DB: ({lastState.X},{lastState.Y}) angle={lastState.Angle}");
                }
                else
                {
                    Debug.WriteLine("No previous successful state found in DB - using default initial state.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize arm state from DB: {ex.Message}");
            }
        }

        public string RegisterClient(string clientName)
        {
            try
            {
                Debug.WriteLine($"RegisterClient called for {clientName}");
                var sessionId = Guid.NewGuid().ToString();

                if (!_sessionManager.TryRegisterClient(clientName, sessionId))
                {
                    Debug.WriteLine($"Client {clientName} already connected");
                    return "ERROR:Client already connected";
                }

                var callback = OperationContext.Current.GetCallbackChannel<IRobotArmCallback>();
                _callbacks[clientName] = callback;
                Debug.WriteLine($"Client {clientName} registered successfully");

                return $"Registered:{sessionId}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RegisterClient error: {ex.Message}");
                return $"ERROR:{ex.Message}";
            }
        }

        public string UnregisterClient(string clientName)
        {
            Debug.WriteLine($"UnregisterClient called for {clientName}");
            _sessionManager.UnregisterClient(clientName, "");
            _callbacks.TryRemove(clientName, out _);
            return "Unregistered";
        }

        public Task<CommandResult> ExecuteCommand(string clientName, string encryptedCommand)
        {
            string command;
            try
            {
                // Decrypt using private key
                command = RsaEncryptionHelper.Decrypt(encryptedCommand);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new CommandResult($"Decryption failed: {ex.Message}", null));
            }

            var tcs = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new CommandRequest
            {
                ClientName = clientName,
                Command = command,
                Timestamp = DateTime.Now,
                CompletionSource = tcs
            };

            _requests.Enqueue(request);
            return tcs.Task;
        }



        public RobotArmState GetCurrentState()
        {
            lock (_armLock)
            {
                return new RobotArmState(_arm.X, _arm.Y, _arm.Angle);
            }
        }

        private void ProcessCommands()
        {
            Debug.WriteLine("ProcessCommands task started");
            while (true)
            {
                try
                {
                    Debug.WriteLine("Waiting for command...");
                    var request = _requests.Dequeue(); // Blocking call
                    Debug.WriteLine($"Dequeued request: client={request.ClientName}, cmd={request.Command}");

                    string resultMessage;
                    RobotArmState state;
                    lock (_armLock)
                    {
                        resultMessage = ProcessCommandInternal(request.ClientName, request.Command);
                        // build current state
                        state = new RobotArmState(_arm);
                    }

                    Debug.WriteLine($"Command processed: client={request.ClientName}, result={resultMessage}");
                    // set result with typed DTO
                    request.CompletionSource.SetResult(new CommandResult(resultMessage, state));
                    Debug.WriteLine($"SetResult completed for client={request.ClientName}");

                    // Notify all clients with typed callback args
                    NotifyAllClients();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in ProcessCommands: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private string ProcessCommandInternal(string clientName, string command)
        {
            string cmdUpper = command.ToUpper();
            string result;

            if (!Permissions.TryGetValue(clientName, out var permission))
            {
                result = "Unknown client";
            }
            else if (Array.IndexOf(permission, cmdUpper) == -1)
            {
                result = "Permission denied";
            }
            else
            {
                bool success = cmdUpper switch
                {
                    "LEFT" => _arm.MoveLeft(),
                    "RIGHT" => _arm.MoveRight(),
                    "UP" => _arm.MoveUp(),
                    "DOWN" => _arm.MoveDown(),
                    "ROTATE" => _arm.Rotate(),
                    _ => false,
                };

                result = success ? "Success" : "Out of bounds";
            }

            try
            {
                _dbHelper.LogOperation(clientName, command, result, _arm.X, _arm.Y, _arm.Angle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log operation: {ex.Message}");
            }

            return $"{result}. Position=({_arm.X},{_arm.Y}), Rotation={_arm.Angle}°";
        }

        private void NotifyAllClients()
        {
            int x, y, angle;
            lock (_armLock)
            {
                x = _arm.X;
                y = _arm.Y;
                angle = _arm.Angle;
            }

            var state = new RobotArmState(x, y, angle);

            var deadClients = new List<string>();

            foreach (var kvp in _callbacks)
            {
                try
                {
                    Debug.WriteLine($"Notifying client {kvp.Key} of state change");
                    kvp.Value.OnStateChanged(state);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to notify {kvp.Key}: {ex.Message}");
                    deadClients.Add(kvp.Key);
                }
            }

            // Clean up dead callbacks
            foreach (var clientName in deadClients)
            {
                _callbacks.TryRemove(clientName, out _);
                Debug.WriteLine($"Removed dead callback for {clientName}");
            }
        }
    }
}