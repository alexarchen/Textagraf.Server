using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SearchServer.Models
{
    public class CommentModel
    {
        public CommentModel(int? Id = null)
        {
            this.Id = Id;
        }

        public readonly int? Id;

        public CommentModel(Comment cmt)
        {
            DateTime = cmt.DateTime;
            Document = cmt.Document != null ? new DocModel(cmt.Document, false) : new DocModel(cmt.DocumentId);
            Group = cmt.Group != null ? new GroupModel(cmt.Group, false) : new GroupModel(cmt.GroupId);
            nDislikes = cmt.nDislikes;
            nLikes = cmt.nLikes;
            Text = cmt.Text;
            User = cmt.User != null ? new UserModel(cmt.User, false) : new UserModel(cmt.UserId);
            Id = cmt.Id;
        }

        public readonly UserModel User;

        public readonly DocModel Document;
        public readonly GroupModel Group;

        public DateTime DateTime { get; set; }

        public int nDislikes { get; set; }

        public int nLikes { get; set; }

        public string Text { get; set; }

    }
}
