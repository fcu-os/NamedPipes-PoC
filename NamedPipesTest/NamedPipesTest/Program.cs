using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NamedPipesTest {
    class Program {

        internal const string PingMessage = "Can you replace me?";
        internal const string ReuseMessage = "Login Info goes here...";

        static void Main(string[] _) {
            // Launch a bunch of dummy processes -- Notepad process will do, for this example
            Process.Start("notepad.exe");
            Process.Start("notepad.exe");
            //Process.Start("notepad.exe");

            // Gather relevant (dummy) processes and their PIDs
            var relevantProcess = ProcessUtils.FindRelevantProcesses("notepad");
            Console.WriteLine($"Number of relevant processes found: {relevantProcess.Length}\n");

            // Ping each of the relevant processes found
            Parallel.ForEach(relevantProcess, process => {
                var canonicalProcessName = ProcessUtils.GetCanonicalProcessName(process);
                Parallel.Invoke(
                    () => NamedPipeServer.Launch(pipeName: canonicalProcessName),
                    () => NamedPipeClient.Launch(pipeName: canonicalProcessName));
            });

            Console.WriteLine("Done.");
        }
    }
}
