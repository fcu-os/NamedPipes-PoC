using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesTest {

    class NamedPipeClient {

        internal static (string pipeName, string pingResponse) Launch(string pipeName) {
            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            using var reader = new StreamReader(pipeClient);
            using var writer = new StreamWriter(pipeClient);

            if (!pipeClient.IsConnected) {
                pipeClient.Connect();
                Console.WriteLine($"Client '{pipeName}' is now connected...\n");
            }

            writer.WriteLine(Program.PingMessage);
            writer.Flush();
            Console.WriteLine($"Client '{pipeName}' sends a ping...\n");

            pipeClient.WaitForPipeDrain();

            var pingResponse = reader.ReadLine();
            Console.WriteLine($"Client '{pipeName}' got response from ping: '{pingResponse}'\n");

            return (pipeName, pingResponse);
        }
    }
}
