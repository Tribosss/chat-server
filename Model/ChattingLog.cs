using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_server_csharp.Model
{
    internal class ChattingLog
    {
        public string SenderId { get; set; }
        public string RoomId { get; set; }
        public string Msg { get; set; }
        public string CreatedAt { get; set; }
    }
}
