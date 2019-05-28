using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SearchServer.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using SearchServer.Controllers;
using System.Threading;

namespace SearchServer.Models
{
    public interface IRated
    {
        bool CalcRating(out long Rating);
        long Rating { get; set; }
    }

    public static class ModelHelpers
    {
        
        public static async Task RefreshRating<T>(this T entity, UserContext dbContext) where T : class, IRated
        {
            long r;
            if (entity.CalcRating(out r)) {
                entity.Rating = r;
            }
            dbContext.Update(entity);
            await dbContext.SaveChangesAsync();

        }
    }

    public class User : IdentityUser<int>, IRated
    {
        [Required]
        [MinLength(3)]
        [RegularExpression("[A-Za-z0-9._-]+")]
        public override string UserName {get; set;}

        public List<Document> Documents { get; set; } = new List<Document>();
        // Many-to-one
        public List<Comment> Comments { get; set; } = new List<Comment>();
        // Many-to-Many
        // subscribers to current user
        public List<UserSubscriber> Subscribers { get; set; } = new List<UserSubscriber>();
        // Many-to-Many
        public List<GroupSubscriber> SubscribesToGroups { get; } = new List<GroupSubscriber>();
        // Many-to-Many
        public List<UserSubscriber> SubscribesToUsers { get; } = new List<UserSubscriber>();
        // 
        public List<DocumentLike> Likes { get; set; } = new List<DocumentLike>();

        public List<GroupAdmin> AdminOfGroups { get; set; } = new List<GroupAdmin>();

        public List<Group> CreatedGroups { get; set; } = new List<Group>();

        public List<GroupUser> Participate { get; set; } = new List<GroupUser>();

        public List<Message> Messages { get; set; } = new List<Message>();

        public List<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

        public List<Group> GetGroupsAdminList()
        {
            return AdminOfGroups.Select(a => a.Group).ToList();
        }
        public List<Group> GetAllGroupsList()
        {
            return AdminOfGroups.Select(a => a.Group)
                .Concat(Participate.Select(gu => gu.Group))
                .Concat(SubscribesToGroups.Select(gu => gu.Group)).ToList();
        }

        public DateTime DateTime { get; set; }

        public string Info { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string Banned { get; set; }

        public long Rating { get; set; }

        public bool CalcRating(out long r)
        {
            if ((Subscribers != null) && (Documents != null))
            {
                long rr = Subscribers.Count();
                Documents.ForEach(d => {
                    long dr = 0;
                    if (d.CalcRating(out dr))
                    {
                        d.Rating = dr;
                    }

                    rr += d.Rating; });
                r = rr;
                return true;
            }
            r = 0;
            return false;
        }

        public List<PayedSubscribe> PayedSubscribes { get; set; } = new List<PayedSubscribe>();
    }

    public class PayedSubscribe
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string DocumentId { get; set; }
        public Document Document { get; set; }

        public enum SubscribeType {
            PersonalStorage = 1, // personal storage of documents, ut to 100 private groups, up to 1 Gb total space
            PromoteDoc = 2,    // promote document among others
            Publisher = 3   // publish a lot of material
        }

        public class SubscribeOptions
        {
            public int NumberOfPrivateGroups=5;
            public int AllowedPrivateSpaceMb = 500;
            public bool PromoteThisDocument = false;
            public int MaxUploadsPerDay = 5;
            public int MaxFileSizeMb = 50;
        }
        
        public SubscribeOptions getOptions()
        {

            SubscribeOptions opt = new SubscribeOptions();

            switch (Type)
            {
                case SubscribeType.PersonalStorage:
                    opt.NumberOfPrivateGroups = 100;
                    opt.MaxFileSizeMb = 300;
                break;
                case SubscribeType.PromoteDoc:
                    opt.PromoteThisDocument = true;
                break;
                case SubscribeType.Publisher:
                    opt.MaxUploadsPerDay = 100;
                    opt.MaxFileSizeMb = 100;
                break;
            }

            return opt;
        }

        public bool IsValid()
        {
            return ((DateTime.UtcNow >= StartTime) && (DateTime.UtcNow <= EndTime));
        }

        public SubscribeType Type { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
    }

    /* Unity of pepole */
    public class Group: IRated
    {
        [Required]
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [MaxLength(32)]
        public string UniqueName { get; set; }

        public string Tags{ get; set; }

