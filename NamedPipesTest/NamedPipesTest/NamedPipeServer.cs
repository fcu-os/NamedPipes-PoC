using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesTest {
    class NamedPipeServer {

        internal static void Launch(string pipeName) {
            var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
            var reader = new StreamReader(pipeServer);
            var writer = new StreamWriter(pipeServer);

            var running = true;

            Console.WriteLine($"Server '{pipeName}' is launched...\n");
            Console.WriteLine($"Server '{pipeName}' is waiting for the target process to connect...\n");
            pipeServer.WaitForConnection();

            Console.WriteLine($"Server '{pipeName}' confirms that the client process is connected.\n");

            while (running) {
                var messageFromClient = reader.ReadLine();

                if (!string.IsNullOrEmpty(messageFromClient)) {
                    Console.WriteLine($"Server '{pipeName}' received from client: '{messageFromClient}'\n");
                    switch (messageFromClient) {
                        case Messages.PingMessage:
                            Thread.Sleep(3000 * new Random().Next(2, 10));
                            writer.WriteLine("YES");
                            writer.Flush();
                            //pipeServer.Disconnect();
                            break;
                        case Messages.ReuseMessage:
                            Console.WriteLine($"Server '{pipeName}' received REUSE message\n");
                            running = false;
                            break;
                    }
                }
            }
        }
    }
}
