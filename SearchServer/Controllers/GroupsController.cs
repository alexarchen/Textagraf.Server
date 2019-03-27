using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SearchServer.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using System.IO;

namespace SearchServer.Controllers
{


    public class GroupsController : AvatarController
    {
        private readonly UserContext _context;
        private readonly SignInManager<User> _smngr;
        private readonly UserManager<User> _mngr;
        private readonly IMemoryCache _cache;



        public GroupsController(UserContext context, SignInManager<User> mngr,IMemoryCache memoryCache,IFileUploader fileUploader):base(mngr.UserManager,fileUploader,"group")
        {
            _context = context;
            _smngr = mngr;
            _mngr = _smngr.UserManager;
            _cache = memoryCache;
        }

        // GET: Groups
        public async Task<IActionResult> All()
        {
            if (_smngr.IsSignedIn(User))
            {
                ViewBag.CanCreate = true;
            }

            var groups = await _context.Group.Where(g=>g.Type!=Group.GroupType.Personal).OrderBy(g => g.Rating).Take(100).Select(g=>new GroupModel(g,false)).ToListAsync();

            return View("Index",groups);
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.CanCreate = false;
            if (_smngr.IsSignedIn(User))
            {
                User user = await _mngr.GetUserAsync(User);
                // select groups where user is admin or creator or participant
                ViewBag.CanCreate = true;
                user = await _context.User.Include(u => u.SubscribesToGroups).ThenInclude(s => s.Group).
                    Include(u => u.AdminOfGroups).ThenInclude(a => a.Group).
                    Include(u => u.Participate).ThenInclude(p => p.Group).Where(u=>u.Id.Equals(user.Id)).FirstOrDefaultAsync();

                return View("Index", user.GetAllGroupsList().Select(g=>new GroupModel(g)).ToList());
            }

            return await All();
        }

        /// <summary>
        /// Low level version of CheckUserRightsToRead
        /// </summary>
        /// <param name="GroupType"></param>
        /// <param name="IsGroupAdmin"></param>
        /// <param name="isGroupUser"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool CheckUserRightsToRead(Group.GroupType GroupType,bool IsGroupAdmin,bool isGroupUser, int? userId)
        {
            
            if ((GroupType == Group.GroupType.Open) || (GroupType == Group.GroupType.Blog))
                return true;

            if (userId == null) return false;

            if (GroupType == Group.GroupType.Personal)
                return (IsGroupAdmin);

            return (IsGroupAdmin || isGroupUser);
        }
        /// <summary>
        /// Checks if user has acccess right to read group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool CheckUserRightsToRead(Group group, int? userId)
        {
            if ((group.Type == Group.GroupType.Open) || (group.Type == Group.GroupType.Blog))
                return true;

            if (userId == null) return false;

            if ((group.Participants == null) || (group.Admins==null)) throw new Exception("Group participants and admins must not be null");

            if (group.Type == Group.GroupType.Personal)
                return (group.Admins.Any(p => p.UserId == userId.Value));

            return ((group.Participants.Any(p => p.UserId == userId.Value)) ||
                (group.Admins.Any(p => p.UserId == userId.Value)));
        }
        /// <summary>
        /// Check uif user can post and edit group, group.Admins,.Participants must be loaded
        /// </summary>
        /// <param name="group"></param>
        /// <param name="user"></param>
        /// <param name="CanEdit"></param>
        /// <returns></returns>
        public static bool CheckUserRightsToPost(Group group, int? userId, out bool CanEdit)
        {
            CanEdit = false;
            bool CanPost = false;
            if (userId == null) return false;
            if ((group.Admins.Where(ga => ((ga.GroupId == group.Id) && (ga.UserId == userId))).Count() > 0)
                   || ((group.Creator != null) && (group.Creator.Id.Equals(userId))))
            {
                CanPost = true;
                CanEdit = true;
            }
            else if ((group.Type != Group.GroupType.Personal) && (group.Participants.Any(gu => ((gu.GroupId == group.Id) && (gu.UserId == userId)))))
                CanPost = true;

            return CanPost;
        }

        // GET: Groups/Id/5[/Tags]
        //[HttpGet("Groups/Id/{Id}")]
        public async Task<IActionResult> Id(int? id,string tags)
        {
            if (id == null)
            {
                return NotFound();
            }
            var @group = await _context.Group.Include(g=>g.Comments).Include(g=>g.Documents).ThenInclude(d=>d.Comments).Include(g=>g.Admins).ThenInclude(ga=>ga.User).Include(g=>g.Subscribers).ThenInclude(ga => ga.User).Include(g=>g.Participants).ThenInclude(ga => ga.User).FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null)
            {
                return NotFound();
            }
            return await outGroup(@group);
        }

