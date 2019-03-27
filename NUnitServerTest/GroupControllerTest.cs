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
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq;

namespace NUnitServerTest
{
    [TestFixture]
    [TestOf(typeof(GroupsController))]
    public class GroupsControllerTest : ControllerTest
    {
        GroupsController controller;

        [SetUp]
        public void SetUp()
        {
            UserContext _context = GetContext();
            
                controller = new GroupsController(
                _context,
                smngrMoq.Object,
                memMoq.Object
                );

            SetupController(controller);

            SetHttpRequest("GET", "/");
        }

        [Test]
        public async Task IdTest()
        {
            UserContext _context = GetContext();
            _context.Document.Add(new Document() {
                Id = "GRT47895347",
                GroupId = 1,
                UserId = 1,
                Tags = "Abc",
                File = ""
            });
            _context.Document.Add(new Document()
            {
                Id = "GRTfgfgf5",
                GroupId = 1,
                UserId = 1,
                Tags = "Arr",
                File = ""
            });
            _context.Document.Add(new Document()
            {
                Id = "GRT43765yuerfh",
                GroupId = 1,
                UserId = 2,
                Tags = "Abc; Bcd;",
                File = ""
            });
            await _context.SaveChangesAsync();

            LogOut();
            IActionResult res = await controller.Id(1,null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 1);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsParticipant"]);
            res = await controller.Id(200, null);
            Assert.IsInstanceOf<NotFoundResult>(res);
            res = await controller.Id(2, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<GroupModel>(((ViewResult)res).Model);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsParticipant"]);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 2);
            res = await controller.Id(3, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsFalse ((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsParticipant"]);

            LogIn(Admin);
            res = await controller.Id(1, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);
            // Test number
            Assert.AreEqual(_context.Document.Where(d=>d.GroupId==1).Count(), ((GroupModel)((ViewResult)res).Model).Documents.Count);
            // Test tags
            qs = new QueryString("?#Abc");
            HttpRequestMoq.SetupGet(r => r.QueryString).Returns(qs);
            res = await controller.Id(1, null);
            Assert.AreEqual(2, ((GroupModel)((ViewResult)res).Model).Documents.Count);
            qs = new QueryString("?#bcd");
            HttpRequestMoq.SetupGet(r => r.QueryString).Returns(qs);
            res = await controller.Id(1, null);
            Assert.AreEqual(1, ((GroupModel)((ViewResult)res).Model).Documents.Count);
            qs = new QueryString("?#Arr");
            HttpRequestMoq.SetupGet(r => r.QueryString).Returns(qs);
            res = await controller.Id(1, null);
            Assert.AreEqual(1, ((GroupModel)((ViewResult)res).Model).Documents.Count);
            qs = new QueryString("?#sdfsdfg5r");
            HttpRequestMoq.SetupGet(r => r.QueryString).Returns(qs);
            res = await controller.Id(1, null);
            Assert.AreEqual(0, ((GroupModel)((ViewResult)res).Model).Documents.Count);
            qs = new QueryString();
            HttpRequestMoq.SetupGet(r => r.QueryString).Returns(qs);


            res = await controller.Id(2, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<GroupModel>(((ViewResult)res).Model);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 2);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);
            res = await controller.Id(3, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 3);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);

            LogIn(users[1]);
            res = await controller.Id(1, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 1);
            res = await controller.Id(2, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<GroupModel>(((ViewResult)res).Model);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 2);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);
            res = await controller.Id(3, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 3);

            LogIn(users[2]);
            res = await controller.Id(1, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsParticipant"]);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 1);
            res = await controller.Id(2, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<GroupModel>(((ViewResult)res).Model);
            Assert.AreEqual(((GroupModel)((ViewResult)res).Model).Id, 2);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsTrue((bool)((ViewResult)res).ViewData["IsParticipant"]);
            res = await controller.Id(3, null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanRead"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["CanEdit"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsAdmin"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsSubscribed"]);
            Assert.IsFalse((bool)((ViewResult)res).ViewData["IsParticipant"]);

        }

        [Test]
        public async Task CreateTest()
        {
            IActionResult res;
            GroupModel gm = new GroupModel(100)
            {
                Tags = "TAGS",
                Name = "NAME",
                Type = Group.GroupType.Open
            };


            /*Rely on [Authorize]
            LogOut();
            res = controller.Create();
            Assert.IsInstanceOf<ForbidResult>(res); 
            res = await controller.Create(gm);
            Assert.IsInstanceOf<ForbidResult>(res);
            */
            LogIn(users[1]);
            res = controller.Create();
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<GroupModel>(((ViewResult)res).Model);
            int c = groups.Count;
            res = await controller.Create(gm);
            Assert.IsInstanceOf<RedirectToActionResult>(res);
            Assert.AreEqual(c+1,(await GetContext().Group.LastAsync()).Id);

            c = await GetContext().Group.CountAsync();
            controller.ModelState.AddModelError("Name", "ERR");
            res = await controller.Create(gm);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<GroupModel>(((ViewResult)res).Model);
            Assert.AreEqual(c, await GetContext().Group.CountAsync());

        }


        int GetGroupSubCount(int Id)
        {
            return GetContext().Group.Find(Id).Subscribers.Count;
        }

        /* API */
        [Test]
        public async Task apiSubscribeTest()
        {
            memMoq.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);
            LogIn(users[1]);
            // open, but user[1] is participant
            UserContext _context = GetContext();

            int c = await _context.GroupSubscriber.CountAsync();

            JsonResult res = await controller.apiSubscribe(groups[0].Id);
            Assert.AreEqual(c,subscribers.Count);
            // closed (no subscribers)
            c = GetGroupSubCount(groups[1].Id);
            res = await controller.apiSubscribe(groups[1].Id);
            Assert.AreEqual(c, GetGroupSubCount(groups[1].Id));
            // private (no subscribers)
            c = GetGroupSubCount(groups[1].Id);
            res = await controller.apiSubscribe(groups[2].Id);
            Assert.AreEqual(c, GetGroupSubCount(groups[2].Id));

            LogIn(users[2]);
            // open, but user[1] is subscriber
            c = GetGroupSubCount(groups[0].Id);
            res = await controller.apiSubscribe(groups[0].Id);
            Assert.AreEqual(c-1, GetGroupSubCount(groups[0].Id));
            res = await controller.apiSubscribe(groups[0].Id);
            Assert.AreEqual(c, GetGroupSubCount(groups[0].Id));
            // closed (no subscribers)
            c = GetGroupSubCount(groups[1].Id);
            res = await controller.apiSubscribe(groups[1].Id);
            Assert.AreEqual(c, GetGroupSubCount(groups[1].Id));
            // private (no subscribers)
            c = GetGroupSubCount(groups[2].Id);
            res = await controller.apiSubscribe(groups[2].Id);
            Assert.AreEqual(c, GetGroupSubCount(groups[2].Id));

            LogIn(users[3]);
            // open
            c = GetGroupSubCount(groups[0].Id);
            res = await controller.apiSubscribe(groups[0].Id);
            Assert.AreEqual(c + 1 + groups[0].Admins.Count, new List<UserModel>((IEnumerable<UserModel>)res.Value.GetType().GetProperty("Subscribers").GetValue(res.Value)).Count);
            Assert.True((bool)res.Value.GetType().GetProperty("IsSubscribed").GetValue(res.Value));
            Assert.AreEqual(c+1, GetGroupSubCount(groups[0].Id));
            // unsubscribe
            res = await controller.apiSubscribe(groups[0].Id);
            Assert.AreEqual(c + groups[0].Admins.Count, new List<UserModel>((IEnumerable<UserModel>)res.Value.GetType().GetProperty("Subscribers").GetValue(res.Value)).Count);
            Assert.False((bool)res.Value.GetType().GetProperty("IsSubscribed").GetValue(res.Value));
            Assert.AreEqual(c, GetGroupSubCount(groups[0].Id));
            // closed (no subscribers)
            c = GetGroupSubCount(groups[1].Id);
            res = await controller.apiSubscribe(groups[1].Id);
            Assert.IsInstanceOf<JsonError>(res.Value);
            Assert.AreEqual(c, GetGroupSubCount(groups[1].Id));
            // private (no subscribers)
            c = GetGroupSubCount(groups[2].Id);
            res = await controller.apiSubscribe(groups[2].Id);
            Assert.IsInstanceOf<JsonError>(res.Value);
            Assert.AreEqual(c, GetGroupSubCount(groups[2].Id));

            res = await controller.apiSubscribe(1000);
            Assert.IsInstanceOf<JsonError>(res.Value);

        }

        int GetGroupPartCount(int Id)
        {
            return GetContext().Group.Find(Id).Participants.Count;
        }


        [Test]
        public async Task apiParticipateTest()
        {
            memMoq.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);
            UserContext _context = GetContext();

            LogIn(users[1]);
            // open no participants, but user[1] is Admin
            int c;
            c = GetGroupPartCount(groups[0].Id);
            JsonResult res = await controller.apiParticipate(groups[0].Id);
            Assert.AreEqual(c, GetGroupPartCount(groups[0].Id));
            Assert.IsInstanceOf<JsonError>(res.Value);

            // closed, user[1] is already
            c = GetGroupPartCount(groups[1].Id);
            res = await controller.apiParticipate(groups[1].Id);
            Assert.AreEqual(c-1, GetGroupPartCount(groups[1].Id));
            res = await controller.apiParticipate(groups[1].Id);
            Assert.AreEqual(c, GetGroupPartCount(groups[1].Id));

            // private, user[1] is already
            c = GetGroupPartCount(groups[2].Id);
            res = await controller.apiParticipate(groups[2].Id);
            Assert.AreEqual(c-1, GetGroupPartCount(groups[2].Id));
            res = await controller.apiParticipate(groups[2].Id);
            Assert.IsInstanceOf<JsonError>(res.Value); // private group
            Assert.AreEqual(c-1, GetGroupPartCount(groups[2].Id));
            // restore implicit
            _context.Group.Find(groups[2].Id).Participants.Add(new GroupUser(users[1], _context.Group.Find(groups[2].Id)));
            _context.SaveChanges();

            LogIn(users[2]);
            // open
            c = GetGroupPartCount(groups[0].Id);
            res = await controller.apiParticipate(groups[0].Id);
            Assert.AreEqual(c + 1, GetGroupPartCount(groups[0].Id));
            Assert.AreEqual(c + 1 + groups[0].Admins.Count, new List<UserModel>((IEnumerable<UserModel>)res.Value.GetType().GetProperty("Participants").GetValue(res.Value)).Count);
            Assert.True((bool)res.Value.GetType().GetProperty("IsParticipant").GetValue(res.Value));
            res = await controller.apiParticipate(groups[0].Id);
            Assert.AreEqual(c, GetGroupPartCount(groups[0].Id));
            // closed, already
            c = GetGroupPartCount(groups[1].Id);
            res = await controller.apiParticipate(groups[1].Id);
            Assert.AreEqual(c-1, GetGroupPartCount(groups[1].Id));
            Assert.False((bool)res.Value.GetType().GetProperty("IsParticipant").GetValue(res.Value));
            res = await controller.apiParticipate(groups[1].Id);
            Assert.AreEqual(c, GetGroupPartCount(groups[1].Id));
            // private 
            c = GetGroupPartCount(groups[2].Id);
            res = await controller.apiParticipate(groups[2].Id);
            Assert.AreEqual(c, GetGroupPartCount(groups[2].Id));
            Assert.IsInstanceOf<JsonError>(res.Value);

            res = await controller.apiParticipate(1000);
            Assert.IsInstanceOf<JsonError>(res.Value);

        }

    }
}
