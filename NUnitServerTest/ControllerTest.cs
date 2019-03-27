using System;
using System.Collections.Generic;
using System.Text;
using SearchServer.Controllers;
using SearchServer.Models;
using SearchServer;
using System.Threading.Tasks;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Moq.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Http.Internal;

namespace NUnitServerTest
{
    public class ControllerTest : IFileUploader, IDocumentProcessor 
    {

        public Task Upload(string fname, IFormFile file)
        {
            return Task.Factory.StartNew(() => { });
        }
        public void DropFile(string fname)
        {

        }
        public string Folder { get; set; }
        public void ConvertToAvatar(string fname, int cropX, int cropY, int cropSize, string fnameout)
        {

        }

        public void ProcessDocument(string filename, Action<string, ProcessResult, UserContext> onfinish = null)
        {

        }
        public int nGetDocHtml = 0;
        public const string DOCBODY = "DOCBODY";
        public string GetDocHtml(string dir)
        {
            nGetDocHtml++;
            return DOCBODY;
        }
        public byte[] GetThumbnail(string file, int n, int crop = 0)
        {
            return new byte[] { 0, 0 };
        }


        // Moqs
        protected Mock<SignInManager<User>> smngrMoq;
        protected Mock<IMemoryCache> memMoq;
        protected Mock<UserManager<User>> umngrMoq;

        protected HttpContext HttpContext => HttpContextMoq.Object;
        protected HttpRequest HttpRequest => HttpRequestMoq.Object;
        protected HttpResponse HttpResponse => HttpResponseMoq.Object;

        protected Mock<HttpContext> HttpContextMoq;
        protected Mock<HttpRequest> HttpRequestMoq;
        protected Mock<HttpResponse> HttpResponseMoq;
        protected Mock<IHeaderDictionary> ResponseHeadersMoq;
        protected Mock<ControllerContext> ControllerContextMoq;

        protected Mock<ClaimsPrincipal> UserMoq;



        protected UserManager<User> umngr;

        protected List<User> users;
        protected List<Group> groups;
        protected List<Document> docs;
        protected List<GroupSubscriber> subscribers;
        protected List<GroupUser> participants;

        protected DbContextOptions<UserContext> options;

        protected User Admin;

        UserContext _context;

        public UserContext GetContext()
        {
            if (_context == null) _context = new UserContext(options,memMoq.Object);
            return _context; // (new UserContext(options));
        }

        [OneTimeSetUp]
        public void OneSetup()
        { 
            options = new DbContextOptionsBuilder<UserContext>()
                            .UseInMemoryDatabase(databaseName: "DB"+this.GetType().Name)
                            .Options;

            // Two users: Admin, user
            // Entities Mocks
            _context = new UserContext(options, memMoq.Object);
            
                Admin = new User() {
                //    Id = 1,
                    UserName = "Admin", EmailConfirmed = true
                };
                User user = new User() {
                    //Id = 2,
                    UserName = "__Test__", EmailConfirmed = true };
                User user2 = new User() {
                    //Id = 3,
                    UserName = "__Test2__", EmailConfirmed = true };
                User user3 = new User() {
                    //Id = 4,
                    UserName = "__Test3__", EmailConfirmed = true };
                users = new List<User>(new User[] { Admin, user, user2, user3 });

                groups = new List<Group>(new Group[] {
                new Group (){
                    // Id = 1,
                     Creator = Admin,
                     Type = Group.GroupType.Open,
                     Admins = new List<GroupAdmin>(new GroupAdmin[]{ new GroupAdmin(){
                         UserId = Admin.Id, GroupId = 1, User = Admin},
                     new GroupAdmin(){
                         UserId = user.Id, GroupId = 1, User = user}
                     }),
                     Participants = new List<GroupUser>(new GroupUser[]{

                     }),
                     Subscribers = new List<GroupSubscriber>(new GroupSubscriber[]{

                         new GroupSubscriber (){ GroupId = 1, UserId = users[2].Id, User = users[2] }
                     })
                },
                new Group (){
                    // Id = 2,
                     Creator = Admin,
                     Type = Group.GroupType.Closed,
                     Admins = new List<GroupAdmin>(new GroupAdmin[]{ new GroupAdmin(){
                         UserId = Admin.Id, GroupId = 2, User = Admin} }),
                     Participants = new List<GroupUser>(new GroupUser[]{
                         new GroupUser(){ GroupId = 2, UserId = 2, User = user},
                         new GroupUser(){ GroupId = 2, UserId = 3, User = user2}
                     })
                },
                new Group (){
                    // Id = 3,
                     Creator = Admin,
                     Type = Group.GroupType.Private,
                     Admins = new List<GroupAdmin>(new GroupAdmin[]{ new GroupAdmin(){
                         UserId = Admin.Id, GroupId = 3, User = Admin} }),
                     Participants = new List<GroupUser>(new GroupUser[]{
                         new GroupUser(){ GroupId = 3, UserId = 2, User = user}
                     })
                }
                });

                groups[0].Admins[0].Group = groups[0];
                groups[0].Admins[1].Group = groups[0];
                groups[0].Subscribers[0].Group = groups[0];

                groups[1].Admins[0].Group = groups[1];
                groups[1].Participants[0].Group = groups[1];

                groups[2].Admins[0].Group = groups[2];
                groups[2].Participants[0].Group = groups[2];

                Document doc = new Document()
                {
                    Id = "DOC",
                    File = "1/DOC/.pdf",
                    Title = "Title",
                    Tags = "",
                    UserId = 2,
                    GroupId = null,
                    Group = null,
                    Pages = 1
                };
                Document opendoc = new Document()
                {
                    Id = "TEST",
                    File = "1/TEST/.pdf",
                    Title = "Title",
                    Tags = "",
                    UserId = 2,
                    GroupId = 1,
                    Group = groups[0],
                    Pages = 1
                };
                Document closedoc = new Document()
                {
                    Id = "CLOSED",
                    File = "1/CLOSED/.pdf",
                    Title = "Title",
                    Tags = "",
                    GroupId = 2,
                    UserId = 3,
                    Group = groups[1],
                    Pages = 1
                };
                Document privdoc = new Document()
                {
                    Id = "PRIV",
                    File = "1/PRIV/.pdf",
                    Title = "Title",
                    Tags = "",
                    GroupId = 3,
                    UserId = 2,
                    Group = groups[2],
                    Pages = 1
                };
                Document[] docs = new Document[] { doc, opendoc, closedoc, privdoc };

                foreach (User u in users)
                {
                    _context.Add(u);
                }

                subscribers = new List<GroupSubscriber>();
                participants = new List<GroupUser>();

                foreach (Group g in groups)
                {
                    _context.Group.Add(g);

                    if (g.Admins != null)
                        foreach (GroupAdmin a in g.Admins)
                        {
                            _context.GroupAdmin.Add(a);
                            Assert.AreEqual(g.Id, a.GroupId);
                        }

                    if (g.Participants != null)
                        foreach (GroupUser u in g.Participants)
                        {
                            participants.Add(u);
                            _context.GroupUser.Add(u);
                            Assert.AreEqual(g.Id, u.GroupId);
                        }
                    if (g.Subscribers != null)
                        foreach (GroupSubscriber s in g.Subscribers)
                        {
                            subscribers.Add(s);
                            _context.GroupSubscriber.Add(s);
                            Assert.AreEqual(g.Id, s.GroupId);
                        }

                }

                foreach (Document d in docs)
                {
                    d.User = users.Find(u => u.Id == d.UserId);
                    if (d.GroupId != null)
                        Assert.AreEqual(d.Group.Id, d.GroupId);

                    _context.Document.Add(d);
                }

                _context.SaveChanges();

            }

