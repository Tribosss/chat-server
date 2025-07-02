using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chat_server.Model;
using DotNetEnv;
using MySql.Data.MySqlClient;

namespace chat_server
{
    internal class DBClient
    {
        private MySqlConnection connection = null;
        public DBClient()
        {
            string? host, port, uid, name, pwd;
            string dbConnection;

            try
            {
                Env.Load();

                host = Environment.GetEnvironmentVariable("DB_HOST");
                if (host == null) return;
                port = Environment.GetEnvironmentVariable("DB_PORT");
                if (port == null) return;
                uid = Environment.GetEnvironmentVariable("DB_UID");
                if (uid == null) return;
                pwd = Environment.GetEnvironmentVariable("DB_PWD");
                if (pwd == null) return;
                name = Environment.GetEnvironmentVariable("DB_NAME");
                if (name == null) return;

                dbConnection = $"Server={host};Port={port};Database={name};Uid={uid};Pwd={pwd}";

                connection = new MySqlConnection(dbConnection);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public List<Member> GetMembersByRoomId(string roomId)
        {
            List<Member> members = new List<Member>(); 
            if (connection == null) return members;

            connection.Open();

            string query = $"select m.emp_id from chat_members m where m.room_id='{roomId}'";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlDataReader rdr = cmd.ExecuteReader();
            if (rdr == null) return members;

            while (rdr.Read())
            {

                Member member = new Member()
                {
                    RoomId = roomId,
                    EmpId = rdr[0].ToString()
                };
                members.Add(member);
            }

            connection.Close();

            return members;
        }
        public List<Room> GetRoomIdList(string requestorId)
        {
            List<Room> roomIds = new List<Room>();
            if (connection == null)
            {
                Console.WriteLine("connection is null");
                return roomIds;
            }

            connection.Open();

            string query = "select r.id from chat_rooms r ";
            query += "inner join chat_members m on m.room_id=r.id ";
            query += $"where m.emp_id='{requestorId}';";

            MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlDataReader rdr = cmd.ExecuteReader();
            if (rdr == null) return roomIds;

            string? roomId;
            while(rdr.Read())
            {
                Room room = new Room() { Id = rdr[0].ToString() };
                roomIds.Add(room);
            }

            connection.Close();

            return roomIds;
        }
        public List<ChattingLog> GetChattingLogs(string roomId)
        {
            List<ChattingLog> chatLogs = new List<ChattingLog>();
            if (connection == null) return chatLogs;

            connection.Open();

            string query = "select m.sender_id, m.msg from chat_messages m ";
            query += $"where m.room_id='{roomId}'";

            MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlDataReader rdr = cmd.ExecuteReader();
            if (rdr == null) return chatLogs;

            string? senderId;
            string? message;
            while (rdr.Read())
            {
                senderId = rdr[0].ToString();
                message = rdr[1].ToString();
                if (message == null) continue;

                ChattingLog chatLog = new ChattingLog()
                {
                    SenderId = senderId,
                    Msg = message,
                    RoomId = roomId
                };
                chatLogs.Add(chatLog);
            }

            connection.Close();

            return chatLogs;
        }

        public int InsertChattingLog(ChattingLog chat) {
            if (connection == null) return -1;

            connection.Open();

            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string query = "insert into chat_messages(room_id, sender_id, msg, created_at) ";
            query += $"values('{chat.RoomId}', '{chat.SenderId}', '{chat.Msg}', '{createdAt}');";

            MySqlCommand cmd = new MySqlCommand(query, connection);

            if (cmd.ExecuteNonQuery() == 1)
            { 
                connection.Close();
                return 0;
            }
            else
            {
                connection.Close();
                return -1;
            }
        }
        public int CreateRoom(Room room, List<Member> members) {
            if (connection == null) return -1;

            connection.Open();

            // create room
            string roomId = Guid.NewGuid().ToString();
            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string query = "insert into chat_rooms(id, created_at) ";
            query += $"values('{roomId}', '{createdAt}'";

            MySqlCommand roomInsertCmd = new MySqlCommand(query, connection);

            if (roomInsertCmd.ExecuteNonQuery() != 1)
            {
                connection.Close();
                return -1;
            }

            // insert members
            foreach(Member member in members)
            {
                query = "insert into chat_members(room_id, emp_id, joined_at) ";
                query += $"values('{roomId}', '{member.EmpId}', '{createdAt}'";
                MySqlCommand memberInsertQuery = new MySqlCommand(query, connection);
                if (memberInsertQuery.ExecuteNonQuery() != 1)
                {
                    connection.Close();
                    return -1;
                }
            }

            connection.Close();

            return 0;
        }
    }
}
