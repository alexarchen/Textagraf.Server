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


    [TestFixture]
    [TestOf(typeof(DocumentsController))]
    public class DocumentControllerTest: ControllerTest
    {
        DocumentsController controller;

        [SetUp]
        public void SetUp()
        {
            controller = new DocumentsController(
                GetContext(),
                smngrMoq.Object,
                this,
                this,
                memMoq.Object, null
                );

            SetupController(controller);

            SetHttpRequest("GET", "/");
        }

        [Test]
        public async Task EditTest()
        {
            // first no user logged
            LogOut();
            IActionResult res = await controller.Item("NOTEXIST", null);
            Assert.IsInstanceOf<NotFoundResult>(res);
            res = await controller.Edit("DOC", null);
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("TEST");
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("CLOSED");
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("PRIV");
            Assert.IsInstanceOf<ForbidResult>(res);

            // user logged as admin
            LogIn(Admin);

            res = await controller.Edit("DOC");
            Assert.IsInstanceOf<ViewResult>(res);
            res = await controller.Edit("TEST");
            Assert.IsInstanceOf<ViewResult>(res);
            res = await controller.Edit("CLOSED");
            Assert.IsInstanceOf<ViewResult>(res);
            res = await controller.Edit("PRIV");
            Assert.IsInstanceOf<ViewResult>(res);

            // user logged as 2
            LogIn(users[1]);

            res = await controller.Edit("DOC");
            Assert.IsInstanceOf<ViewResult>(res);
            res = await controller.Edit("TEST");
            Assert.IsInstanceOf<ViewResult>(res);
            res = await controller.Edit("CLOSED");
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("PRIV");
            Assert.IsInstanceOf<ViewResult>(res);

            // user logged as 2
            LogIn(users[2]);

            res = await controller.Edit("DOC");
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("TEST");
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("CLOSED");
            Assert.IsInstanceOf<ViewResult>(res);
            res = await controller.Edit("PRIV");
            Assert.IsInstanceOf<ForbidResult>(res);

            res = await controller.Edit("DOC",null);
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("TEST", null);
            Assert.IsInstanceOf<ForbidResult>(res);
            res = await controller.Edit("CLOSED", null);
            Assert.IsInstanceOf<ViewResult>(res, null);
            res = await controller.Edit("PRIV");
            Assert.IsInstanceOf<ForbidResult>(res, null);

        }

        [Test]
        public async Task ItemTest()
        {
            ActionContext actionContext = new ActionContext()
            {
                HttpContext = controller.HttpContext
            };
            
             var modelStateMoq = new Mock<ModelStateDictionary>();

            // first no user logged
            LogOut();

            IActionResult res = await controller.Item("NOTEXIST", null);
            Assert.IsInstanceOf<NotFoundResult>(res);
            res = await controller.Item("TEST", null);
            Assert.IsInstanceOf<ViewResult>(res);
            ViewResult v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            DocModel doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "TEST");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["IsEPub"]);
            Assert.IsFalse((bool)v.ViewData["CanEdit"]);
            Assert.IsFalse((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            res = await controller.Item("CLOSED", null);
            Assert.IsInstanceOf<ForbidResult>(res);

            res = await controller.Item("PRIV", null);
            Assert.IsInstanceOf<ForbidResult>(res);

            // user logged as admin
            LogIn(Admin);

            res = await controller.Item("NOTEXIST", null);
            Assert.IsInstanceOf<NotFoundResult>(res);
            res = await controller.Item("TEST", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "TEST");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["IsEPub"]);
            Assert.IsTrue((bool)v.ViewData["CanEdit"]);
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            res = await controller.Item("CLOSED", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "CLOSED");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["IsEPub"]);
            Assert.IsTrue((bool)v.ViewData["CanEdit"]);
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            res = await controller.Item("PRIV", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "PRIV");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["IsEPub"]);
            Assert.IsTrue((bool)v.ViewData["CanEdit"]);
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            Assert.AreEqual(((UserModel)v.ViewData["User"]).Id, Admin.Id);

            // user logged as user Id=2
            LogIn(users[1]);

            res = await controller.Item("NOTEXIST", null);
            Assert.IsInstanceOf<NotFoundResult>(res);

            res = await controller.Item("TEST", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "TEST");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["IsEPub"]);
            Assert.IsTrue((bool)v.ViewData["CanEdit"]); // user 2 is creater
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            res = await controller.Item("CLOSED", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "CLOSED");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["CanEdit"]);
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            res = await controller.Item("PRIV", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "PRIV");
            Assert.NotNull(v.ViewData);
            Assert.IsTrue((bool)v.ViewData["CanEdit"]);
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            Assert.AreEqual(((UserModel)v.ViewData["User"]).Id, users[1].Id);

            // user logged as user Id=3
            LogIn(users[2]);

            res = await controller.Item("NOTEXIST", null);
            Assert.IsInstanceOf<NotFoundResult>(res);
            res = await controller.Item("TEST", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "TEST");
            Assert.NotNull(v.ViewData);
            Assert.IsFalse((bool)v.ViewData["IsEPub"]);
            Assert.IsFalse((bool)v.ViewData["CanEdit"]);
            Assert.IsFalse((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            res = await controller.Item("CLOSED", null);
            Assert.IsInstanceOf<ViewResult>(res);
            v = (ViewResult)res;
            Assert.NotNull(v.Model);
            Assert.IsInstanceOf<DocModel>(v.Model);
            doc = (DocModel)v.Model;
            Assert.AreEqual(doc.Id, "CLOSED");
            Assert.NotNull(v.ViewData);
            Assert.IsTrue((bool)v.ViewData["CanEdit"]);
            Assert.IsTrue((bool)v.ViewData["CanPostComment"]);
            Assert.IsFalse(v.ViewData.Keys.Contains("Error"));
            Assert.AreEqual(v.ViewData["body"], DOCBODY);

            Assert.AreEqual(((UserModel)v.ViewData["User"]).Id, users[2].Id);

            res = await controller.Item("PRIV", null);
            Assert.IsInstanceOf<ForbidResult>(res);

        }

        [Test]
        public async Task ItemWithPageTest()
        {
            ActionContext actionContext = new ActionContext()
            {
                HttpContext = controller.HttpContext
            };

            var modelStateMoq = new Mock<ModelStateDictionary>();

            DocumentsController.DocFolder = "__DocContTest/";
            Directory.CreateDirectory(DocumentsController.DocFolder + "1/PRIV/");

            using (Stream s = File.Create(DocumentsController.DocFolder + "1/PRIV/.fore.png"))
            {
                using (StreamWriter sw = new StreamWriter(s))
                {
                    sw.Write("PRIVFORETEST");
                }
            }

            // first no user logged
            LogOut();

            IActionResult res = await controller.Item("NOTEXIST", "fore");
            Assert.IsInstanceOf<NotFoundResult>(res);
            res = await controller.Item("TEST", "fore");
            Assert.IsInstanceOf<NoContentResult>(res);

            res = await controller.Item("CLOSED", "fore");
            Assert.IsInstanceOf<ForbidResult>(res);

            res = await controller.Item("PRIV", ".fore.png");
            Assert.IsInstanceOf<ForbidResult>(res);

            res = await controller.Item("TEST", ".fore.png");
            Assert.IsInstanceOf<FileContentResult>(res);
            Assert.True(((FileContentResult)res).ContentType.StartsWith("image/"));
            res = await controller.Item("TEST", ".back.jpg");
            Assert.IsInstanceOf<FileContentResult>(res);
            Assert.True(((FileContentResult)res).ContentType.StartsWith("image/"));

            // login user
            LogIn(users[1]);

            res = await controller.Item("PRIV", ".fore.png");
            Assert.IsInstanceOf<FileStreamResult>(res);

            using (StreamReader sr = new StreamReader(((FileStreamResult)res).FileStream))
            {
                Assert.AreEqual("PRIVFORETEST", sr.ReadToEnd());
            }

            ((FileStreamResult)res).FileStream.Dispose();
            Assert.True(((FileStreamResult)res).ContentType.StartsWith("image/"));

            res = await controller.Item("TEST", ".back.jpg");
            Assert.IsInstanceOf<FileContentResult>(res);
            Assert.True(((FileContentResult)res).ContentType.StartsWith("image/"));

            Directory.Delete(DocumentsController.DocFolder, true);

        }


        [Test]
        public async Task CreateTest()
        {
            DocumentsController.DocFolder = "__DocContTest/";
            Directory.CreateDirectory(DocumentsController.DocFolder + "1/DEL/");
            UserContext _context = GetContext();

            // create GET
            LogIn(users[1]);
            Assert.IsInstanceOf<ViewResult>(await controller.Create((int?)null, ""));
            Assert.IsInstanceOf<ViewResult>(await controller.Create(1, ""));
            Assert.IsInstanceOf<ViewResult>(await controller.Create(2, ""));
            Assert.IsInstanceOf<ViewResult>(await controller.Create(3, ""));
            LogIn(users[2]);
            Assert.IsInstanceOf<ViewResult>(await controller.Create((int?)null, ""));
            Assert.IsInstanceOf<ForbidResult>(await controller.Create(1, ""));
            Assert.IsInstanceOf<ViewResult>(await controller.Create(2, ""));
            Assert.IsInstanceOf<ForbidResult>(await controller.Create(3, ""));
            
            // create POST
            LogIn(users[1]);
            DocModel newDoc = new DocModel() { TempFileName = "ABC.pdf", Title = "BAT", GroupId = null };
            controller.ModelState.AddModelError("Name", "Error");
            Assert.IsInstanceOf<ViewResult>(await controller.Create(newDoc, ""));
            MakeVerified(controller.ModelState);

            newDoc = new DocModel() { TempFileName = "ABC.xdf", Title = "BAT", GroupId = null };
            Assert.IsInstanceOf<ViewResult>(await controller.Create(newDoc, ""));
            Assert.False(controller.ModelState.IsValid);
            MakeVerified(controller.ModelState);

            newDoc = new DocModel() { TempFileName = "ABC.pdf", Title = "BAT", GroupId = null };
            this.Folder = "DOCCRTEST_TEMP/";
            DocumentsController.DocFolder = "DOCCRTEST_DOC/";
            Directory.Delete(DocumentsController.DocFolder, true);
            Directory.CreateDirectory(this.Folder);
            Directory.CreateDirectory(DocumentsController.DocFolder);
            File.WriteAllText(this.Folder + "ABC.pdf", "ABC");

            Assert.IsInstanceOf<RedirectToActionResult>(await controller.Create(newDoc, ""));
            Document doc = await _context.Document.FirstOrDefaultAsync(d => d.Title.Equals("BAT"));
            Assert.NotNull(doc);
            Assert.AreEqual(doc.UserId, users[1].Id);
            Assert.IsNull(doc.GroupId);
            _context.Document.Remove(doc);
            _context.SaveChanges();
            Directory.Delete(DocumentsController.DocFolder,true);
            Directory.CreateDirectory(DocumentsController.DocFolder);

            File.WriteAllText(this.Folder + "ABC.pdf", "ABC");
            newDoc = new DocModel() { TempFileName = "ABC.pdf", Title = "BATG", GroupId = 1 };
            Assert.IsInstanceOf<RedirectToActionResult>(await controller.Create(newDoc, ""));
            doc = await _context.Document.FirstOrDefaultAsync(d => d.Title.Equals("BATG"));
            Assert.NotNull(doc);
            Assert.AreEqual(users[1].Id, doc.UserId);
            Assert.AreEqual(1,doc.GroupId);
            _context.Document.Remove(doc);
            _context.SaveChanges();
            // not allowed to post to group
            LogIn(users[2]);
            File.WriteAllText(this.Folder + "ABC.pdf", "ABC");
            Assert.IsInstanceOf<ForbidResult>(await controller.Create(newDoc, ""));

            Directory.Delete(this.Folder, true);
            Directory.Delete(DocumentsController.DocFolder, true);
        }


        /// <summary>
        /// DocumentController.apiDelete and BookmarkController.apiDelete test
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteTest()
        {
            DocumentsController.DocFolder = "__DocContTest/";
            Directory.CreateDirectory(DocumentsController.DocFolder + "1/DEL/");

            UserContext _context = GetContext();
            _context.Document.Add(new Document() { Id = "DEL", Title = "DEL", File = "1/DEL/.pdf", UserId = users[2].Id, GroupId = 1});
            await _context.SaveChangesAsync();

            LogOut();
            JsonResult res = await controller.apiDelete("DELDFHdjghfdgdfgjk");
            Assert.AreEqual(JsonError.ERROR_NOT_FOUND, res.Value);
            res = await controller.apiDelete("DEL");
            Assert.AreEqual(JsonError.ERROR_ACCESS_DENIED,res.Value);
            LogIn(users[3]);
            res = await controller.apiDelete("DEL");
            Assert.AreEqual(JsonError.ERROR_ACCESS_DENIED, res.Value);
            LogIn(users[2]);
            res = await controller.apiDelete("DEL");
            Assert.AreEqual(JsonError.OK, res.Value);
            // Check folder
            Assert.False(Directory.Exists(DocumentsController.DocFolder + "1/DEL/"));
            // after that not found
            Assert.Null(_context.Document.Find("DEL"));
            // Now add bookmark to document and try to delete
            Directory.CreateDirectory(DocumentsController.DocFolder + "1/DEL/");
            _context.Document.Add(new Document() { Id = "DEL", Title = "DEL", File = "1/DEL/.pdf", UserId = users[2].Id, GroupId = 1 });
            Bookmark bm = new Bookmark() { DocumentId = "DEL", Name = "TEST", UserId = users[3].Id };
            _context.Bookmark.Add(bm);
            await _context.SaveChangesAsync();
            res = await controller.apiDelete("DEL");
            Assert.AreEqual(JsonError.OK, res.Value);
            Assert.True(Directory.Exists(DocumentsController.DocFolder + "1/DEL/"));
            Assert.NotNull(_context.Document.Find("DEL"));
            // now delete bookmark via BookmarksController
            LogIn(users[3]);
            BookmarksController bmc = new BookmarksController(_context, smngrMoq.Object);
            SetupController(bmc);
            await bmc.apiDelete(bm.Id);
            Assert.Null(_context.Bookmark.Find(bm.Id));
            Assert.False(Directory.Exists(DocumentsController.DocFolder + "1/DEL/"));
            Assert.Null(_context.Document.Find("DEL"));
            // Check delete from group 
            Directory.CreateDirectory(DocumentsController.DocFolder + "1/DEL/");
            _context.Document.Add(new Document() { Id = "DEL", Title = "DEL", File = "1/DEL/.pdf", UserId = users[2].Id, GroupId = 1 });
            await _context.SaveChangesAsync();
            // user[1] is groups admin, he can only "hide" document from group
            LogIn(users[1]);
            res = await controller.apiDelete("DEL");
            Assert.AreEqual(JsonError.OK, res.Value);
            var doc = _context.Document.Find("DEL");
            Assert.NotNull(doc);
            Assert.AreEqual(Document.DocStatusEnum.Invisible, doc.DocStatus);
            // check messages
            LogIn(users[2]);
            res = await (new MessagesController(_context, umngr)).apiIndex();
            Assert.AreEqual(Message.MessageType.DocRemoved, ((List<MessageModel>)res.Value)[0].Type);

            Directory.Delete(DocumentsController.DocFolder, true);
        }


    }

}
