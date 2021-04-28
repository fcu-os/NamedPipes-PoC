using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipesTest {
    class Program {

        internal const string PingMessage = "PING";
        internal const string ReuseMessage = "Login Info goes here...";

        static void Main(string[] _) {
            // Launch a bunch of dummy processes -- Notepad process will do, for this example
            Process.Start("notepad.exe");
            Process.Start("notepad.exe");
            Process.Start("notepad.exe");

            // Gather relevant (dummy) processes and their PIDs
            var relevantProcess = ProcessUtils.FindRelevantProcesses("notepad");
            Console.WriteLine($"Number of relevant processes found: {relevantProcess.Length}\n");

            var cts = new CancellationTokenSource();
            var po = new ParallelOptions {
                CancellationToken = cts.Token
            };

            var nameOfChosenProcess = string.Empty;

            // All servers are already listening:
            relevantProcess
                .ToList()
                .ForEach(p => Task.Run(() => NamedPipeServer.Launch(pipeName: ProcessUtils.GetCanonicalProcessName(p))));

            // Ping all servers:
            try {
                Parallel.For(fromInclusive: 0, toExclusive: relevantProcess.Length, parallelOptions: po, (i, state) => {
                    var canonicalProcessName = ProcessUtils.GetCanonicalProcessName(relevantProcess[i]);

                    // Ping server and wait for a response
                    var (pipeName, pingResponse) = NamedPipeClient.Launch(pipeName: canonicalProcessName);

                    // If there has already been a "YES" response to a ping from some iteration,
                    // the parallel for will be canceled and all other iterations that hit this point will
                    // be abandoned at this point
                    if (state.ShouldExitCurrentIteration) {
                        Console.WriteLine($"@{pipeName} - Should exit current iteration? {state.ShouldExitCurrentIteration}\n");
                        return;
                    }

                    if (pingResponse == "YES") {
                        // If a process has responded positively to the ping, then 
                        // we can cancel all other operations in the loop and break out of it.
                        Console.WriteLine($"Found 'YES' response from pipe '{pipeName}'\n");
                        Console.WriteLine($"@{pipeName} - Should cancel? {state.ShouldExitCurrentIteration}\n");

                        nameOfChosenProcess = pipeName;
                        state.Break();
                        cts.Cancel();
                        return;
                    }
                });
            } catch (OperationCanceledException e) {
                Console.WriteLine("Exception -- Loop canceled!\n");
            } finally {
                cts.Dispose();
                Console.WriteLine($"Chosen process (i.e., first process to reply'YES' to the ping): '{nameOfChosenProcess}'");
            }
        }
    }
}
