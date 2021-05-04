using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStudio.Presenter.ProcessCommunication {
    internal class NamedPipeClient : INamedPipeClient, IDisposable {

        private const int DefaultReadTimeout = 1000;
        private const int DefaultWriteTimeout = 1000;

        private readonly NamedPipeClientStream clientPipe;

        public NamedPipeClient(string pipeName, PipeDirection pipeDirection = PipeDirection.InOut, int timeout = Timeout.Infinite) {
            // Client and Server processes are intended to run on the same computer, as such, we give "." as the server name
            clientPipe = new NamedPipeClientStream(serverName: ".", pipeName, pipeDirection);
            // Connects to a waiting server within the specified miliseconds timeout period
            clientPipe.Connect(timeout);
        }

        public async Task<string> ReadMessage(int timeout = DefaultReadTimeout) {
            var stream = new NamedPipeStream(clientPipe);
            return await stream.ReadMessage(timeout);
        }

        public async Task SendMessage(string message, int timeout = DefaultWriteTimeout) {
            var stream = new NamedPipeStream(clientPipe);
            await stream.SendMessage(message, timeout);
        }

        public async Task<string> SendMessageAndWaitResponse(string message, int readTimeout, int writeTimeout) {
            // Send message to the server
            await SendMessage(message, writeTimeout);
            // Wait for server to read and send a response
            clientPipe.WaitForPipeDrain();
            return await ReadMessage(readTimeout);
        }

        public void Dispose() {
            clientPipe.Close();
            clientPipe.Dispose();
        }
    }
}
