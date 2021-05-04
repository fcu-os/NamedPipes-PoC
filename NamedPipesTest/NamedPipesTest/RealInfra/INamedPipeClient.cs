using System.Threading.Tasks;

namespace ServiceStudio.Presenter.ProcessCommunication {
    internal interface INamedPipeClient {
        Task<string> ReadMessage(int readTimeout);
        Task SendMessage(string message, int writeTimeout);
        Task<string> SendMessageAndWaitResponse(string message, int readTimeout, int writeTimeout);
    }
}