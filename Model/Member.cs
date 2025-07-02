using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_server.Model
{
    internal class Member
    {
        public string RoomId { get; set; }
        public string EmpId { get; set; }
        public string JoinedAt { get; set; }
    }
}
