using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace chat_server
{
    internal class ClientManager
    {
        public static ConcurrentDictionary<int, ClientData> clientDict = new ConcurrentDictionary<int, ClientData> ();
        public event Action<string, string> msgParsingEvt = null;
        public event Action<string, int> evtHandler = null;

        public void AddClient(TcpClient newClient)
        {
            ClientData currentClient = new ClientData(newClient);

            try
            {
                NetworkStream stream = currentClient.tcpClient.GetStream();
                clientDict.TryAdd(currentClient.clientNumber, currentClient);
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private void DataReceived(IAsyncResult ar)
        {
            ClientData client = ar.AsyncState as ClientData;

            try
            {
                int byteLength = client.tcpClient.GetStream().EndRead(ar);
                string stringData = Encoding.Default.GetString(client.readBuffer, 0, byteLength);
                NetworkStream stream = client.tcpClient.GetStream();
                stream.BeginRead(
                    client.readBuffer,
                    0,
                    client.readBuffer.Length,
                    new AsyncCallback(DataReceived),
                    client
                );

                if (string.IsNullOrEmpty(client.clientName) && evtHandler != null && CheckID(stringData)) {
                    string username = stringData.Substring(3);
                    client.clientName = username;
                    string accessLog = string.Format($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {client.clientName} Access Server");
                    evtHandler.Invoke(accessLog, StaticDefine.ADD_ACCESS_LOG);
                }

                if (msgParsingEvt != null)
                {
                    msgParsingEvt.BeginInvoke(client.clientName, stringData, null, null);
                }

            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private bool CheckID(string id) {
            if (id.Contains("%^&")) return true;
            return false;
        }
    }
}