        public async Task<IActionResult> Name(string name)
        {
            if (name == null)
            {
                return NotFound();
            }
            var @group = await _context.Group.Include(g => g.Comments).Include(g => g.Documents).ThenInclude(d => d.Comments).Include(g => g.Admins).ThenInclude(ga => ga.User).Include(g => g.Subscribers).ThenInclude(ga => ga.User).Include(g => g.Participants).ThenInclude(ga => ga.User).FirstOrDefaultAsync(m => m.UniqueName == name);
            if (@group == null)
            {
                return NotFound();
            }

            return await outGroup(@group);
        }

        async Task<IActionResult> outGroup(Group group)
        {
            ViewBag.CanPost = false;
            ViewBag.CanEdit = false;
            ViewBag.IsAdmin = false;
            ViewBag.IsSubscribed = false;
            ViewBag.IsParticipant = false;
            ViewBag.CanRead = false;
            string tags = null;

            if (HttpContext.Request.QueryString.HasValue)
            {
                string val = Uri.UnescapeDataString(HttpContext.Request.QueryString.Value);
                if (val.StartsWith("?#"))
                {
                    tags = HttpContext.Request.QueryString.Value.Substring(2).ToLower();
                }
            }
            ViewBag.CanPostComment = false;

            @group.Comments.Reverse();
            @group.Documents.Reverse();


            if (_smngr.IsSignedIn(User))
            {
                User user = await _mngr.GetUserAsync(User);
                ViewBag.User = new UserModel(user);


                if (User.IsInRole("Admin"))
                {
                    ViewBag.CanPost = true;
                    ViewBag.CanEdit = true;
                }
                else
                {
                    bool ce = false;
                    ViewBag.CanPost = CheckUserRightsToPost(group, user.Id, out ce);
                    ViewBag.CanEdit = ce;
                }

                ViewBag.IsAdmin = group.Admins.Any(ga => ((ga.GroupId == group.Id) && (ga.UserId == user.Id)));
                ViewBag.IsParticipant = ViewBag.IsAdmin || (group.Participants.Where(gs => ((gs.GroupId == group.Id) && (gs.UserId == user.Id))).Count() > 0);
                ViewBag.IsSubscribed = ViewBag.IsAdmin || ViewBag.IsParticipant || (group.Subscribers.Where(gs => ((gs.GroupId == group.Id) && (gs.UserId == user.Id))).Count() > 0);

                if (User.IsInRole("Admin"))
                    ViewBag.CanRead = true;
                else
                    ViewBag.CanRead = CheckUserRightsToRead(group, user.Id);

                ViewBag.CanPostComment = ViewBag.CanRead;

            }
            else ViewBag.CanRead = CheckUserRightsToRead(group, null);


            GroupModel model = new GroupModel(@group);

            if (System.IO.File.Exists(Path.Combine(StorageController.Folder, $"Groups/{@group.Id}.html")))
                model.body = new MarkdownSharp.Markdown(new MarkdownSharp.MarkdownOptions() { AutoHyperlink = true }).Transform(System.IO.File.ReadAllText(Path.Combine(StorageController.Folder, $"Groups/{group.Id}.html")));

            model.Documents = model.Documents.Where(d => d.IsReady).ToList();

            if (tags != null)
            {
                // TODO: make DOCODO request for large group
               model.Documents = model.Documents.Where(d => d.Tags.ToLower().Contains(tags)).ToList();
            }

            if (!ViewBag.CanRead)
            {
                if (model.Type == Group.GroupType.Private)
                {
                    model.Subscribers.Clear();
                    model.Participants.Clear();
                    model.Admins.Clear();
                    model.Documents.Clear();
                }
                else
                    if (model.Type == Group.GroupType.Personal)
                {
                    return NotFound();
                }
            }

            return View("Id",model);

        }

        // GET: Groups/Create
        [Authorize]
        public IActionResult Create()
        {
            return View(new GroupModel());
        }

