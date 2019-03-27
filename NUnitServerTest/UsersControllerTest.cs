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
    class UsersControllerTest: ControllerTest
    {
        UsersController usersController;

        [SetUp]
        public void SetUp()
        {
            usersController = new UsersController(GetContext(), umngr, this, memMoq.Object);
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
    }
}
