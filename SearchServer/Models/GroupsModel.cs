using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SearchServer.Models
{
    // Model incapsulates Group in order to protect information

    public class GroupModel
    {
        public GroupModel() { }
        public GroupModel(int? Id = null)
        {

            this.Id = Id;
        }
        public GroupModel(Group gr, bool collections = true)
        {
            Name = gr.Name;
            UniqueName = gr.UniqueName;
            ImageUrl = gr.ImageUrl;
            Tags = gr.Tags;
            Type = gr.Type;
            Access = gr.Access;
            Description = gr.Description;
            Rating = gr.Rating;

            if (collections)
            {
                Participants = gr.Participants?.Select(g => new UserModel(g.User)).ToList();
                Subscribers = gr.Subscribers?.Select(g => new UserModel(g.User)).ToList();
                Admins = gr.Admins?.Select(g => g.User != null ? new UserModel(g.User) : new UserModel(g.UserId)).ToList();
                Documents = gr.Documents?.Where(d=>d.DocStatus==Document.DocStatusEnum.Normal).Select(d => new DocModel(d)).ToList();
                Comments = gr.Comments?.Select(d => new CommentModel(d)).ToList();
            }
            else
            {
                Participants = gr.Participants?.Select(g => new UserModel(g.UserId)).ToList();
                Subscribers = gr.Subscribers?.Select(g => new UserModel(g.UserId)).ToList();
                Admins = gr.Admins?.Select(g => new UserModel(g.UserId)).ToList();
                Documents = gr.Documents?.Where(d => d.DocStatus == Document.DocStatusEnum.Normal).Select(d => new DocModel(d.Id)).ToList();
                Comments = gr.Comments?.Select(d => new CommentModel(d.Id)).ToList();
            }
            Id = gr.Id;
        }

        public static explicit operator Group(GroupModel gr)
        {
            return new Group
            {
                Name = gr.Name,
                Tags = gr.Tags,
                Type = gr.Type,
                Description = gr.Description,
                Access = gr.Access,
                ImageUrl = gr.ImageUrl
            };
        }

        public string Link { get
            {
                if (UniqueName != null) return "/:" + UniqueName;
                else return "/Groups/Id/" + Id;
            }
        }

        public void Update(Group group)
        {
            group.Name = Name;
            group.UniqueName = UniqueName;
            group.Tags = Tags;
            group.Type = Type;
            group.ImageUrl = ImageUrl;
            group.Access = Access;
            group.Description = Description;
        }

        public readonly int? Id;
        [Required]
        [MaxLength(128)]
        public string Name { get; set; }

        [Display(Name="Unique Name")]
        [UIHint("Unique identifier for direct group Url, min 4 symbols, only small latin latters, digits and '-','_' are allowed")]
        [MinLength(4)]
        [MaxLength(32)]
        [RegularExpression(@"[a-z._\-0-9]+",ErrorMessage =  "only small latin latters, digits and '-','_'")]
        public string UniqueName { get; set; }

        public string ImageUrl { get; set; }
        public string Image { get => (ImageUrl != null ? "/"+ImageUrl : "/images/group.png"); }

        public Group.GroupType Type { get; set; }
        [MaxLength(4096)]
        public string Tags { get; set; }
        [MaxLength(1024)]
        public string Description { get; set; }
        public string Access { get; set; }

        public readonly long Rating;

        public string body { get; set; } = "";

        public List<UserModel> Participants { get; }
        public List<UserModel> Subscribers { get; }
        public List<UserModel> Admins { get; }
        public List<DocModel> Documents { get; internal set;  }
        public List<CommentModel> Comments { get; }

    }
}
