using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SearchServer.Models
{
    public class UserModel
    {
        public static string AvatarStorage = "/";
        public UserModel() { }
        public UserModel(int? Id = null)
        {
            this.Id = Id;
        }
        public UserModel(User user, bool collections = true)
        {   if (user == null) return;
            Name = user.UserName;
            Info = user.Info;
            // define load avatart url
            if (user.ImageUrl != null)
                ImageUrl = (AvatarStorage + user.ImageUrl);
            Banned = user.Banned;
            Id = user.Id;
            if (collections)
            {
                Subscribers = user.Subscribers?.Select(s => new UserModel(s.User, false)).ToList();
                SubscribesToGroups = user.SubscribesToGroups?.Select(gs => new GroupModel(gs.Group, false)).ToList();
                SubscribesToUsers = user.SubscribesToUsers?.Select(us => us.ToUser != null ? new UserModel(us.ToUser, false) : new UserModel(us.ToUserId)).ToList();
                Documents = user.Documents?.Where(d => d.DocStatus != Document.DocStatusEnum.Deleted).Select(d => new DocModel(d, false)).ToList();
                Likes = user.Likes?.Select(l => new DocModel(l.Document, false)).ToList();
                AdminOfGroups = user.AdminOfGroups?.Select(gs => new GroupModel(gs.Group, false)).ToList();
                Participate = user.Participate?.Select(gs => new GroupModel(gs.Group, false)).ToList();
                Bookmarks = user.Bookmarks?.Select(bm => new BookmarkModel(bm)).ToList();
            }
            else
            {
                // only ids
                Subscribers = user.Subscribers?.Select(s => { var um = new UserModel(s.UserId); return um; }).ToList();
                SubscribesToGroups = user.SubscribesToGroups?.Select(gs => { var gm = new GroupModel(gs.GroupId); return gm; }).ToList();
                SubscribesToUsers = user.SubscribesToUsers?.Select(us => { var um = new UserModel(us.ToUserId); return um; }).ToList();
                Documents = user.Documents?.Where(d => d.DocStatus == Document.DocStatusEnum.Normal).Select(d => { var dm = new DocModel(d.Id); return dm; }).ToList();
                Likes = user.Likes?.Select(l => new DocModel(l.DocumentId)).ToList();
                AdminOfGroups = user.AdminOfGroups?.Select(gs => new GroupModel(gs.GroupId)).ToList();
                Participate = user.Participate?.Select(gs => new GroupModel(gs.GroupId)).ToList();
                Bookmarks = user.Bookmarks?.Select(bm => new BookmarkModel()).ToList();
            }
            Rating = user.Rating;
        }

        [Required]
        [MaxLength(32)]
        [MinLength(3)]
        [RegularExpression(@"[a-zA-Z-.0-9_]+", ErrorMessage ="Only latin letters, digits, lowline, dot and hyphen can be used")]
        public string Name { get; set; }

        [DataType(DataType.Html)]
        [MaxLength(1024)]
        public string Info { get; set; }

        public string Banned { get; set; }

        [DataType(DataType.ImageUrl)]
        public string ImageUrl { get; set; }
        public string Image { get => (ImageUrl != null ? ImageUrl : "/images/no_avatar.png"); }

        public int? Id { get; set; }

        public List<UserModel> Subscribers { get; }
        public List<UserModel> SubscribesToUsers { get; }
        public List<GroupModel> SubscribesToGroups { get; }
        public List<DocModel> Documents { get; }
        public List<DocModel> Likes { get; }
        public List<GroupModel> Participate { get; }
        public List<GroupModel> AdminOfGroups { get; }
        public List<BookmarkModel> Bookmarks { get; }

        public readonly long Rating;

        public void Update(User user)
        {
            user.UserName = Name;
            user.Info = Info;

        }

    }

    public class UserModelEx: UserModel
    {
        public UserModelEx(User user, bool collections = true):base(user,collections)
        {

        }
        public bool CanEdit { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsMe { get; set; }

        public bool IsSubscribed { get; set; }

    }

}
