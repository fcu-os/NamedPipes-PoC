using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStudio.Presenter.ProcessCommunication {

    internal class NamedPipeStream {

        private readonly Stream stream;
        private readonly UnicodeEncoding streamEncoding;

        public NamedPipeStream(Stream stream) {
            this.stream = stream;
            streamEncoding = new UnicodeEncoding();
        }

        public async Task SendMessage(string message, int timeout) {
            var messageInBytes = WriteToBuffer(message);
            await WriteAsync(messageInBytes, timeout);
        }

        public async Task<string> ReadMessage(int timeout) {
            var messageLength = ReadIntFromStream(stream);
            return await ReadAsync(messageLength, timeout);
        }

        private async Task WriteAsync(byte[] message, int timeout) {
            var cts = new CancellationTokenSource(delay: TimeSpan.FromMilliseconds(timeout));

            try {
                await stream.WriteAsync(message, 0, message.Length, cts.Token);
                stream.Flush();
            } catch (Exception) {
                // TODO: Log or something
                throw;
            } finally {
                cts.Cancel();
                cts.Dispose();
            }
        }

        private async Task<string> ReadAsync(int messageLength, int timeout) {
            var buffer = new byte[messageLength];
            var cts = new CancellationTokenSource(delay: TimeSpan.FromMilliseconds(timeout));

            try {
                await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                return streamEncoding.GetString(buffer);
            } catch (Exception) {
                Console.WriteLine("Exception @ ReadAsync");
                // TODO: Log or something
            } finally {
                cts.Cancel();
                cts.Dispose();
            }

            return null;
        }

        private static int ReadIntFromStream(Stream stream) {
            var valueInBytes = new byte[4];
            for (var i = 0; i < valueInBytes.Length; i++) {
                valueInBytes[i] = (byte)stream.ReadByte();
            }
            return BitConverter.ToInt32(valueInBytes, 0);
        }

        private byte[] WriteToBuffer(string message) {
            var messageContentInBytes = streamEncoding.GetBytes(message);
            var messageContentLengthInBytes = BitConverter.GetBytes(messageContentInBytes.Length);

            var finalBuffer = new byte[messageContentInBytes.Length + messageContentLengthInBytes.Length];
            messageContentLengthInBytes.CopyTo(finalBuffer, 0);
            messageContentInBytes.CopyTo(finalBuffer, messageContentLengthInBytes.Length);
            return finalBuffer;
        }
    }
}
