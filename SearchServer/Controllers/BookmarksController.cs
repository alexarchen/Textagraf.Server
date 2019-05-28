using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SearchServer.Models;

namespace SearchServer.Controllers
{
    public class BookmarksController : CommonController
    {

        public BookmarksController(UserContext context, SignInManager<User> smngr):base(context,smngr)
        {
        }

        [Authorize]
        [HttpPost("api/Bookmarks/{id}/{*pg}",Name = "AddBookmark")]
        public async Task<JsonResult> apiAdd(string id,string pg,string label)
        {
            int userid = GetUserId().Value;
            Bookmark bm = new Bookmark() { DateTime = DateTime.UtcNow, DocumentId = id, Page = pg, Name = label, UserId = userid };
            _context.Bookmark.Add(bm);
            await _context.SaveChangesAsync();
            return Json(new BookmarkModel(bm));
        }


        [Authorize]
        [HttpDelete]
        [Route("api/Bookmarks/{id}", Name = "DelBookmark")]
        public async Task<JsonResult> apiDelete(long id)
        {
            int userid = GetUserId().Value;
            Bookmark bmark = _context.Bookmark.Where(bm => (bm.Id.Equals(id) && bm.UserId == userid)).First();
            _context.Bookmark.Remove(bmark);
            await _context.SaveChangesAsync();
            // now check if deleted document still have bookmarks
            Document doc = _context.Document.Find(bmark.DocumentId);
            if (doc.DocStatus == Document.DocStatusEnum.Deleted)
            {
                DocumentsController dc = new DocumentsController(_context, _smngr, null, null, null, null)
                {
                    ControllerContext = ControllerContext,
                  //  User = User
                };
                await dc.TryDeletePermanent(doc);
            }
            return Json(new { status = "Ok" });
        }


    }
}