        public Category Category { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        // Closed - read for partcipants only, everyone can be participant
        // Private - read for partcipants only, Admin adds participants
        // Personal - only for admins
        public enum GroupType {
            [Display(Name = "Public")]
            Open =1,
            [Display(Name = "Closed")]
            Closed =2,
            [Display(Name = "Private")]
            Private =3,
            [Display(Name = "Blog")]
            Blog = 4,
            [Display(Name ="Personal")] // mycloud
            Personal
        }
        public GroupType Type {get; set; }

        // who created this group
        public User Creator { get; set; }
        // M2M, who administrate this group
        public List<GroupAdmin> Admins { get; set; } = new List<GroupAdmin>();
        // M2M, who consist in this group and has rights to publish
        public List<GroupUser> Participants { get; set; } = new List<GroupUser>();
        // M2M, who subscried to this group
        public List<GroupSubscriber> Subscribers { get; set; } = new List<GroupSubscriber>();
        // O2M, documents of this group
        public List<Document> Documents { get; set; } = new List<Document>();

        public List<Comment> Comments { get; set; } = new List<Comment>();

        public string Access { get; set; }

        public long Rating { get; set; }

        public bool CalcRating(out long r)
        {
            r = 0;
            if ((Subscribers != null) && (Participants != null))
            {
                r = Participants.Count + Subscribers.Count + Documents.Count;
                return true;
            }
            return false;
        }
    }
    public class DocumentLike
    {
        public DocumentLike() { }
        public DocumentLike(int UserId, string DocumentId)
        {
            this.UserId = UserId;
            this.DocumentId = DocumentId;
        }

        [Required]
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User User { get; set; }

        public string DocumentId { get; set; }
        public Document Document { get; set; }

    }

    // table to bind Group <Many-to-many> User 
    public class GroupSubscriber
    {
        [Required]
        [Key]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [Key]
        public int GroupId { get; set; }
        public Group Group { get; set; }

    }
    // table to bind User <Many-to-many> User by subscriptions
    public class UserSubscriber
    {
        [Required]
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int ToUserId { get; set; }
        public User ToUser { get; set; }

    }
    // table to bind Group <Many-to-many> User by Participants
    public class GroupUser
    {
        public GroupUser() { }
        public GroupUser(User user,Group group)
        {
            UserId = user.Id;
            GroupId = group.Id;
        }

        [Required]
        [Key]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [Key]
        public int GroupId { get; set; }
        public Group Group { get; set; }

    }
    // table to bind Group <Many-to-many> User by Admins
    public class GroupAdmin
    {
        public GroupAdmin() { }
        public GroupAdmin(User user, Group group)
        {
            UserId = user.Id;
            GroupId = group.Id;
        }

        [Required]
        [Key]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [Key]
        public int GroupId { get; set; }
        public Group Group { get; set; }
    }

    public enum DocTitlePageType : byte
    {
        BWText=0, // BW text without title
        BWTitle, // BW with title
        Color
    }

    public class Document : IRated
    {
        [Required]
        [Key]
        public string Id { get; set; }

        public string Title { get; set; }

        public string Tags { get; set; }

        public string Description { get; set; }

        [Required]
        public string File { get; set; }

        public const int PROCESS_START_VALUE = 7777;
        public int ProcessedState { get; set; }

        public const int READ_USER = 1;
        public const int READ_GROUP = 4;
        public const int ALL_USER = 3;
        public const int DOWN_USER = 2;
        public const int DOWN_GROUP = 8;
        public const int ALL_GROUP = 12;

        public enum DocAccess
        {
            [Display(Name = "Open")]
            Open = ALL_USER| ALL_GROUP,
            //[Display(Name = "Open, Download for Group only")]
            //GropuDownload = READ_USER | ALL_GROUP,
            [Display(Name = "No downloads")]
            Readonly = READ_USER | READ_GROUP,
            //[Display(Name = "Group only")]
            //Group = ALL_GROUP,
            //[Display(Name = "Group Read only")]
            //GroupRead = READ_GROUP,
            //[Display(Name = "Private")]
            //Private = 0
        }
        public DocAccess? Access { get; set; } = DocAccess.Open;

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        // Personal or unity document, can be null
        public int? GroupId { get; set; }
        public Group Group { get; set; }

        public List<DocumentLike> Likes { get; set; } = new List<DocumentLike>();

        public DateTime DateTime { get; set; }

        public string Additional { get; set; }

        public string Url { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();

        public int Downloads { get; set; }

        public List<DocumentRead> Reads { get; set; } = new List<DocumentRead>();

        public long Rating { get; set; }

        public long Size { get; set; } // size of initial file in bytes

        public short Pages { get; set; } // number of pages in document (for PDF)

        public enum DocStatusEnum : byte
        {
            Normal=0,
            Deleted,  // deleted permanently (but exists in database)
            Invisible // not visible in any group
        }
        public DocStatusEnum DocStatus { get; set; }

        public DocTitlePageType TitlePage { get; set; } // 

        public bool CalcRating(out long r) {

            r = 0;
            if ((Likes != null) && (Reads != null))
            {
                r = Likes.Count * 100 + Reads.GroupBy(read => read.UserId).Count();
                return true;
            }

            return false;
        }

       }

