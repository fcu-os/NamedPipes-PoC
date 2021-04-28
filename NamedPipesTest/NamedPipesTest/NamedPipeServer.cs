using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesTest {
    class NamedPipeServer {

        internal static void Launch(string pipeName) {
            using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
            using var reader = new StreamReader(pipeServer);
            using var writer = new StreamWriter(pipeServer);

            Console.WriteLine($"Server '{pipeName}' is launched...\n");
            Console.WriteLine($"Server '{pipeName}' is waiting for the target process to connect...\n");
            pipeServer.WaitForConnection();

            Console.WriteLine($"Server '{pipeName}' confirms that the client process is connected.\n");

            var messageFromClient = reader.ReadLine();
            Console.WriteLine($"Server '{pipeName}' received from client: '{messageFromClient}'\n");

            switch (messageFromClient) {
                case Program.PingMessage:
                    Thread.Sleep(3000 * new Random().Next(1, 5));
                    writer.WriteLine("YES");
                    writer.Flush();
                    break;
                default:
                    break;
            }
        }
    }
}
