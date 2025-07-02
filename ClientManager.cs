using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace chat_server
{
    internal class ClientManager
    {
        public static ConcurrentDictionary<string, ClientData> clientDict = new ConcurrentDictionary<string, ClientData> ();
        public event Action<string, string>? messageParsingAction = null;
        public event Action<string, int>? EventHandler = null;

        // 새로운 Client 접속 시 실행 
        public void AddClient(TcpClient newClient)
        {
            ClientData currentClient = new ClientData(newClient);
            clientDict.TryAdd(currentClient.ClientKey, currentClient);

            // Call ReceiveLoopAsync
            ReceiveLoopAsync(currentClient)
                // Task 끝난 후 예외 Exception Logging
                .ContinueWith(
                    t => Console.WriteLine(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted
                );
        }

        // Client에게 데이터 수신
        private async Task ReceiveLoopAsync(ClientData client)
        {
            try
            {
                while (true)
                {
                    NetworkStream stream = client.tcpClient.GetStream();
                    byte[] buffer = client.readBuffer;     
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string strData = Encoding.Default.GetString(buffer, 0, bytesRead);
                    await HandleReceivedAsync(client, strData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        // 수신한 데이터 처리
        private Task HandleReceivedAsync(ClientData client, string strData)
        {
            // 최초 접속 처리
            if (string.IsNullOrEmpty(client.ClientId) && CheckID(strData))
            {
                client.ClientId = strData.Substring(3);
                string AccessLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {client.ClientId} Access Server";
                EventHandler?.Invoke(AccessLog, StaticDefine.ADD_ACCESS_LOG);
                return Task.CompletedTask;
            }

            if (messageParsingAction != null) return Task.Run(() => messageParsingAction(client.ClientId, strData));

            return Task.CompletedTask;
        }

        private bool CheckID(string id)
        {
            return id.Contains("%^&");
        }
    }
}