        [SetUp]
        public void Setup()
        {

            memMoq = new Mock<IMemoryCache>();
            umngrMoq = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
            umngr = umngrMoq.Object;
            smngrMoq = new Mock<SignInManager<User>>(umngr, new Mock<IHttpContextAccessor>().Object, new Mock<IUserClaimsPrincipalFactory<User>>().Object, null, null, null);


            HttpContextMoq = new Mock<HttpContext>();
            HttpRequestMoq = new Mock<HttpRequest>();
            HttpResponseMoq = new Mock<HttpResponse>();


            HttpContextMoq.Setup(c => c.Response).Returns(HttpResponseMoq.Object);
            HttpContextMoq.Setup(c => c.Request).Returns(HttpRequestMoq.Object);
            Mock<ConnectionInfo> conn = new Mock<ConnectionInfo>();
            conn.Setup(c => c.RemoteIpAddress).Returns(new System.Net.IPAddress(1));
            HttpContextMoq.Setup(c => c.Connection).Returns(conn.Object);
            UserMoq = new Mock<ClaimsPrincipal>();
            HttpContextMoq.Setup(h => h.User).Returns(UserMoq.Object);

            HttpResponseMoq.Setup(r => r.HttpContext).Returns(HttpContextMoq.Object);
            HttpRequestMoq.Setup(r => r.HttpContext).Returns(HttpContextMoq.Object);
            HttpRequestMoq.Setup(r => r.Body).Returns(RequestStream);
            HttpRequestMoq.SetupGet(r => r.QueryString).Returns(()=>qs);
            
            HttpResponseMoq.Setup(r => r.Body).Returns(ResponseStream);
            ResponseHeadersMoq = new Mock<IHeaderDictionary>();
            HttpResponseMoq.Setup(r => r.Headers).Returns(ResponseHeadersMoq.Object);

            ControllerContextMoq = new Mock<ControllerContext>();
            ControllerContextMoq.Object.HttpContext = HttpContext;



        }

        protected QueryString qs = new QueryString();

        protected MemoryStream RequestStream = new MemoryStream();
        protected MemoryStream ResponseStream = new MemoryStream();

        virtual public void SetHttpRequest(string method, string path)
        {
            HttpRequestMoq.Setup(r => r.Method).Returns(method);
            HttpRequestMoq.Setup(r => r.Path).Returns(path);
            HttpRequestMoq.Setup(r => r.QueryString).Returns(new QueryString());
        }


        public void SetupController(Controller controller)
        {
            controller.ControllerContext = ControllerContextMoq.Object;
            var url = new Mock<IUrlHelper>();
            controller.Url = url.Object;
        }

        public void LogIn(User user)
        {
            smngrMoq.Reset();
            UserMoq.Reset();
            umngrMoq.Reset();
            smngrMoq.Setup(a => a.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(true);
            UserMoq.Setup(u => u.IsInRole("Admin")).Returns(user.Id == 1);
            umngrMoq.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).Returns(Task.FromResult(user));
            umngrMoq.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id.ToString());

        }

        public void LogOut()
        {
            smngrMoq.Reset();
            UserMoq.Reset();
            umngrMoq.Reset();
            smngrMoq.Setup(a => a.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(false);
            UserMoq.Setup(u => u.IsInRole("Admin")).Returns(false);
            umngrMoq.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).Returns(Task.FromResult((User)null));
            umngrMoq.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns<int?>(null);
        }


        [TearDown]
        public void TearDown()
        {

            ResponseStream.Dispose();
            RequestStream.Dispose();
        }


        public static void MakeVerified(ModelStateDictionary modelState)
        {
            foreach (var entry in modelState.Keys)
            {
                modelState[entry].Errors.Clear();
                modelState[entry].ValidationState = ModelValidationState.Valid;
            }
        }
    }
}
