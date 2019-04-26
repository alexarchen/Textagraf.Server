using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SearchServer.Controllers;
using SearchServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitServerTest
{
    class UsersControllerTest: ControllerTest, IAntiforgery
    {
        UsersController usersController;

        [SetUp]
        public void SetUp()
        {
            usersController = new UsersController(GetContext(), umngr, this, memMoq.Object,this);
        }

        [Test]
        public async Task IndexTest()
        {
            var _context = GetContext();


            IActionResult res = await usersController.Index(null);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<List<UserModel>>(((ViewResult)res).Model);
            res = await usersController.Index(users[1].Id);
            Assert.IsInstanceOf<ViewResult>(res);
            Assert.IsInstanceOf<UserModel>(((ViewResult)res).Model);
            UserModel um = (UserModel) (((ViewResult)res).Model);
            int c = _context.Document.Count(d => d.UserId == users[1].Id);
            Assert.AreEqual(c, um.Documents.Count);
            // now hide document
            /*
            Document doc = _context.Document.First(d => (d.UserId == users[1].Id) && (d.GroupId!=null));
            doc.DocStatus = Document.DocStatusEnum.Invisible;
            _context.Update(doc);
            _context.SaveChanges();
            // check absence of document on the user page
            res = await usersController.Index(users[1].Id);
            um = (UserModel)(((ViewResult)res).Model);
            Assert.AreEqual(c - 1, um.Documents.Count);
            doc.DocStatus = Document.DocStatusEnum.Normal;
            _context.Update(doc);
            _context.SaveChanges();
            */

        }

        class UserSignInResult : Microsoft.AspNetCore.Identity.SignInResult
        {
            public UserSignInResult(bool Success)
            {
                Succeeded = Succeeded;
                IsLockedOut = false;
                IsNotAllowed = Succeeded;
                RequiresTwoFactor = false;
            }
        }

        [Test]
        async void apiLoginTest()
        {
            var _context = GetContext();

            smngrMoq.Setup(c => c.PasswordSignInAsync(users[0].UserName, "TEST", true, false)).Returns(Task.FromResult((Microsoft.AspNetCore.Identity.SignInResult)new UserSignInResult(true)));
            smngrMoq.Setup(c => c.PasswordSignInAsync("xxxxxxx", "TEST", true, false)).Returns(Task.FromResult((Microsoft.AspNetCore.Identity.SignInResult)new UserSignInResult(false)));

            var res = await usersController.apiLogin(users[0].UserName, "test");
            Assert.IsInstanceOf<UserModel>(((JsonResult)res).Value);

            res = await usersController.apiLogin("xxxxxxxx","test");
            Assert.IsInstanceOf<JsonError>(((JsonResult)res).Value);
        }

        int GetAndStoreTokensCall = 0;
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            GetAndStoreTokensCall++;
            return new AntiforgeryTokenSet("TEST", "COOKIE", "REQ", "HEAD");
        }

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        {
            return new AntiforgeryTokenSet("TEST", "COOKIE", "REQ", "HEAD");
        }

        public async Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            return true;
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            throw new NotImplementedException();
        }
    }
}
