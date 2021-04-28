using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
            var relevantProcess = ProcessUtils.FindRelevantProcesses("notepad");
            Console.WriteLine($"Number of relevant processes found: {relevantProcess.Length}\n");

            var nameOfChosenProcess = string.Empty;
            NamedPipeClient namedPipeClient = null;

            // All servers are already listening:
            var relevantProcessesList = relevantProcess.ToList();
            relevantProcessesList.ForEach(p => Task.Run(() => NamedPipeServer.Launch(pipeName: ProcessUtils.GetCanonicalProcessName(p))));

            // Create the Tasks that will ping each server
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var pingTasks = new List<Task<(NamedPipeClient, string)>>(relevantProcess.Length);

            relevantProcessesList.ForEach(p => {
                var task = Task.Run(() => {
                    var pipeName = ProcessUtils.GetCanonicalProcessName(p);
                    return new NamedPipeClient().Launch(pipeName);
                }, cancellationToken);

                pingTasks.Add(task);
            });

            while (pingTasks.Any()) {
                var completedTask = await Task.WhenAny(pingTasks);
                var (client, pingResult) = await completedTask;

                if (pingResult == "YES") {
                    namedPipeClient = client;
                    nameOfChosenProcess = client.pipeName;
                    cancellationTokenSource.Cancel();
                    break;
                } else {
                    pingTasks.Remove(completedTask);
                }
            }

            Console.WriteLine($"Chosen process (i.e., first process to reply 'YES' to the ping): '{nameOfChosenProcess}'\n");

            // Now that we have an available process, send it a REUSE message
            if (!namedPipeClient.pipeClient.IsConnected) {
                namedPipeClient.pipeClient.Connect();
                Console.WriteLine($"Now connected to '{nameOfChosenProcess}'...\n");
            }

            namedPipeClient.writer.WriteLine(Messages.ReuseMessage);
            namedPipeClient.writer.Flush();
            namedPipeClient.pipeClient.WaitForPipeDrain();

            Console.WriteLine($"Sent REUSE message to '{nameOfChosenProcess}', terminating...\n");
        }
    }
}
