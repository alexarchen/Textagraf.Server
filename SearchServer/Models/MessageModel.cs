using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SearchServer.Models
{
    public class MessageModel
    {
        private Message self;
        public MessageModel() { self = new Message(); self.DateTime = DateTime.UtcNow; }
        public MessageModel(Message message) { self = message; }

        public int? fromUserId => self.fromUserId;
        public int toUserId => self.toUserId;

        public UserModel fromUser { get => new UserModel(self.fromUser, false); }
        public UserModel toUser { get => new UserModel(self.toUser, false); }

        DateTime DateTime => self.DateTime;

        [MaxLength(1024)]
        public string text { get => self.text; set => self.text = value; }

        public Message.MessageType Type => self.Type;

        public int? groupId => self.groupId;
        public GroupModel group { get => new GroupModel(self.group); }

    }
}
