using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace chat_server
{
    internal class ChatServer
    {
        ClientManager _cm = null;
        ConcurrentBag<string> chatLog = null;
        ConcurrentBag<string> accessLog = null;
        Thread connectCheckThread = null;

        public ChatServer()
        {
            AsyncServerStart();
        }

        private void AsyncServerStart()
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));
            listener.Start();
            Console.WriteLine("Started Chatting Server");

            while(true)
            {
                TcpClient connectedClient = listener.AcceptTcpClient();
                Console.WriteLine("Successful Connection of Client");

                ClientData clientData = new ClientData(connectedClient);
                NetworkStream stream = clientData.tcpClient.GetStream();
                // async function
                stream.BeginRead(
                    clientData.readBuffer,
                    0,
                    clientData.readBuffer.Length,
                    new AsyncCallback(DataReceived),
                    clientData
                );
            }
        }

        private void DataReceived(IAsyncResult ar)
        {
            ClientData? callbackClient = ar.AsyncState as ClientData;
            if (callbackClient == null) return;

            int bytesRead = callbackClient.tcpClient.GetStream().EndRead(ar);
            string readString = Encoding.Default.GetString(callbackClient.readBuffer, 0, bytesRead);

            Console.WriteLine($"User {callbackClient.clientNumber}: {readString}");

            NetworkStream stream = callbackClient.tcpClient.GetStream();
            // async function
            // recursive callback 
            stream.BeginRead(
                callbackClient.readBuffer,
                0,
                callbackClient.readBuffer.Length,
                new AsyncCallback(DataReceived),
                callbackClient
            );
        }
    }
}
