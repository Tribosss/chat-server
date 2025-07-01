using System;
using System.Collections.Generic;
using System.Linq;
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
        public string clientName { get; set; } 
        public int clientNumber;
        public ClientData(TcpClient client) {
            this.currentMsg = new StringBuilder();
            this.tcpClient = client;
            this.readBuffer = new byte[1024];

            char[] splitDivision = new char[2];
            splitDivision[0] = '.';
            splitDivision[1] = ':';
            string[] temp = null;

            temp = tcpClient.Client.LocalEndPoint.ToString().Split(splitDivision);
            this.clientNumber = int.Parse(temp[3]);
            Console.WriteLine($"Successful Connect of User {clientNumber}");
        }
    }
}
