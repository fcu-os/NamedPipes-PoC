using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesTest {

    class NamedPipeClient {

        internal NamedPipeClientStream pipeClient;
        internal StreamReader reader;
        internal StreamWriter writer;

        internal string pipeName;

        internal (NamedPipeClient pipeClient, string pingResponse) Launch(string pipeName) {

            // WARNING: Here we're not using the 'using' statement because we don't want the pipeStream
            // and its reader/writer to be disposed at this point; we're doing a 2-step messaging thing,
            // e.g. PING and then REUSE, and so we need the same pipe opened all that time.
            // We'll need to make sure we manage this right.

            this.pipeName = pipeName;
            this.pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            this.reader = new StreamReader(pipeClient);
            this.writer = new StreamWriter(pipeClient);

            if (!pipeClient.IsConnected) {
                pipeClient.Connect();
                Console.WriteLine($"Client '{pipeName}' is now connected...\n");
            }

            writer.WriteLine(Messages.PingMessage);
            writer.Flush();
            Console.WriteLine($"Client '{pipeName}' sends a ping...\n");

            pipeClient.WaitForPipeDrain();

            var pingResponse = reader.ReadLine();
            Console.WriteLine($"Client '{pipeName}' got response from ping: '{pingResponse}'\n");

            return (this, pingResponse);
        }
    }
}
