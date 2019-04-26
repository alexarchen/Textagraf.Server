
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SearchServer.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SearchServer.Controllers
{
    public partial struct JsonError
    {
        static public JsonError ERROR_SELF = new JsonError("Can't subscribe to self", 10);

    }



    public abstract class AvatarController: Controller
    {
        protected UserManager<User> _mngr;
        protected IFileUploader _fileUploader;
        protected string AvatarName { get; }

        public AvatarController(UserManager<User> manager, IFileUploader fileUploader, string name)
        {
            _mngr = manager;
            _fileUploader = fileUploader;
            AvatarName = name;
        }

        /// <summary>
        /// Upload Avatar to server into temp file
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public virtual async Task<IActionResult> UploadAvatar(IFormFile avatar, string id)
        {
            if (!await CheckAvatarId(id)) return Json(JsonError.ERROR_ACCESS_DENIED);

            if ((avatar.FileName.ToLower().EndsWith(".png")) || (avatar.FileName.ToLower().EndsWith(".jpg")))
            {
//                int userId = int.Parse(_mngr.GetUserId(User));
                
                string fname = $"{AvatarName}_{id}_avatar" + avatar.FileName.Substring(avatar.FileName.LastIndexOf('.'));
                await _fileUploader.Upload(fname, avatar);
                return Json(new { Status = "Ok", Url = Url.Action("TempAvatar", new { name = fname }) });
            }
            return Json(JsonError.ERROR_FORMAT_NOT_SUP);
        }

        protected abstract Task<string> UpdateAvatar(string id, string name);
        protected abstract Task<bool> CheckAvatarId(string id);


        [HttpPost]
        [Authorize]
        public virtual async Task<IActionResult> SaveAvatar(string id, string filename, float X, float Y, float Size)
        {
            //            var user = await _mngr.GetUserAsync(User);
            if (!await CheckAvatarId(id)) return Json(JsonError.ERROR_ACCESS_DENIED);

            string fname = $"{AvatarName}_{id}_avatar" + filename.Substring(filename.LastIndexOf('.'));
            if (System.IO.File.Exists(System.IO.Path.Combine(_fileUploader.Folder, fname)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(StorageController.Folder, "Avatars/"));
                _fileUploader.ConvertToAvatar(System.IO.Path.Combine(_fileUploader.Folder, fname), (int)X, (int)Y, (int)Size, System.IO.Path.Combine(StorageController.Folder, $"Avatars/{AvatarName}_{id}.jpg"));

                return Json(new { Status = "Ok", Url = await UpdateAvatar(id, $"Storage/Avatars/{AvatarName}_{id}.jpg") });
            }

            return Json(new { Error = "Not uploaded" });
        }

//        [HttpGet("api/users/TempAvatar/{name}", Name = "TempAvatar")]
        public IActionResult TempAvatar(string name)
        {
            return new PhysicalFileResult(new System.IO.FileInfo(_fileUploader.Folder + "/" + name).FullName, StorageController.GetMimeType(name));
        }

    }


    public class UsersController : AvatarController
    {
        private readonly UserContext _context;
        private readonly IMemoryCache _cache;
        protected SignInManager<User> _smngr;
        private IAntiforgery _antiforgery;
        public UsersController(UserContext context, SignInManager<User> mngr,IFileUploader fileUploader,IMemoryCache memoryCache, IAntiforgery antiforgery) :base(mngr.UserManager,fileUploader,"user")
        {
            
            _context = context;
            _smngr = mngr;
            _fileUploader = fileUploader;
            _cache = memoryCache;
        }



        async Task<User> getUserById(int id)
        {
            User user;

            if ((_mngr.GetUserId(User).Equals(id.ToString())) || (User.IsInRole("Admin")))
                user = await _context.GetFullUserLinq().GetCached(_cache,(m) => m.Id).FirstOrDefaultAsync(id);
            else
             user = await _context.User.Include(u => u.Documents).ThenInclude(d => d.Likes).Include(u => u.Subscribers).ThenInclude(us => us.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            return user;
        }

        async Task<User> getUserByName(string name)
        {
            User user;

            if ((_mngr.GetUserName(User)?.Equals(name)??false) || (User.IsInRole("Admin")))
                user = await _context.GetFullUserLinq().GetCached(_cache, (m) => m.Id).FirstOrDefaultAsync(m=>m.UserName.Equals(name));
            else
                user = await _context.User.Include(u => u.Documents).ThenInclude(d => d.Likes).Include(u => u.Subscribers).ThenInclude(us => us.User)
                   .FirstOrDefaultAsync(m => m.UserName.Equals(name));
            return user;
        }

        // GET: Users/[Id]
        [HttpGet("Users/Id/{Id}",Name ="UserPage")]
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                // all users
                return View(await _context.User.Include(u => u.Subscribers).ThenInclude(s => s.User)
                    .OrderBy(u => u.Rating).Select(u=>new UserModel(u, false)).ToListAsync());
            }

            User user = await getUserById(id.Value);
            if (user == null)
            {
                return NotFound();
            }
            return outUser(user);
        }


        [Authorize]
        public async Task<IActionResult> Self()
        {
            var user = await getUserById(GetUserId().Value);
            if (user == null)
            {
                return NotFound();
            }
            return outUser(user);


        }
        //[HttpGet("Users/@{Name}", Name = "UserPage")]
        public async Task<IActionResult> Name(string name)
        {
            var user = await getUserByName(name);

            if (user == null)
            {
                return NotFound();
            }
            return outUser(user);
        }

        int? GetUserId()
        {
            string uid = _mngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;
            return userId;
        }

        IActionResult outUser(User user)
        {
            ViewBag.IsMy = false;
            ViewBag.IsSubscribed = false;
            //User me = await _mngr.GetUserAsync(User);
            int? me = GetUserId();

            if (me != null)
            {
                ViewBag.IsMy = (user.Id == me.Value);

                ViewBag.IsSubscribed = user.Subscribers.Where(us => ((us.ToUserId == user.Id) && (us.UserId == me.Value))).Count() > 0;
                if (ViewBag.IsMy)
                {
                    ViewBag.EmailConfirmed = user.EmailConfirmed;
                    ViewBag.Email = user.Email;
                }
            }
            else 

//            await user.RefreshRating(_context);
            user.Documents.Reverse();
            return View("Page", new UserModel(user));

        }


        async Task<User>  getUserForEdit(int? id)
        {
            User user;
            if ((User.IsInRole("Admin")) && (id!=null))
            {
                user = await _context.User.FindAsync(id);
            }
            else
                user = await _mngr.GetUserAsync(User);

            return user;
        }
        // GET: Users/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            User user = await getUserForEdit(id);

            if (user == null)
            {
                return NotFound();
            }

            UserModel model = new UserModel(user);
            if (!User.IsInRole("Admin"))
                model.Banned = null;
            ViewBag.EmailConfirmed = user.EmailConfirmed;
            return View(model);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("Name,Info,ImageUrl,Banned")] UserModel user)
        {
            User _user = await getUserForEdit(id);

            if ((user.Banned != null) && (!User.IsInRole("Admin")))
                user.Banned = null;

            if (ModelState.IsValid)
            {
                try
                {
              
                    user.Update(_user);
//                    _context.User.RemoveFromCache(_cache, id);
                    _context.Update(_user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(_user.Id))
                    {
                        // return NotFound();
                        ModelState.AddModelError("id", "User not found!");
                    }
                    else
                    {
                       // throw;
                    }
                }
                //return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.User.FindAsync(id);
            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.Id == id);
        }

        [Authorize]
        public async Task<IActionResult> Likes()
        {
            int? me = GetUserId();
            if (me != null)
            {
                // load cached info
                User user = await _context.User.Include(u => u.Likes).ThenInclude(l => l.Document).ThenInclude(d => d.Group).Include(u=>u.Likes).ThenInclude(u=>u.Document).ThenInclude(d=>d.User).FirstOrDefaultAsync((m) => m.Id.Equals(me.Value));//GetFullUserLinq().GetCachedAsync(_cache, (m) => m.Id, me.Value);
                return View(user.Likes.Select(l => new DocModel(l.Document)).ToList());
            }
            return Forbid();
        }

        [Authorize]
        public async Task<IActionResult> Subscribes()
        {
            int? me = GetUserId();
            if (me != null)
            {
                // load cached info
                User user = await _context.User.Include(u => u.SubscribesToUsers).FirstOrDefaultAsync((m) => m.Id.Equals(me.Value));//GetFullUserLinq().GetCachedAsync(_cache, (m) => m.Id, me.Value);
                return View(user.SubscribesToUsers.Select(us => new UserModel(us.ToUser)).ToList());
            }
            return Forbid();
        }

        [Authorize]
        public async Task<IActionResult> Groups()
        {
            int? me = GetUserId();
            if (me != null)
            {
                // load cached info
                User user = await _context.User.Include(u => u.SubscribesToGroups).ThenInclude(gs=>gs.Group).Include(u=>u.AdminOfGroups).ThenInclude(ga=>ga.Group).FirstOrDefaultAsync((m) => m.Id.Equals(me.Value));//GetFullUserLinq().GetCachedAsync(_cache, (m) => m.Id, me.Value);
                return View(user.SubscribesToGroups.Select(gs => gs.Group).Concat(user.AdminOfGroups.Select(ga=>ga.Group)).Distinct().Select(g=>new GroupModel(g)).ToList());
            }
            return Forbid();
        }

        [Authorize]
        public async Task<IActionResult> Bookmarks()
        {
            int? me = GetUserId();
            if (me != null)
            {
                // load cached info
                User user = await _context.User.Include(u => u.Bookmarks).ThenInclude(b=>b.Document).FirstOrDefaultAsync((m) => m.Id.Equals(me.Value));//GetFullUserLinq().GetCachedAsync(_cache, (m) => m.Id, me.Value);
                return View(user.Bookmarks.Select(b=>new BookmarkModel(b)).ToList());
            }
            return Forbid();
        }

        [Authorize]
        public async Task<IActionResult> Views()
        {
            int? me = GetUserId();
            if (me != null)
            {
                // load cached info
                var list = await _context.DocumentRead.Include(dr => dr.Document).ThenInclude(d => d.Group).Include(dr => dr.Document).ThenInclude(d => d.User).Where(dr => dr.UserId.Equals(me.Value)).OrderByDescending(dr=>dr.Id).Take(10000).ToListAsync();
                return View(list.Select(dr => dr.Document).Distinct().Take(100).Select(d => new DocModel(d, false)));
            }
            return Forbid();
        }

        [Authorize]
        public async Task<IActionResult> Comments()
        {
            int? me = GetUserId();
            if (me != null)
            {
                // TODO: show user's documents comments,
                // user's groups comments, user's groups documents comments
                // and user's commented documents comments

                var comments =_context.Comment.Include(c => c.Document).ThenInclude(d => d.Group).Include(c => c.User).OrderByDescending(c=>c.Id).Take(1000).GroupBy(c=>c.DocumentId).TakeLast(20).Select(cg=>new CommentModel(cg.First())).ToList();
                return View(comments);

            }
            return Forbid();
        }

        protected override async Task<bool> CheckAvatarId(string id)
        {
            return (User.IsInRole("Admin") || _mngr.GetUserId(User).Equals(id));
        }

        protected override async Task<string> UpdateAvatar(string id, string name)
        {
            var user = await _mngr.GetUserAsync(User);
            user.ImageUrl = name;
            _context.Update(user);
            await _context.SaveChangesAsync();
            return (new UserModel(user).ImageUrl);
        }

        /* API */


        public IActionResult RequireConfirmEmail()
        {
            return View();
        }


        // subscribe/unsubscribe
        [HttpGet("api/Users/Subscribe/{Id}", Name = "SubscribeToUser")]
        [Authorize]
        public async Task<JsonResult> apiSubscribe(int id)
        {
            //int userId = (await _mngr.GetUserAsync(User)).Id;
            //User me = await _context.Users.Include(u => u.SubscribesToUsers).FirstOrDefaultAsync(u => u.Id == userId);
            User me = await _mngr.GetUserAsync(User);
            if (me.Id == id) return Json(JsonError.ERROR_SELF);
            User user = await _context.Users.Include(u=>u.Subscribers).FirstOrDefaultAsync(u=>u.Id==id);
            if (user == null) return Json(JsonError.ERROR_NOT_FOUND);

            bool issub = false;
            UserSubscriber subscribed = user.Subscribers.Find(u => u.UserId == me.Id);
            if (subscribed != null)
            {
//                user.Subscribers.Remove(subscribed);
                _context.UserSubscriber.Remove(subscribed);
               // _context.Update(_context.UserSubscriber);
            //    await _context.SaveChangesAsync();
            //    return Json(new { IsSubscribed = false,  Subscribers = user.Subscribers.Count});
            }
            else
            {

                _context.UserSubscriber.Add(new UserSubscriber() { UserId = me.Id, ToUserId = id});
                issub = true;
            }

            //_context.Update(user);
            await _context.SaveChangesAsync();
            _context.Entry(me).State = EntityState.Detached;
            _context.Entry(user).State = EntityState.Detached;

            me = await _context.User.Include(u => u.Participate).ThenInclude(gu => gu.User).Include(u => u.SubscribesToGroups).ThenInclude(gs => gs.Group).Include(u => u.SubscribesToUsers).ThenInclude(us => us.User).Include(u => u.AdminOfGroups).ThenInclude(ga => ga.Group).FirstOrDefaultAsync(u => u.Id.Equals(me.Id));
            user = await _context.User.Include(u => u.Subscribers).ThenInclude(us => us.User).FirstOrDefaultAsync(u => u.Id.Equals(id));

            await user.RefreshRating(_context);
            // TODO: renew cache instead of deleteing 
//            _context.User.RemoveFromCache(_cache, me.Id);
//            _context.User.RemoveFromCache(_cache, user.Id);

            _cache.RemoveUserDocsCache(me.Id);

            return Json(new { IsSubscribed = issub, new UserModel(user).Subscribers, User = new UserModel(me)});

        }

        [Authorize]
        [HttpGet("api/Users/Self")]
        public async Task<JsonResult> apiSelf()
        {
            var user = await getUserById(GetUserId().Value);

            if (user == null)
            {
                return Json(JsonError.ERROR_NOT_FOUND);
            }
           
            return Json(new UserModel(user));

        }

        [HttpPost("api/Users/Login")]
        public async Task<JsonResult> apiLogin(string login, string password)
        {
            User user;
            if (login.Contains("@"))
            {
                user = await _smngr.UserManager.FindByEmailAsync(login);
            }
            else
                user = await _smngr.UserManager.FindByNameAsync(login);
            if (user == null) return Json(JsonError.ERROR_USER_NOT_FOUND);

            await Task.Delay(1000);

            var res = await _smngr.PasswordSignInAsync(user, password, true, false);
            if (res.Succeeded)
            {
                var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
                string AccessToken = tokens.RequestToken;

                //HttpContext.Response.Headers["Set-Cookie"] += tokens.CookieToken+";";

                foreach (var s in HttpContext.Response.Headers["Set-Cookie"])
                {
                    if (s.StartsWith(".AspNetCore.Identity.Application="))
                    {
                        string val = s.Split('=', ';')[1];
                        string name = ".AspNetCore.Identity.Application";

                        return Json(new { Token = val, TokenName = name, Status = "Ok", User=new UserModel(user), AccessToken});
                    }
                }
            }

            return Json(JsonError.ERROR_ACCESS_DENIED);
        }

        // GET: Users/[Id]
        [HttpGet("api/Users/Id/{Id}", Name = "UserPageAPI")]
        public async Task<IActionResult> apiIndex(int? id)
        {
            var user = await _context.User.Include(u => u.Documents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return Json(JsonError.ERROR_NOT_FOUND);
            }

           
            return Json(new UserModel(user));

        }

    }
}