     /// <summary>
    /// DocumentInfo needed for fast load information about document and access rights to it
    /// </summary>
    public class DocumentInfo
    {
    
        public string Id { get; set; }

        public string File { get; set; }

        public int ProcessedState { get; set; }

        public Document.DocAccess? Access { get; set; }

        public int UserId { get; set; }

        public int? GroupId { get; set; }

        public long Size { get; set; } // size of initial file in bytes

        public short Pages { get; set; } // number of pages in document (for PDF)

        public int? IsGAdmin { get; set; }

        public int? IsGUser { get; set; }

        public Group.GroupType? GroupType { get; set; }
    }

    public class DocumentRead
    {
       
        [Key]
        public long Id { get; set; }

        [Required]
        public string DocumentId { get; set; }
        public Document Document { get; set; }

        public int? UserId { get; set; }
        public User User { get; set; }

        [MaxLength(256)]
        public string Host { get; set; }

        public DateTime DateTime { get; set; }
    }

    public class Category
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

    }

    public class Comment
    {
        public Comment() { }
        public Comment(string docId,int userId,string Text)
        {
            DocumentId = docId;
            UserId = userId;
            this.Text = Text;
            nLikes = nDislikes = 0;
            DateTime = DateTime.UtcNow;
        }

        public Comment(int groupId, int userId, string Text)
        {
            GroupId = groupId;
            UserId = userId;
            this.Text = Text;
            nLikes = nDislikes = 0;
            DateTime = DateTime.UtcNow;
        }


        [Key]
        [Required]
        public long Id { get; set; }

       // public long? ParentId { get; set; } // for later use

        public string DocumentId { get; set; }
        public Document Document { get; set; }

        public int? GroupId { get; set; }
        public Group Group { get; set; }

        public int? ToUserId { get; set; }
        public User ToUser { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [DataType(DataType.Html)]
        public string Text { get; set; }

        public DateTime DateTime { get; set; }

        public int nLikes { get; set; }
        public int nDislikes { get; set; }
    }

public class Message
    {
        public Message() { }

        [Key]
        public int Id { get; set; }

        public int? fromUserId { get; set; }
        public User fromUser { get; set; }
        public int toUserId { get; set; }
        public User toUser { get; set; }
        public DateTime DateTime { get; set; }
        public enum MessageType: byte
        {
            Message=0,
            ParticipateGroup,
            ParticipateGroupAccepted,
            ParticipateGroupDeclined,
            DocRemoved // Your Document has been removed from group
        }
        public MessageType Type { get; set; }

        [MaxLength(1024)]
        public string text { get; set; }

        public int? groupId { get; set; }
        public Group group { get; set; }
    }


    public class Bookmark
    {
        [Key]
        public long Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string DocumentId { get; set; }
        public Document Document{ get; set; }

        [MaxLength(256)]
        public string Page { get; set; }

        [MaxLength(256)]
        public string Name { get; set; }

        public DateTime DateTime {get; set;}
    }


    public class UserContext : IdentityDbContext<User,IdentityRole<int>,int>
    {
        IMemoryCache _cache;

        public const string IN_MEM_DATABASE = "Microsoft.EntityFrameworkCore.InMemory";

        public UserContext(DbContextOptions<UserContext> opt, IMemoryCache memoryCache) : base(opt)
        {
            _cache = memoryCache;
            if (!IsInMem)
            {
                Database.Migrate();
            }
            else useCache = false;
        }

        public bool IsInMem { get => Database.ProviderName.Equals("Microsoft.EntityFrameworkCore.InMemory"); }

