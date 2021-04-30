using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipesTest {
    class ParallelForTest {

        static void Main(string[] _) {
            // Launch a bunch of dummy processes -- Notepad process will do, for this example
            Process.Start("notepad.exe");
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
                    var pipeName = ProcessUtils.GetCanonicalProcessName(relevantProcess[i]);

                    // Ping server and wait for a response
                    var pingResponse = NamedPipeClient.Launch(pipeName: pipeName);

                    // If there has already been a "YES" response to a ping from some iteration,
                    // the parallel for will be canceled and all other iterations that hit this point will
                    // be abandoned at this point
                    if (state.IsStopped) {
                        Console.WriteLine($"@{pipeName} - Should exit current iteration? {state.IsStopped}\n");
                        return;
                    }

                    if (pingResponse == "YES") {
                        // If a process has responded positively to the ping, then 
                        // we can cancel all other operations in the loop and break out of it.
                        Console.WriteLine($"Found 'YES' response from pipe '{pipeName}'\n");
                        Console.WriteLine($"@{pipeName} - Should cancel? {state.ShouldExitCurrentIteration}\n");

                        nameOfChosenProcess = pipeName;
                        state.Stop();
                        cts.Cancel();
                        return;
                    }
                });
            } catch (OperationCanceledException) {
                Console.WriteLine("Exception -- Loop canceled!\n");
            } finally {
                cts.Dispose();
            }

            Console.WriteLine($"Chosen process (i.e., first process to reply'YES' to the ping): '{nameOfChosenProcess}'");

            // Now that we have an available process, send it a REUSE message
            using var pipeClient = new NamedPipeClientStream(".", nameOfChosenProcess, PipeDirection.InOut);
            using var reader = new StreamReader(pipeClient);
            using var writer = new StreamWriter(pipeClient);

            if (!pipeClient.IsConnected) {
                pipeClient.Connect();
                Console.WriteLine($"Now connected to '{nameOfChosenProcess}'...\n");
            }

            writer.WriteLine(Messages.ReuseMessage);
            writer.Flush();
            Console.WriteLine($"Sent REUSE message to '{nameOfChosenProcess}', terminating...\n");
        }
    }
}
