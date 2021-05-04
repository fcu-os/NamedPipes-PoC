using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStudio.Presenter.ProcessCommunication {
    internal interface INamedPipeServerProvider {
        Task<NamedPipeServerStream> CreateServerAndWaitClientConnection(string pipeName, PipeDirection pipeDirection, int maxNumberOfServerInstances, CancellationToken token);
    }
}