using ServiceStudio.Presenter.ProcessCommunication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipesTest {
    class WhenAnyTest {

        static async Task Main(string[] _) {
            Console.WriteLine("Starting 'WhenAny' Test\n");

            // Launch a bunch of dummy processes -- Notepad process will do, for this example
            Process.Start("notepad.exe");
            Process.Start("notepad.exe");
            Process.Start("notepad.exe");
            Process.Start("notepad.exe");

            // Gather relevant (dummy) processes and their PIDs
            var relevantProcesses = ProcessUtils.FindRelevantProcesses("notepad");
            Console.WriteLine($"Number of relevant processes found: {relevantProcesses.Length}\n");

            Process availableProcess = null;

            // All servers are already listening:
            var relevantProcessesList = relevantProcesses.ToList();

            string nameOfChosenProcess = null;

            var timeout = TimeSpan.FromMilliseconds(5000);
            bool lockTaken = false;

            var lockObj = new object();

            // All servers are already listening:
            relevantProcesses
                .ToList()
                .ForEach(p => Task.Run(() => {
                    var server = new NamedPipeServer(
                        namedPipeServerProvider: new NamedPipeServerProvider(),
                        pipeName: ProcessUtils.GetCanonicalProcessName(p));

                    server.MessageReceived += (messageReceived) => {
                        return messageReceived switch {
                            "ping" => "YES",
                            "reuse" => "reusing...",
                            _ => string.Empty,
                        };
                        ;
                    };

                }));

            // Create the Tasks that will ping each server
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var pingTasks = new Dictionary<Process, Task>(relevantProcessesList.Count());

            foreach (var process in relevantProcesses) {
                var task = Task.Run(async () => {

                    var pipeName = ProcessUtils.GetCanonicalProcessName(process);
                    var pingTaskStream = new NamedPipeClient(pipeName);
                    var pingResponse = await pingTaskStream.SendMessageAndWaitResponse("ping", 3000, 3000);

                    if (pingResponse == "YES") {
                        try {
                            if (Monitor.TryEnter(lockObj, timeout)) {
                                // If a process has responded positively to the ping, then 
                                // we can cancel all other operations in the loop and break out of it.
                                Console.WriteLine($"Found 'YES' response from pipe '{pipeName}'\n");
                                //Console.WriteLine($"@{pipeName} - Should cancel? {state.ShouldExitCurrentIteration}\n");
                                nameOfChosenProcess = pipeName;
                                availableProcess = process;

                                // Renew the client stream...
                                pingTaskStream.Dispose();
                                pingTaskStream = new NamedPipeClient(pipeName);

                                await pingTaskStream.SendMessage("reuse", 1000);
                                Console.Write("Sent Reuse to server!\n");

                                //state.Stop();
                                cancellationTokenSource.Cancel();
                                return;
                            }
                        } finally {
                            // Ensure that the lock is released.
                            if (lockTaken) {
                                Monitor.Exit(lockObj);
                            }
                        }
                    }
                }, cancellationTokenSource.Token);

                pingTasks.Add(process, task);
            }

            await Task.WhenAny(pingTasks.Values);

            //while (pingTasks.Any()) {
            //    var completedTask = await Task.WhenAny(pingTasks.Values);
            //    var pingResult = await completedTask;
            //    var correspondingProcess = pingTasks.FirstOrDefault(p => p.Value == completedTask).Key;

            //    if (pingResult == "YES") {
            //        try {
            //            if (Monitor.TryEnter(lockObj, timeout)) {
            //                // If a process has responded positively to the ping, then 
            //                // we can cancel all other operations in the loop and break out of it.
            //                Console.WriteLine($"Found 'YES' response from pipe '{pipeName}'\n");
            //                //Console.WriteLine($"@{pipeName} - Should cancel? {state.ShouldExitCurrentIteration}\n");
            //                nameOfChosenProcess = pipeName;

            //                // Renew the client stream...
            //                pingTaskStream.Dispose();
            //                pingTaskStream = new NamedPipeClient(pipeName);

            //                await pingTaskStream.SendMessage("reuse", 1000);
            //                Console.Write("Sent Reuse to server!\n");

            //                state.Stop();
            //                cts.Cancel();
            //                return;
            //            }
            //        } finally {
            //            // Ensure that the lock is released.
            //            if (lockTaken) {
            //                Monitor.Exit(lockObj);
            //            }
            //        }


            //        cancellationTokenSource.Cancel();
            //        availableProcess = correspondingProcess;
            //        break;
            //    } else {
            //        pingTasks.Remove(correspondingProcess);
            //    }
            //}

            Console.WriteLine($"Chosen process (i.e., first process to reply 'YES' to the ping): '{ProcessUtils.GetCanonicalProcessName(availableProcess)}'\n");

            // Now that we have an available process, send it a REUSE message
            //var pipeName = ProcessUtils.GetCanonicalProcessName(availableProcess);

            //new PipeStream()

            //if (!namedPipeClient.pipeClient.IsConnected) {
            //    namedPipeClient.pipeClient.Connect();
            //    Console.WriteLine($"Now connected to '{nameOfChosenProcess}'...\n");
            //}

            //namedPipeClient.writer.WriteLine(Messages.ReuseMessage);
            //namedPipeClient.writer.Flush();
            //namedPipeClient.pipeClient.WaitForPipeDrain();

            //Console.WriteLine($"Sent REUSE message to '{nameOfChosenProcess}', terminating...\n");
        }
    }
}