        // POST: Groups/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Tags,Type,Access")] GroupModel @group)
        {
            if (ModelState.IsValid)
            {
                User user = await _mngr.GetUserAsync(User);
                user = await _context.User.Include(u => u.PayedSubscribes).Include(u=>u.CreatedGroups).FirstAsync(u => u.Id.Equals(user.Id));

                int nPrivateGroups = user.CreatedGroups.Select(g => g.Type == Group.GroupType.Private).Count();
                int maxGroups = (User.IsInRole("Admin")?10000:user.PayedSubscribes.Where(ps => ps.IsValid()).Append(new PayedSubscribe()).Select(ps => ps.getOptions().NumberOfPrivateGroups).Max());
                if (nPrivateGroups >= maxGroups)
                {
                    ModelState.AddModelError("Type", "Allowed private groups limit reached. Purchase payed subscribe");
                }
                else
                {

                    Group gr = (Group)group;
                    gr.Creator = user;
                    gr.Admins.Add(new GroupAdmin(user, gr));
                    _context.Group.Add(gr);
                    await _context.SaveChangesAsync();
//                    _context.GetFullUserLinq().GetCached(_cache,v=>v.Id).Clear(user.Id);

                    return RedirectToAction(nameof(Id), new { id = @gr.Id });
                }
            }
            return View(group);
        }

