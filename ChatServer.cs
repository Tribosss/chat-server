using chat_server.Model;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace chat_server
{
    internal class ChatServer
    {
        private ClientManager _cm = null;
        private DBClient _dbClient = null;
        // ConcurrentBag: Thread로부터 안전한 List, Dictionary
        Thread connectCheckThread = null;
        public ChatServer()
        {
            // default variable initialize
            _cm = new ClientManager();
            _dbClient = new DBClient();

            // handler binding
            _cm.messageParsingAction += MessageParsing;

            // server activate
            Task serverStart = Task.Run(() => ServerRun());

            // heartbeat thread activate
            connectCheckThread = new Thread(ConnectCheckLoop);
            connectCheckThread.Start();
        }

        // HeartBeat Thread
        // 현재 접속자 확인 (1초마다 "ADMIN<TEST>" 전송)
        private void ConnectCheckLoop()
        {
            while (true)
            {
                foreach (var item in ClientManager.clientDict)
                {
                    try
                    {
                        string sendStringData = "ADMIN<TEST>";
                        byte[] sendByteData = new byte[sendStringData.Length];
                        sendByteData = Encoding.Default.GetBytes(sendStringData);

                        item.Value.tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);

                    }
                    catch (Exception e)
                    {
                        RemoveClient(item.Value);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        // HeartBeat Thread에서 접속 끊긴 Client 삭제
        private void RemoveClient(ClientData targetClient)
        {
            ClientData result = null;
            ClientManager.clientDict.TryRemove(targetClient.ClientKey, out result);
        }

        // Message Parsing 
        // Called by ClientManager
        private void MessageParsing(string sender, string message)
        {
            List<string> msgList = new List<string>();

            string[] msgArray = message.Split(">");
            foreach (var item in msgArray)
            {
                if (string.IsNullOrEmpty(item)) continue;

                msgList.Add(item);
            }
            SendMsgToClient(msgList, sender);
        }

        // 대상자에게 메시지 전달
        private void SendMsgToClient(List<string> msgList, string senderId)
        {
            string parsedMessage = "";
            string roomId = "";

            string senderKey = "";
            string receiverKey = "";

            foreach (var item in msgList) {
                string[] splitedMsg = item.Split("<");

                roomId = splitedMsg[0];
                parsedMessage = string.Format($"{senderId}<{splitedMsg[1]}>");

                senderKey = GetClientNumber(senderId);

                if (string.IsNullOrEmpty(senderKey)) return;

                if (parsedMessage.Contains("<GiveMeUserList"))
                {
                    string userListStringData = "ADMIN<";

                    List<Room> rooms = _dbClient.GetRoomIdList(senderId);

                    foreach(Room room in rooms)
                    {
                        userListStringData += string.Format($"${room.Id}");
                    }

                    userListStringData += ">";
                    byte[] userListByteData = new byte[userListStringData.Length];

                    userListByteData = Encoding.Default.GetBytes(userListStringData);
                    ClientManager.clientDict[senderKey].tcpClient.GetStream().Write(userListByteData, 0, userListByteData.Length);

                    return;
                }

                string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ChattingLog log = new ChattingLog()
                {
                    Msg = splitedMsg[1],
                    SenderId = senderId,
                    RoomId = roomId,
                    CreatedAt = createdAt,
                };

                List<Member> members = _dbClient.GetMembersByRoomId(roomId);
                foreach (Member member in members) {
                    receiverKey = GetClientNumber(member.EmpId);

                    if (receiverKey == senderKey) continue;

                    ChattingLog logParam = new ChattingLog()
                    {
                        RoomId = roomId,
                        SenderId = senderId,
                        Msg = splitedMsg[1],
                        CreatedAt = createdAt,
                    };
                    _dbClient.InsertChattingLog(logParam);

                    if (string.IsNullOrEmpty(senderKey)) continue;
                    byte[] sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDict[receiverKey].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
                }
            }
        }

        private string GetClientNumber(string targetClientName)
        {
            foreach (var item in ClientManager.clientDict) {
                if (item.Value.ClientId == targetClientName) return item.Value.ClientKey;
            }
            return "";
        }

        private void ServerRun()
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));
            listener.Start();

            while(true)
            {
                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
                acceptTask.Wait();

                TcpClient newClient = acceptTask.Result;

                _cm.AddClient(newClient);
            }
        }
    }
}
