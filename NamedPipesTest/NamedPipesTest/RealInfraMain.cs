//using ServiceStudio.Presenter.ProcessCommunication;
//using System;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace NamedPipesTest {
//    class RealInfraMain {

//        static void Main(string[] _) {
//            Console.WriteLine("Starting 'RealInfra' Test\n");

//            // Launch a bunch of dummy processes -- Notepad process will do, for this example
//            Process.Start("notepad.exe");
//            Process.Start("notepad.exe");
//            Process.Start("notepad.exe");
//            Process.Start("notepad.exe");

//            // Gather relevant (dummy) processes and their PIDs
//            var relevantProcesses = ProcessUtils.FindRelevantProcesses("notepad");

//            foreach (var process in relevantProcesses) {
//                Console.WriteLine(ProcessUtils.GetCanonicalProcessName(process));
//            }

//            var cts = new CancellationTokenSource();
//            var po = new ParallelOptions {
//                CancellationToken = cts.Token
//            };

//            var nameOfChosenProcess = string.Empty;
//            var timeout = TimeSpan.FromMilliseconds(5000);
//            bool lockTaken = false;

//            var lockObj = new object();

//            // All servers are already listening:
//            relevantProcesses
//                .ToList()
//                .ForEach(p => Task.Run(() => {
//                    var server = new NamedPipeServer(
//                        namedPipeServerProvider: new NamedPipeServerProvider(),
//                        pipeName: ProcessUtils.GetCanonicalProcessName(p));

//                    server.MessageReceived += (messageReceived) => {
//                        return messageReceived switch {
//                            "ping" => "YES",
//                            "reuse" => "reusing...",
//                            _ => string.Empty,
//                        };
//                        ;
//                    };

//                }));

//            try {
//                Parallel.For(fromInclusive: 0, toExclusive: relevantProcesses.Length, parallelOptions: po, async (i, state) => {
//                    var pipeName = ProcessUtils.GetCanonicalProcessName(relevantProcesses[i]);

//                    var pingTaskStream = new NamedPipeClient(pipeName);

//                    // Ping server and wait for a response
//                    var pingResponse = await pingTaskStream.SendMessageAndWaitResponse("ping", 3000, 3000);

//                    if (pingResponse == "YES") {
//                        try {
//                            if (Monitor.TryEnter(lockObj, timeout)) {
//                                // If a process has responded positively to the ping, then 
//                                // we can cancel all other operations in the loop and break out of it.
//                                Console.WriteLine($"Found 'YES' response from pipe '{pipeName}'\n");
//                                //Console.WriteLine($"@{pipeName} - Should cancel? {state.ShouldExitCurrentIteration}\n");
//                                nameOfChosenProcess = pipeName;

//                                // Renew the client stream...
//                                pingTaskStream.Dispose();
//                                pingTaskStream = new NamedPipeClient(pipeName);

//                                await pingTaskStream.SendMessage("reuse", 1000);
//                                Console.Write("Sent Reuse to server!\n");

//                                state.Stop();
//                                cts.Cancel();
//                                return;
//                            }
//                        } finally {
//                            // Ensure that the lock is released.
//                            if (lockTaken) {
//                                Monitor.Exit(lockObj);
//                            }
//                        }
//                    }
//                });
//            } catch {
//                Console.WriteLine("Loop exited (one of the threads canceled it!)\n");
//                Console.WriteLine($"Chosen process: {nameOfChosenProcess}");
//            } finally {
//                cts.Dispose();
//            }

//            Console.WriteLine("Out of the loop");
//        }
//    }
//}
