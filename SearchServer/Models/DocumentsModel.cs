
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SearchServer.Models
{
    public class DocReadModel
    {
        public DocReadModel() { }
        public DocReadModel(DocumentRead read)
        {
            DocumentId = read.DocumentId;
            UserId = read.UserId;
            Host = read.Host;
            if (read.User != null) User = new UserModel(read.User, false);
        }
        public string Host { get; set; }
        public string DocumentId { get; }
        public int? UserId { get; }
        public UserModel User { get; }
    }

    public class DocLikeModel
    {
        public DocLikeModel(int? userId = null)
        {

            UserId = userId;
        }
        public DocLikeModel(DocumentLike dl)
        {
            UserId = dl.UserId;
            if (dl.User != null) User = new UserModel(dl.User, false);
        }
        public int? UserId { get; set; }
        public UserModel User { get; }
    }
    public class BookmarkModel
    {
        public BookmarkModel() { }
        public BookmarkModel(Bookmark bm)
        {
            UserId = bm.UserId;
            Id = bm.Id;
            DocumentId = bm.DocumentId;
            if (bm.Document!=null)
             Document = new DocModel(bm.Document,false);
            Name = bm.Name;
            Page = bm.Page;
        }
        public long Id { get; }

        public string DocumentId { get; }
        public DocModel Document { get;  }

        public int UserId { get; }

        [MaxLength(256)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string Page { get; set; }
    }

    public class DocModel
    {
        public DocModel(string Id = null)
        {
            this.Id = Id;
        }
        public DocModel() { }
        public DocModel(Document doc, bool collections = true)
        {

            Title = doc.Title;
            Size = doc.Size;
            Tags = doc.Tags;
            GroupId = doc.GroupId;
            Access = doc.Access ?? Document.DocAccess.Open;
            Additional = doc.Additional;
            Description = doc.Description;
            Id = doc.Id;
            DateTime = doc.DateTime;
            ProcessedState = doc.ProcessedState;
            User = doc.User != null ? new UserModel(doc.User, false) : null;
            Group = doc.Group != null ? new GroupModel(doc.Group, false) : null;
            Reads = doc.Reads?.Select(r => new DocReadModel()).ToList();
            Format = doc.File.ToLower().EndsWith(".pdf") ? DocFormat.Pdf : DocFormat.EPub;
            Pages = doc.Pages;
            Url = doc.Url;
            TitlePage = doc.TitlePage;

            if (collections)
            {
                Likes = doc.Likes?.Select(l => new DocLikeModel(l)).ToList();
                Comments = doc.Comments?.Select(c => new CommentModel(c)).ToList();
            }
            else
            {
                Likes = doc.Likes?.Select(l => new DocLikeModel(l.UserId)).ToList();
                Comments = doc.Comments?.Select(c => new CommentModel(c.Id)).ToList();
            }
            Rating = doc.Rating;
        }
        public const int PROCESS_START_VALUE = 7777;

        public readonly string Id;

        public UserModel User { get; }

        public int ProcessedState { get; }
        public bool IsReady { get => ((Format==DocFormat.EPub) || (Pages > 0)) && (ProcessedState != Document.PROCESS_START_VALUE); }

        [Required]
        [MinLength(5)]
        public string Title { get; set; }

        public string Tags { get; set; }

        public readonly DateTime DateTime;

        public Document.DocAccess Access { get; set; } = Document.DocAccess.Open;

        public int? GroupId { get; set; }

        public GroupModel Group { get; set; }

        public string Additional { get; set; }

        public long Size { get; set; }

        public List<DocLikeModel> Likes { get; }
        public List<DocReadModel> Reads { get; }
        public List<BookmarkModel> Bookmarks { get; set;  }
        public List<CommentModel> Comments { get; }

        public long Rating { get; }

        public enum DocFormat
        {
            Pdf = 1,
            EPub = 2
        }
        public DocFormat Format { get; set; }

        public string Url { get; set; }

        public string opfUrl { get; set; }

        public short Pages { get; }

        public DocTitlePageType TitlePage { get; }

        [MaxLength(400)]
        public string Description { get; set; }

        [Display(Name = "Upload Pdf or EPub File", Description = "Select pdf or epub file")]
        [RegularExpression(@".*\.(pdf|PDF|epub|EPUB)")]
        public IFormFile Upload { get; set; }

        [Required]
        public string TempFileName { get; set; }

        public string Thumbnail { get => $"/api/docs/{Id}/thumb/1";  }
        /*
        [Display(Name = "Mobile Pdf File", Description = "Select optional pdf with A6 paper size for mobile devices")]
        [RegularExpression(@".*\.(pdf|PDF)")]
        public IFormFile MobileUpload { get; set; }
        */
        public static implicit operator Document(DocModel m)
        {
            return new Document() { Title = m.Title, Tags = m.Tags, Access = m.Access, GroupId = m.GroupId, Additional = m.Additional, Description = m.Description, Url = m.Url };
        }

        public Document Update(Document doc)
        {
            doc.Title = Title;
            doc.Tags = Tags;
            doc.Additional = Additional;
            doc.Access = Access;
            doc.Description = Description;
            doc.Url = Url;
            return doc;
        }
    }

}