        // GET: Groups/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            User user = await _mngr.GetUserAsync(User);
            var @group = await _context.Group.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }


            if (User.IsInRole("Admin") || (group.Admins.Contains(new GroupAdmin(user,group)))
                || group.Creator.Equals(user))
            {
                var model = new GroupModel(@group);
                
                if (System.IO.File.Exists(Path.Combine(StorageController.Folder, $"Groups/{id.Value}.html")))
                 model.body = System.IO.File.ReadAllText(Path.Combine(StorageController.Folder,$"Groups/{id.Value}.html"));
                return View(model);
            }

            return Forbid();


        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Tags,ImageUrl,Type,Access,UniqueName,body")] GroupModel gr, bool checkonly=false)
        {
            var @group = await _context.Group.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }

            if (checkonly)
            {
                if ((gr.UniqueName != null) && (gr.UniqueName.Length > 3))
                {
                    if (_context.Group.Any(g=>g.UniqueName.Equals(gr.UniqueName)))
                     ModelState.AddModelError("Input.Name", "User already exists");
                }

                return Json(ModelState);
            }


            if (ModelState.IsValid)
            {
                try
                {
                    gr.Update(@group);
                    _context.Update(@group);
                    if (gr.body != null)
                    {
                        gr.body = ClearBodyHtml(gr.body);
                        Directory.CreateDirectory(Path.Combine(StorageController.Folder, "Groups"));
                          System.IO.File.WriteAllText(Path.Combine(StorageController.Folder, $"Groups/{id}.html"), gr.body);
                    }
                    // TODO: update user cache of all subscribers, participants
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(@group.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Id),new { id=@group.Id});
            }
            return View(gr);
        }

        string ClearBodyHtml(string body)
        {
            body = body.Replace("<script", "<b>Error:</b><p");
            body = body.Replace("/script>", "/p>");
            body = body.Replace("<link", "<b>Error</b><p");
            body = body.Replace("<style", "<b>Error:</b><p");
            body = body.Replace("/style>", "/p>");
            return body;
        }


        // GET: Groups/Delete/5
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Group
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null)
            {
                return NotFound();
            }

            return View(@group);
        }

        // POST: Groups/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @group = await _context.Group.FindAsync(id);
            _context.Group.Remove(@group);
            await _context.SaveChangesAsync();

            // TODO: renew user cache

            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Group.Any(e => e.Id == id);
        }



        protected override async Task<bool> CheckAvatarId(string id)
        {
            bool b;
            if (User.IsInRole("Admin")) return true;
            if (!_smngr.IsSignedIn(User)) return false;
            Group group = await _context.Group.Include(g => g.Admins).FirstOrDefaultAsync(g=>g.Id.ToString().Equals(id));
            if (group==null) return false;
            CheckUserRightsToPost(group, int.Parse(_mngr.GetUserId(User)), out b);
            return b;
        }

        protected override async Task<string> UpdateAvatar(string id, string name)
        {
            Group group = await _context.Group.Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id.ToString().Equals(id));
            group.ImageUrl = name;
            _context.Update(group);
            await _context.SaveChangesAsync();
            return (new GroupModel(group).ImageUrl);
        }



        /* API */
        int? GetUserId()
        {
            string uid = _mngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;
            return userId;
        }


        [HttpGet("api/Groups/Id/{Id}",Name = "GroupById")]
        [Produces("application/json")]
        public async Task<JsonResult> apiId(int? id)
        {
            if (id == null)
            {
                return Json(JsonError.ERROR_NOT_FOUND);
            }

            var @group = await _context.Group.Include(g => g.Documents).ThenInclude(d => d.Comments).Include(g => g.Admins).ThenInclude(ga => ga.User).Include(g => g.Subscribers).ThenInclude(ga => ga.User).Include(g => g.Participants).ThenInclude(ga => ga.User).FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null)
            {
                return Json(JsonError.ERROR_NOT_FOUND);
            }
            ViewBag.CanPost = false;
            ViewBag.CanEdit = false;
            ViewBag.IsAdmin = false;
            ViewBag.IsSubscribed = false;
            ViewBag.IsParticipant = false;
            ViewBag.CanRead = true;


            if (_smngr.IsSignedIn(User))
            {
                //User user = await _mngr.GetUserAsync(User);
                string uid = _mngr.GetUserId(User);
                int? userId = uid != null ? (int?)int.Parse(uid) : null;

                if (User.IsInRole("Admin") || (group.Admins.Where(ga => ((ga.GroupId == group.Id) && (ga.UserId == userId))).Count() > 0)
                    || ((group.Creator != null) && (group.Creator.Id.Equals(userId))))
                {
                    ViewBag.CanPost = true;
                    ViewBag.CanEdit = true;
                }
                else if (group.Participants.Where(gu => ((gu.GroupId == group.Id) && (gu.UserId == userId))).Count() > 0)
                    ViewBag.CanPost = true;

                ViewBag.IsAdmin = (group.Admins.Where(ga => ((ga.GroupId == group.Id) && (ga.UserId == userId))).Count() > 0);
                ViewBag.IsParticipant = ViewBag.IsAdmin || (group.Participants.Where(gs => ((gs.GroupId == group.Id) && (gs.UserId == userId))).Count() > 0);
                ViewBag.IsSubscribed = ViewBag.IsAdmin || ViewBag.IsParticipant || (group.Subscribers.Where(gs => ((gs.GroupId == group.Id) && (gs.UserId == userId))).Count() > 0);

                if ((!User.IsInRole("Admin")) && (!CheckUserRightsToRead(group, userId)))
                {
                    ViewBag.CanRead = false;
                }
            }
            else ViewBag.CanRead = (User.IsInRole("Admin")) || CheckUserRightsToRead(group, null);

            GroupModel model = new GroupModel(@group);
            if (!ViewBag.CanRead)
            {
                if (model.Type == Group.GroupType.Private)
                {
                    model.Subscribers.Clear();
                    model.Participants.Clear();
                    model.Admins.Clear();
                    model.Documents.Clear();
                }
            }

            model.Access = "" + (ViewBag.CanRead ? "Read " : "") + (ViewBag.CanPost ? "Post " : "") + (ViewBag.CanEdit ? "Edit " : "");
             
            if (System.IO.File.Exists(Path.Combine(StorageController.Folder, $"Groups/{group.Id}.html")))
                model.body = System.IO.File.ReadAllText(Path.Combine(StorageController.Folder, $"Groups/{group.Id}.html"));

            return Json(model);
        }

        // subscribe/unsubscribe
        [HttpPut("api/Groups/Subscribe/{Id}", Name = "SubscribeToGroup")]
        [Authorize]
        [Produces("application/json")]
        public async Task<JsonResult> apiSubscribe(int id)
        {
            //int userId = (await _mngr.GetUserAsync(User)).Id;
            //User me = await _context.Users.Include(u => u.SubscribesToUsers).FirstOrDefaultAsync(u => u.Id == userId);
            int userId = GetUserId().Value;
            //           User me  = await _mngr.GetUserAsync(User);

            Group group = await _context.Group.Include(g => g.Subscribers).Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return Json(JsonError.ERROR_NOT_FOUND);

            if (group.Admins.Any(a=>(a.UserId == userId))) return Json(JsonError.ERROR_ADMIN_SUBSCRIBED);



            if ((group.Type == Group.GroupType.Private) || (group.Type == Group.GroupType.Closed))
            {
                return Json(JsonError.ERROR_CANT_SUBSCRIBE);
            }

            bool issub = false;
            GroupSubscriber subscribed = group.Subscribers.Find(gs => gs.UserId == userId);
            if (subscribed != null)
            {
               // _context.GroupSubscriber.Remove(subscribed);
                group.Subscribers.Remove(subscribed);
            }
            else
            {

                GroupSubscriber gs = new GroupSubscriber() { UserId = userId, GroupId = id };
                group.Subscribers.Add(gs);

                issub = true;
            }
            _context.Update(group);

            // TODO: renew
            //_context.Update(user); // update user cache
            //_context.User.RemoveFromCache(_cache, userId);
            //            await _context.SaveChangesAsync();
            await group.RefreshRating(_context);

            //_context.Entry(group).State = EntityState.Detached;

            //group = await _context.Group.Include(g => g.Subscribers).ThenInclude(gs=>gs.User).Include(g => g.Admins).ThenInclude(ga => ga.User).FirstOrDefaultAsync(g => g.Id == id);
            User user = await _context.User.Include(u => u.Participate).ThenInclude(gu => gu.User).Include(u => u.SubscribesToGroups).ThenInclude(gs => gs.Group).Include(u => u.SubscribesToUsers).ThenInclude(us => us.User).Include(u => u.AdminOfGroups).ThenInclude(ga => ga.Group).FirstOrDefaultAsync(u => u.Id.Equals(userId));


            _cache.RemoveUserDocsCache(userId);

            var gm = new GroupModel(group);
            return Json(new { IsSubscribed = issub, Subscribers = gm.Admins.Concat(gm.Subscribers), User = new UserModel(user)});

        }
        // Participate/unParticipate
        [HttpPut("api/Groups/Participate/{Id}", Name = "ParticipateGroup")]
        [Authorize]
        [Produces("application/json")]
        public async Task<JsonResult> apiParticipate(int id)
        {
            //int userId = (await _mngr.GetUserAsync(User)).Id;
            //User me = await _context.Users.Include(u => u.SubscribesToUsers).FirstOrDefaultAsync(u => u.Id == userId);
            //            User me = await _mngr.GetUserAsync(User);
            int userId = GetUserId().Value;

            Group group = await _context.Group.Include(g => g.Participants).Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return Json(JsonError.ERROR_NOT_FOUND);

            if (group.Admins.Any(a => (a.UserId == userId))) return Json(JsonError.ERROR_ADMIN_SUBSCRIBED);


            bool issub = false;
            GroupUser subscribed = group.Participants.Find(gs => gs.UserId == userId);
            if (subscribed != null)
            {
                group.Participants.Remove(subscribed);
            }
            else
            {
                if (group.Type == Group.GroupType.Private)
                {
                    // post messages to all admins of group
                    group.Admins.ForEach(ga => MessagesController.PostMessageAsync(_context, new Message() { Type = Message.MessageType.ParticipateGroup, groupId = id, toUserId = ga.UserId }));
                    return Json(JsonError.NEED_ASK_ADMIN);
                }

                //_context.GroupUser.Add(new GroupUser() { UserId = userId, GroupId = id });
                group.Participants.Add(new GroupUser() { UserId = userId, GroupId = id });
                issub = true;
            }

            _context.Update(group);
            await group.RefreshRating(_context);
            //            await _context.SaveChangesAsync();

            //            _context.Entry(group).State = EntityState.Detached;

            //_context.User.RemoveFromCache(_cache, userId);
            User user = await _context.User.Include(u => u.Participate).ThenInclude(gu => gu.User).Include(u => u.SubscribesToGroups).ThenInclude(gs => gs.Group).Include(u => u.SubscribesToUsers).ThenInclude(us=>us.User).Include(u => u.AdminOfGroups).ThenInclude(ga => ga.Group).FirstOrDefaultAsync(u=>u.Id.Equals(userId));
  //          group = await _context.Group.Include(g => g.Participants).ThenInclude(gs => gs.User).Include(g => g.Admins).ThenInclude(gs => gs.User).FirstOrDefaultAsync(g => g.Id == id);


            var gm = new GroupModel(group);

            _cache.RemoveUserDocsCache(userId);

            return Json(new { IsParticipant = issub, Participants = gm.Admins.Concat(gm.Participants), User = new UserModel(user) });

        }

        [HttpPut("api/Groups/Admin/{Id}/{UserId}", Name = "AddGroupAdmin")]
        [Authorize]
        [Produces("application/json")]
        public async Task<JsonResult> apiAddAdmin(int id,int userId)
        {
            int myuserId = GetUserId().Value;
            Group group = await _context.Group.Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id.Equals(id));
            if (group == null) return Json(JsonError.ERROR_NOT_FOUND);
            if ((!group.Creator.Equals(myuserId)) && (!group.Admins.Any(a=>a.UserId==myuserId))) return Json(JsonError.ERROR_ACCESS_DENIED);

            User user = await _context.User.FindAsync(userId);
            if (user == null) return Json(JsonError.ERROR_USER_NOT_FOUND);
            if (group.Admins.Any(p => p.UserId.Equals(userId))) return Json(JsonError.ERROR_ALREADY_EXISTS);
            group.Admins.Add(new GroupAdmin(user, group));
            _context.Update(group);
            await _context.SaveChangesAsync();
           // _context.User.RemoveFromCache(_cache, userId);

            return Json(new { status  = "Ok", group = new GroupModel(group) });
        }
        [HttpDelete("api/Groups/Admin/{Id}/{UserId}", Name = "DeleteGroupAdmin")]
        [Authorize]
        [Produces("application/json")]
        public async Task<JsonResult> apiDelAdmin(int id, int userId)
        {
            int myuserId = GetUserId().Value;
            Group group = await _context.Group.Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id.Equals(id));
            if (group == null) return Json(JsonError.ERROR_NOT_FOUND);
            if (!group.Creator.Equals(myuserId)) return Json(JsonError.ERROR_ACCESS_DENIED);

            GroupAdmin ga = group.Admins.First(g => g.UserId.Equals(userId));
            if (ga == null) return Json(JsonError.ERROR_USER_NOT_FOUND);
            group.Admins.Remove(ga);
            _context.Update(group);
            await _context.SaveChangesAsync();
            _cache.RemoveUserDocsCache(userId);
           //_context.User.RemoveFromCache(_cache, userId);
            return Json(new { status = "Ok", group = new GroupModel(group) });
        }
        /*
        [HttpPut("api/Groups/User/{Id}/{UserId}", Name = "AddGroupUser")]
        [Authorize]
        [Produces("application/json")]
        public async Task<JsonResult> apiAddUser(int id, int userId)
        {
            int myuserId = GetUserId().Value;
            Group group = await _context.Group.Include(g => g.Admins).Include(g=>g.Participants).FirstOrDefaultAsync(g => g.Id.Equals(id));
            if (group == null) return Json(JsonError.ERROR_NOT_FOUND);
            if ((!group.Creator.Equals(myuserId)) && (!group.Admins.Any(a => a.UserId == myuserId))) return Json(JsonError.ERROR_ACCESS_DENIED);

            User user = await _context.User.FindAsync(userId);
            if (user == null) return Json(JsonError.ERROR_USER_NOT_FOUND);
            if (group.Participants.Any(p => p.UserId.Equals(userId))) return Json(JsonError.ERROR_ALREADY_EXISTS);
            group.Participants.Add(new GroupUser(user, group));
            _context.Update(group);
            await _context.SaveChangesAsync();
            _cache.RemoveUserDocsCache(userId);
            return Json(new { status = "Ok", group = new GroupModel(group) });
        }
        */

        /// <summary>
        /// Delete user and ban it from group so only admin can make it participant again
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("api/Groups/User/{Id}/{UserId}", Name = "DeleteGroupUser")]
        [Authorize]
        [Produces("application/json")]
        public async Task<JsonResult> apiDelUser(int id, int userId)
        {
            int myuserId = GetUserId().Value;
            Group group = await _context.Group.Include(g => g.Admins).FirstOrDefaultAsync(g => g.Id.Equals(id));
            if (group == null) return Json(JsonError.ERROR_NOT_FOUND);
            if ((!group.Creator.Equals(myuserId)) && (!group.Admins.Any(a => a.UserId == myuserId))) return Json(JsonError.ERROR_ACCESS_DENIED);

            User user = await _context.User.FindAsync(userId);
            if (user == null) return Json(JsonError.ERROR_USER_NOT_FOUND);
            // TODO: Ban user from participating this group

            GroupUser ga = group.Participants.First(g => g.UserId.Equals(userId));
            if (ga == null) return Json(JsonError.ERROR_USER_NOT_FOUND);

            group.Participants.Remove(ga);
            _context.Update(group);
            await _context.SaveChangesAsync();
            _cache.RemoveUserDocsCache(userId);
            //_context.User.RemoveFromCache(_cache, userId);

            return Json(new { status = "Ok", group = new GroupModel(group) });
        }


    }
}
