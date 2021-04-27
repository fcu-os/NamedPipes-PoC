using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesTest {

    class NamedPipeServer {

        internal static void Launch(string pipeName) {
            using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut)) {
                using var reader = new StreamReader(pipeServer);
                using var writer = new StreamWriter(pipeServer);

                var running = true;

                Console.WriteLine($"Server '{pipeName}' is waiting for the target process to connect...\n");
                pipeServer.WaitForConnection();
                
                Thread.Sleep(4000);

                Console.WriteLine($"Server '{pipeName}' confirms that the client process is connected.\n");

                Thread.Sleep(4000);
                Console.WriteLine($"Server '{pipeName}' sends a ping...\n");
                writer.WriteLine(Program.PingMessage);
                writer.Flush();

                while (running) {
                    pipeServer.WaitForPipeDrain();
                    Thread.Sleep(4000);

                    var responseFromClientProcess = reader.ReadLine();
                    Console.WriteLine($"Server '{pipeName}' has received from client: '{responseFromClientProcess}'\n");

                    switch (responseFromClientProcess) {
                        case "YES":
                            writer.WriteLine(Program.ReuseMessage);
                            running = false;
                            break;
                        default:
                            break;
                    }
                }
            }
            Console.WriteLine("Server: Quits");
        }
    }
}
