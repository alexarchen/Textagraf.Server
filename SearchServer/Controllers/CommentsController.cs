using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SearchServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace SearchServer.Controllers
{
    public class CommentsController : Controller
    {
        private readonly UserContext _context;
        private readonly SignInManager<User> signInManager;


        public CommentsController(UserContext context,SignInManager<User> mngr)
        {
            _context = context;
            signInManager = mngr;
        }




        public async Task<IActionResult> Index(string Id)
        {
            // show document's comments
            Document document = await _context.Document.Include(d=>d.Comments).ThenInclude(c=>c.User).FirstAsync(d=>d.Id.Equals(Id));
            if (document!=null)
            {
                return View (document.Comments.Select(cmt=>new CommentModel(cmt)).ToList());

            }

            return View(new List<Comment>());
        }


        [HttpGet("api/docs/{Id}/Comments", Name = "Comments")]
        [Produces("application/json")]
        public async Task<IActionResult> apiIndex(string Id)
        {
            // show document's comments
            // TODO: check read permissions
            return Json(await _context.Comment.Include(c => c.User).Where(с => с.DocumentId.Equals(Id)).Select(c => new CommentModel(c)).ToListAsync());
        }

        [HttpGet("api/groups/{Id}/Comments", Name = "GroupComments")]
        [Produces("application/json")]
        public async Task<IActionResult> apiIndex(int Id)
        {
            // show group's comments
            // TODO: check read permissions
            return Json (await _context.Comment.Include(c => c.User).Where(с => с.GroupId.Equals(Id)).Select(c=>new CommentModel(c)).ToListAsync());
        }


        // POST: Comments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("api/docs/{Id}/PostComment",Name ="PostComment")]
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> Create(string Id,[Bind("Text")] string Text)
        {
            //if (ModelState.IsValid)
            if (Text.Length>0)
            {
                User user;
                if (signInManager.IsSignedIn(User))
                {
                    user = (await signInManager.UserManager.GetUserAsync(User));
                }
                else
                {
                    // check api key
                    return Json(JsonError.ERROR_ACCESS_DENIED);
                }

                var document = await _context.Document.Include(d => d.Group).ThenInclude(g => g.Subscribers).Include(d => d.Group).ThenInclude(g => g.Participants).Include(d => d.Group).ThenInclude(g => g.Admins).Include(d => d.Comments).ThenInclude(c => c.User).Include(d => d.Likes)
                    .Include(d => d.User).FirstOrDefaultAsync(d => d.Id.Equals(Id));

                bool b;
                if (!((User.IsInRole("Admin")) || (document.GroupId == null) || (GroupsController.CheckUserRightsToPost(document.Group, user.Id, out b))))
                {
                    return Json(JsonError.ERROR_ACCESS_DENIED);
                }

                Comment comment = new Comment(Id,user.Id,Text);
                _context.Comment.Add(comment);
                await _context.SaveChangesAsync();

                return await apiIndex(Id);

            }
            return Json(new { error = "Data error" });

        }


        [HttpPost("api/groups/{Id}/postComment", Name = "PostGroupComment")]
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> Create(int Id, [Bind("Text")] string Text)
        {
            //if (ModelState.IsValid)
            if (Text.Length > 0)
            {
                User user;
                if (signInManager.IsSignedIn(User))
                {
                    user = (await signInManager.UserManager.GetUserAsync(User));
                }
                else
                {
                    // check api key
                    return Json(JsonError.ERROR_ACCESS_DENIED);
                }

                Group group = await _context.Group.Include(g => g.Participants).Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id.Equals(Id));

                bool b;
                if (!((User.IsInRole("Admin") || (GroupsController.CheckUserRightsToPost(group, user.Id, out b)))))
                {
                    return Json(JsonError.ERROR_ACCESS_DENIED);
                }

                Comment comment = new Comment(Id, user.Id, Text);
                _context.Comment.Add(comment);
                await _context.SaveChangesAsync();

                return await apiIndex(Id);

            }
            return Json(new { error = "Data error" });

        }


        [HttpDelete("api/groups/Comments/{Id}")]
        [HttpDelete("api/docs/Comments/{Id}",Name = "DeleteComment")]
        [HttpDelete("{Id}")]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Comment comment;
            if (!User.IsInRole("Admin")) {

                User user = await signInManager.UserManager.GetUserAsync(User);
                user = await _context.User.Include(u => u.AdminOfGroups).FirstOrDefaultAsync(u => u.Id.Equals(user.Id));

                comment = await _context.Comment.Include(c => c.Document).ThenInclude(d => d.Group).Include(c => c.Group).FirstOrDefaultAsync(c => c.Id.Equals(id));

                if (!comment.UserId.Equals(user.Id))
                {
                    if (comment.GroupId != null)
                    { // group comment
                        if (!user.AdminOfGroups.Any(ga => ga.GroupId.Equals(comment.GroupId)))
                            return Json(JsonError.ERROR_ACCESS_DENIED);
                    }
                    else
                    { // document comment
                      // check group access and authority
                      // if not document's author
                        if (!comment.Document.UserId.Equals(user.Id))
                        {
                            // and not document's group admin
                            if ((comment.Document.GroupId == null) || (!user.AdminOfGroups.Any(ga => ga.GroupId.Equals(comment.Document.GroupId))))
                            {
                                return Json(JsonError.ERROR_ACCESS_DENIED);

                            }
                        }

                    }
                }
            }
            else
                comment = await _context.Comment.FirstOrDefaultAsync(c => c.Id.Equals(id));

            _context.Comment.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { status = "Ok"});
        }


        private bool CommentExists(int id)
        {
            return _context.Comment.Any(e => e.Id == id);
        }
    }
}
