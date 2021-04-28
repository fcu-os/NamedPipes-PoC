using System.Diagnostics;

namespace NamedPipesTest {
    internal class ProcessUtils {

        internal static Process[] FindRelevantProcesses(string name) {
            // Get all instances of $name running on the local computer.
            return Process.GetProcessesByName(name);
        }

        internal static string GetCanonicalProcessName(Process process) {
            return $"{process.ProcessName}-{process.Id}";
        }
    }
}
