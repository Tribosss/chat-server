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
        ConcurrentBag<string> chatLog = null;
        ConcurrentBag<string> accessLog = null;
        Thread connectCheckThread = null;
        public ChatServer()
        {
            // default variable initialize
            _cm = new ClientManager();
            chatLog = new ConcurrentBag<string>();
            accessLog = new ConcurrentBag<string>();
            _dbClient = new DBClient();

            // handler binding
            _cm.EventHandler += ClientEvent;
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
            ClientManager.clientDict.TryRemove(targetClient.clientNumber, out result);
            string leaveLog = string.Format($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {result.clientName} Leave Server");
            accessLog.Add(leaveLog);
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
        private void SendMsgToClient(List<string> msgList, string sender)
        {
            string logMessage = "";
            string parsedMessage = "";
            string roomId = "";

            int senderNumber = -1;
            int receiverNumber = -1;

            foreach (var item in msgList) {
                string[] splitedMsg = item.Split("<");

                roomId = splitedMsg[0];
                parsedMessage = string.Format($"{sender}<{splitedMsg[1]}>");

                senderNumber = GetClientNumber(sender);

                if (senderNumber == -1) return;

                if (parsedMessage.Contains("<GiveMeUserList"))
                {
                    string userListStringData = "ADMIN<";

                    List<Room> rooms = _dbClient.GetRoomIdList(sender);

                    foreach(Room room in rooms)
                    {
                        userListStringData += string.Format($"${room.Id}");
                    }

                    userListStringData += ">";
                    byte[] userListByteData = new byte[userListStringData.Length];

                    userListByteData = Encoding.Default.GetBytes(userListStringData);
                    ClientManager.clientDict[senderNumber].tcpClient.GetStream().Write(userListByteData, 0, userListByteData.Length);

                    return;
                }

                string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ChattingLog log = new ChattingLog()
                {
                    Msg = splitedMsg[1],
                    SenderId = sender,
                    RoomId = roomId,
                    CreatedAt = createdAt,
                };

                List<Member> members = _dbClient.GetMembersByRoomId(roomId);
                foreach (Member member in members) {
                    ClientEvent(logMessage, StaticDefine.ADD_CHATTING_LOG);
                    receiverNumber = GetClientNumber(member.EmpId);

                    if (receiverNumber == -1) continue;

                    byte[] sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDict[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
                }
            }
        }

        private int GetClientNumber(string targetClientName)
        {
            foreach (var item in ClientManager.clientDict) {
                if (item.Value.clientName == targetClientName) return item.Value.clientNumber;
            }
            return -1;
        }

        private void ClientEvent(string message, int key)
        {
            switch (key)
            {
                case StaticDefine.ADD_ACCESS_LOG:
                    {
                        accessLog.Add(message);
                        break;
                    }
                case StaticDefine.ADD_CHATTING_LOG:
                    {
                        chatLog.Add(message);
                        break;
                    }
            }
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

        public void ConsoleView()
        {
            while(true)
            {
                Console.WriteLine("=============서버=============");
                Console.WriteLine("1.현재접속인원확인");
                Console.WriteLine("2.접속기록확인");
                Console.WriteLine("3.채팅로그확인");
                Console.WriteLine("0.종료");
                Console.WriteLine("==============================");

                string key = Console.ReadLine();
                int order = 0;


                if (int.TryParse(key, out order))
                {
                    switch (order)
                    {
                        case StaticDefine.SHOW_CURRENT_CLIENT:
                            {
                                ShowCurrentClient();
                                break;
                            }
                        case StaticDefine.SHOW_ACCESS_LOG:
                            {
                                ShowAccessLog();
                                break;
                            }
                        case StaticDefine.SHOW_CHATTING_LOG:
                            {
                                ShowCattingLog();
                                break;
                            }

                        case StaticDefine.EXIT:
                            {
                                connectCheckThread.Abort();
                                return;
                            }
                        default:
                            {
                                Console.WriteLine("잘못 입력하셨습니다.");
                                Console.ReadKey();
                                break;
                            }
                    }
                }

                else
                {
                    Console.WriteLine("잘못 입력하셨습니다.");
                    Console.ReadKey();
                }
                Console.Clear();
                Thread.Sleep(50);
            }
        }

        // 채팅로그확인
        private void ShowCattingLog()
        {
            if (chatLog.Count == 0)
            {
                Console.WriteLine("채팅기록이 없습니다.");
                Console.ReadKey();
                return;
            }

            foreach (var item in chatLog)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }

        // 접근로그확인
        private void ShowAccessLog()
        {
            if (accessLog.Count == 0)
            {
                Console.WriteLine("접속기록이 없습니다.");
                Console.ReadKey();
                return;
            }

            foreach (var item in accessLog)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }

        // 현재접속유저확인
        private void ShowCurrentClient()
        {
            if (ClientManager.clientDict.Count == 0)
            {
                Console.WriteLine("접속자가 없습니다.");
                Console.ReadKey();
                return;
            }

            foreach (var item in ClientManager.clientDict)
            {
                Console.WriteLine(item.Value.clientName);
            }
            Console.ReadKey();
        }
    }
}