        /// <summary>
        /// Load full user information, must be used with cache
        /// </summary>
        /// <returns></returns>
        /// 
        public IQueryable<User> GetFullUserLinq()
        {
            return User.Include(u => u.Documents).Include(u => u.Subscribers).ThenInclude(us => us.User)
                           .Include(u => u.SubscribesToGroups).ThenInclude(gs => gs.Group)
                           .Include(u => u.SubscribesToUsers).ThenInclude(us => us.User)
                           .Include(u => u.Participate).ThenInclude(gu => gu.Group)
                           .Include(u => u.AdminOfGroups).ThenInclude(gu => gu.Group)
                           .Include(u => u.Likes).ThenInclude(l=>l.Document)
                           .Include(u => u.Messages)
                           .Include(u => u.Bookmarks);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            builder.Query<DocumentInfo>();

//              builder.Entity<DocumentLike>().HasKey(d=>new { d.UserId, d.DocumentId });
            builder.Entity<GroupAdmin>().HasKey(g => new { g.UserId, g.GroupId});
            builder.Entity<GroupUser>().HasKey(u => new { u.UserId, u.GroupId});
            builder.Entity<GroupSubscriber>().HasKey(s => new { s.UserId, s.GroupId });
            //builder.Entity<UserSubscriber>().HasKey(d => new { d.UserId, d.ToUserId});

            builder.Entity<Document>().HasMany(d => d.Comments).WithOne(c => c.Document).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Document>().HasMany(d => d.Likes).WithOne(l => l.Document).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Document>().HasMany(d => d.Reads).WithOne(r => r.Document).OnDelete(DeleteBehavior.Cascade);
            // User relations
            builder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
            builder.Entity<User>().HasMany<UserSubscriber>(u=>u.Subscribers).WithOne(us=>us.ToUser).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<User>().HasMany<Group>(u => u.CreatedGroups).WithOne(g => g.Creator).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<User>().HasMany<GroupSubscriber>(u => u.SubscribesToGroups).WithOne(gs => gs.User).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<User>().HasMany<UserSubscriber>(u => u.SubscribesToUsers).WithOne(us => us.User).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<User>().HasMany<GroupUser>(u => u.Participate).WithOne(gu => gu.User).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<User>().HasMany<DocumentLike>(u => u.Likes).WithOne(dl => dl.User).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<User>().HasMany<Document>(u => u.Documents).WithOne(d => d.User).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<User>().HasMany<GroupAdmin>(u => u.AdminOfGroups).WithOne(ga => ga.User).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<User>().HasMany<DocumentLike>(u => u.Likes).WithOne(dl => dl.User).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<User>().HasMany<PayedSubscribe>(u => u.PayedSubscribes).WithOne(ps => ps.User).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<User>().HasMany<Message>(u => u.Messages).WithOne(m => m.toUser).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<User>().HasMany<Comment>(u => u.Comments).WithOne(c => c.ToUser).OnDelete(DeleteBehavior.Restrict);
            // Group relations
            builder.Entity<Group>().HasMany<Document>(g=>g.Documents).WithOne(d=>d.Group).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Group>().HasMany<GroupAdmin>(g=>g.Admins).WithOne(a=>a.Group).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Group>().HasMany<GroupUser>(g => g.Participants).WithOne(gu => gu.Group).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Group>().HasMany<GroupSubscriber>(g => g.Subscribers).WithOne(gs => gs.Group).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Group>().HasMany<Comment>(g => g.Comments).WithOne(c => c.Group).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Group>().HasIndex(g => g.UniqueName).IsUnique(true);
            // Message
            builder.Entity<Message>().HasIndex(m => m.toUserId);
            // Bookmark
            builder.Entity<Bookmark>().HasIndex("DocumentId");
            builder.Entity<Bookmark>().HasIndex("DocumentId", "UserId");

            builder.Entity<DocumentRead>().HasIndex("UserId");
            builder.Entity<DocumentRead>().HasOne(dr => dr.Document).WithMany(d=>d.Reads);


        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        bool useCache = true;


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            this.ChangeTracker.DetectChanges();
            if (ChangeTracker.Entries().Where(db => (db.State != EntityState.Unchanged)).Select(db => db.Entity.GetType()).Intersect(new Type[] { typeof(User), typeof(Document), typeof(Group), typeof(GroupAdmin), typeof(GroupUser), typeof(Message) }).Count() > 0)
            {
                GetFullUserLinq().ClearCache(_cache);

            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            if (ChangeTracker.Entries().Where(db => (db.State != EntityState.Unchanged)).Select(db => db.Entity.GetType()).Intersect(new Type[] { typeof(User), typeof(Document), typeof(Group), typeof(GroupAdmin), typeof(GroupUser), typeof(Message)}).Count()>0)
            {
                GetFullUserLinq().ClearCache(_cache);
            }

            /*
            if (useCache)
            {
                this.ChangeTracker.DetectChanges();
                
                var changedEntityNames = this.GetChangedEntityNames();

                var result = base.SaveChanges();
                this.GetService<IEFCacheServiceProvider>().InvalidateCacheDependencies(changedEntityNames);

                return result;
            }
            */
            return base.SaveChanges();
        }

        virtual public DbSet<User> User { get; set; }

        virtual public DbSet<Document> Document { get; set; }

        virtual public DbSet<Group> Group { get; set; }

        virtual public DbSet<GroupUser> GroupUser { get; set; }

        virtual public DbSet<GroupAdmin> GroupAdmin { get; set; }

        virtual public DbSet<GroupSubscriber> GroupSubscriber { get; set; }

        virtual public DbSet<UserSubscriber> UserSubscriber { get; set; }

        virtual public DbSet<DocumentLike> DocumentLike { get; set; }

        virtual public DbSet<Comment> Comment { get; set; }

        virtual public DbSet<Message> Message { get; set; }

        virtual public DbQuery<DocumentInfo> DocumentInfo { get; set; }

        virtual public DbSet<Bookmark> Bookmark { get; set; }

        virtual public DbSet<DocumentRead> DocumentRead { get; set; }
    }


}
