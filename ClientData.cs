using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace chat_server
{
    internal class ClientData
    {
        public TcpClient tcpClient { get; set; }
        public byte[] readBuffer { get; set; }
        public StringBuilder currentMsg { get; set; }
        public string ClientId { get; set; } 
        public string ClientKey;
        public ClientData(TcpClient client) {
            this.currentMsg = new StringBuilder();
            this.tcpClient = client;
            this.readBuffer = new byte[1024];

            IPEndPoint remoteEp = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            string ip = remoteEp.Address.ToString();
            int port = remoteEp.Port;
            this.ClientKey = $"{ip}:{port}";

            //temp = tcpClient.Client.LocalEndPoint.ToString().Split(splitDivision);
            //this.ClientId = int.Parse(temp[3]);
        }
    }
}
