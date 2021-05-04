using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStudio.Presenter.ProcessCommunication {
    internal class NamedPipeServerProvider : INamedPipeServerProvider {
        public async Task<NamedPipeServerStream> CreateServerAndWaitClientConnection(string pipeName, PipeDirection pipeDirection, int maxNumberOfServerInstances, CancellationToken token) {
            var serverPipe = new NamedPipeServerStream(pipeName, pipeDirection, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            try {
                await serverPipe.WaitForConnectionAsync(token);
            } catch {
                serverPipe.Dispose();
            }
            return serverPipe;
        }
    }
}
