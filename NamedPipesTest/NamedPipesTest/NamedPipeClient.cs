using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesTest {
    class NamedPipeClient {

        internal static void Launch(string pipeName) {
            using (var pipeClient = new NamedPipeClientStream(serverName: ".", pipeName, PipeDirection.InOut)) {
                Console.WriteLine($"Client '{pipeName}' is launched...\n");
                
                const int connectionTimeoutMilliseconds = 8000;
                if (!pipeClient.IsConnected) {
                    Thread.Sleep(4000);
                    pipeClient.Connect(connectionTimeoutMilliseconds);
                }

                Console.WriteLine($"Client '{pipeName}' is connected!\n");

                using var reader = new StreamReader(pipeClient);
                using var writer = new StreamWriter(pipeClient);

                var running = true;
                while (running) {
                    Thread.Sleep(4000);

                    var messageFromServer = reader.ReadLine();
                    if (messageFromServer != null) {
                        Console.WriteLine($"Client '{pipeName}' received from server: '{messageFromServer}'\n");
                        switch (messageFromServer) {
                            case Program.PingMessage:
                                writer.WriteLine("YES");
                                writer.Flush();
                                break;
                            case "quit":
                                running = false;
                                break;
                        }
                    }
                }
            }
            Console.WriteLine("Client Quits");
        }
    }
}
