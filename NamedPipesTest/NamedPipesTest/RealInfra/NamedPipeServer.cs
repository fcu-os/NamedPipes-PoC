using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStudio.Presenter.ProcessCommunication {

    internal class NamedPipeServer : IDisposable {

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public event Func<string, string> MessageReceived;

        public event Action<Exception> ClientConnectionErrorRaised;

        private const int DefaultMaxErrorsAllowed = 5;

        public int MaxErrorsAllowed { get; set; } = DefaultMaxErrorsAllowed;

        public NamedPipeServer(INamedPipeServerProvider namedPipeServerProvider, string pipeName, PipeDirection pipeDirection = PipeDirection.InOut, int maxNumberOfServerInstances = 1) {
            Task.Run(async () => {
                var errorCount = 0;
                while (!cancellationTokenSource.IsCancellationRequested) {
                    NamedPipeServerStream serverPipe = null;
                    try {
                        serverPipe = await namedPipeServerProvider.CreateServerAndWaitClientConnection(pipeName, pipeDirection, maxNumberOfServerInstances, cancellationTokenSource.Token);
                        await HandleClientConnected(serverPipe);
                    } catch {
                        if (++errorCount > MaxErrorsAllowed) {
                            break;
                        }
                    } finally {
                        serverPipe?.Dispose();
                    }
                }
            });
        }

        public void Dispose() {
            cancellationTokenSource.Cancel();
        }

        private async Task HandleClientConnected(Stream pipeStream) {
            var messageReceivedHandler = MessageReceived;
            if (messageReceivedHandler == null) {
                return;
            }

            try {
                var stream = new NamedPipeStream(pipeStream);
                var messageFromClient = await stream.ReadMessage(2000);
                var responseToClient = messageReceivedHandler(messageFromClient);
                await stream.SendMessage(responseToClient, 2000);
            } catch (Exception e) {
                ClientConnectionErrorRaised?.Invoke(e);
            }
        }
    }
}
