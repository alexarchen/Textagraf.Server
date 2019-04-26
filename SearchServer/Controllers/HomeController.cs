using System;
using System.Collections.Generic;
//using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Docodo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SearchServer.Models;
using SearchServer.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Collections;
using System.Linq.Expressions;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace SearchServer.Controllers
{

    /// <summary>
    /// Cache extentions
    /// </summary>
    public static class CACHE_VARS
    {
        public static (string name,int time) FAV_DOCS_CACHE = ("FavDoclist",300);
        public static (string name, int time) USERS_CACHE = ("UsersCache", 300);

        static HashSet<string> keyValues = new HashSet<string>();

        /*        static Dictionary<Type, object> DictionaryCache<Type>(IMemoryCache cache, string name, int time)
                {
                    Dictionary<Type, object> ret;
                    if (!cache.TryGetValue(name, out ret))
                    {
                        ret = new Dictionary<Type, object>();
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromSeconds(time))
                            .SetSize(;

                        cache.Set(name, ret, cacheEntryOptions);
                    }
                    return ret;

                }

                static void RemoveDictionaryCache<Type>(IMemoryCache cache, string name, Type id)
                {
                    Dictionary<Type, object> ret;
                    if (cache.TryGetValue(name, out ret))
                    {
                        if (ret.ContainsKey(id)) ret.Remove(id);
                    }
                }
                */

        static Newtonsoft.Json.JsonSerializerSettings set = new Newtonsoft.Json.JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        };

        public static async Task<T> CacheAsync<T>(this IMemoryCache cache,string name, int timesec, object key, Func<Task<T>> func)
        {
            T ret = cache.Get<T>(name + "#" + key);
            if (ret != null) return ret;
            ret = await func();
            long size = Newtonsoft.Json.JsonConvert.SerializeObject(ret, set).Length;
            cache.Set(name + "#" + key, ret, new MemoryCacheEntryOptions().SetSlidingExpiration(new TimeSpan(0,0,timesec)).SetSize(size));
            try
            {
                keyValues.Add(name + "#" + key);
            }
            catch (Exception e) { }
            return ret;
        }

    
        public static void RemoveCache(this IMemoryCache cache, string name, object key)
        {
            cache.Remove(name + "#" + key);
            keyValues.Remove(name + "#" + key);
        }

        
        public static void RemoveFromCache<T>(this IQueryable<T> queryable,IMemoryCache cache, object key)
        {
            cache.Remove(queryable.GetType().GUID + "#" + key);
            keyValues.Remove(queryable.GetType().GUID + "#" + key);
        }
        
        public static void RemoveUserDocsCache(this IMemoryCache cache, int userId)
        {
            cache.RemoveCache(FAV_DOCS_CACHE.name, userId);
        }


        public class Cached<T>
        {
            IMemoryCache _cache;
            Func<T, object> _keysel;
            public Cached(IQueryable<T> queryable, IMemoryCache cache, Func<T, object> keysel)
            {
                _cache = cache;
                _queryable = queryable;
                _keysel = keysel;
            }
            IQueryable<T> _queryable;
            public async Task<T> FirstOrDefaultAsync(Func<T,bool> selector)
            {
                nCalls++;
                T ret = FromCache(selector);
                if (ret == null)
                {
                    ret = await _queryable.FirstOrDefaultAsync(v=>selector(v));
                    if (ret != null) AddToCache(ret);
                }
                else nCachedCalls++;
                Console.WriteLine($"Cache stat: {100 * nCachedCalls / nCalls}%");
                return ret;
            }
            public async Task<T> FirstOrDefaultAsync(object key)
            {
                nCalls++;
                T ret = FromCache(key);
                if (ret == null)
                {
                    ret = await _queryable.FirstOrDefaultAsync(v => _keysel(v).Equals(key));
                    if (ret != null) AddToCache(ret);
                }
                else nCachedCalls++;
                Console.WriteLine($"Cache stat: {100*nCachedCalls/nCalls}%");
                return ret;
            }

            public string CacheKey { get => _queryable.GetType().GUID.ToString(); }

            static int nCalls = 0;
            static int nCachedCalls = 0; 
            public T FirstOrDefault(Func<T, bool> selector)
            {
                nCalls++;
                T ret = FromCache(selector);
                if (ret == null)
                {
                    ret = _queryable.FirstOrDefault(v => selector(v));
                    if (ret != null) AddToCache(ret);
                }
                else nCachedCalls++;
                Console.WriteLine($"Cache stat: {100 * nCachedCalls / nCalls}%");
                return ret;
            }
            public T FirstOrDefault(object key)
            {
                nCalls++;
                T ret = FromCache(key);
                if (ret == null)
                {
                    ret = _queryable.FirstOrDefault(v => _keysel(v).Equals(key));
                    if (ret != null) AddToCache(ret);
                }
                else nCachedCalls++;
                Console.WriteLine($"Cache stat: {100 * nCachedCalls / nCalls}%");
                return ret;
            }

            public T FromCache(Func<T, bool> selector)
            {
                Dictionary<object, T> dict = _cache.Get<Dictionary<object, T>>(CacheKey);
                if (dict != null)
                {
                    return (dict.Select(p=>p.Value).FirstOrDefault(p => selector(p)));
                }
                return default(T);
            }
            /// <summary>
            /// Get by key, faster
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public T FromCache(object key)
            {
                Dictionary<object, T> dict = _cache.Get<Dictionary<object, T>>(CacheKey);
                if (dict != null)
                {
                    if (dict.ContainsKey(key)) return dict[key];
                }
                return default(T);
            }

            void AddToCache(T val)
            {
                Dictionary<object, T> dict = _cache.Get<Dictionary<object, T>>(CacheKey);
                if (dict == null) dict = new Dictionary<object, T>();

                object key = _keysel(val);
                if (dict.ContainsKey(key)) dict[key] = val;
                else dict.Add(key, val);

                _cache.Set(CacheKey, dict);
            }
            /// <summary>
            /// Clear cache of this entity
            /// </summary>
            public void Clear(object key)
            {
                Dictionary<object, T> dict = _cache.Get<Dictionary<object, T>>(CacheKey);
                if (dict != null)
                {
                    if (dict.ContainsKey(key)) dict.Remove(key);
                }

            }

            public void Clear()
            {
                _cache.Remove(CacheKey);
            }
        }


        public static Cached<T> GetCached<T>(this IQueryable<T> queryable, IMemoryCache cache, Func<T, object> keysel)
        {
            return new Cached<T>(queryable, cache, keysel);
        }

        public static void ClearCache<T>(this IQueryable<T> queryable,IMemoryCache cache)
        {
            new Cached<T>(queryable, cache, null).Clear();
        }


        /*
        public static T GetFromCache<T>(this IQueryable<T> queryable, IMemoryCache cache, object key) 
        {
            T ret = cache.Get<T>(queryable.GetType().GUID + "#" + key);
            return ret;
        }
        

        public static void Clear(this IMemoryCache cache, string name)
        {
            foreach (string key in keyValues.Where(k=>k.StartsWith(name+"#")))
            {
                cache.Remove(key);
                keyValues.Remove(key);
            }
        }*/
    }

    public class HomeController : Controller
    {
        IDocodoService docodo;
        IMemoryCache Cache;
        SignInManager<User> smngr;
        private readonly UserContext _context;
        IEmailSender _emailSender;
        IConfiguration Configuration;

        public HomeController(IMemoryCache memoryCache,UserContext context, IDocodoService service, SignInManager<User> mngr, IEmailSender emailSender, IConfiguration configuration)
        {
            smngr = mngr;
            docodo = service;
            _context = context;
            Cache = memoryCache;
            _emailSender = emailSender;
            Configuration = configuration;
        }


        public async Task<IActionResult> Index()
        {
            List<DocModel> doclist = new List<DocModel>();

            ViewBag.DisplayNotPersonal = false;

            ViewData["FromCache"] = true;

            if (smngr.IsSignedIn(User))
            {
                var user = await smngr.UserManager.GetUserAsync(User);

                doclist = await Cache.CacheAsync(CACHE_VARS.FAV_DOCS_CACHE.name, CACHE_VARS.FAV_DOCS_CACHE.time, user.Id, async () =>
                {

                    user = await _context.User.Include(u => u.Likes).ThenInclude(l => l.Document).ThenInclude(d => d.User)
                        .Include(u => u.AdminOfGroups).ThenInclude(ga => ga.Group).ThenInclude(g => g.Documents).ThenInclude(d => d.User)
                        .Include(u => u.Participate).ThenInclude(gu => gu.Group).ThenInclude(g => g.Documents).ThenInclude(d => d.User)
                        .Include(u => u.SubscribesToGroups).ThenInclude(gs => gs.Group).ThenInclude(g => g.Documents).ThenInclude(d => d.User)
                        .Include(u => u.SubscribesToUsers).ThenInclude(us => us.ToUser).ThenInclude(u => u.Documents).ThenInclude(d => d.User)
                        .FirstOrDefaultAsync(u => u.Id.Equals(user.Id));

                    try
                    {
                        doclist = user.GetAllGroupsList().Select(g => g.Documents.Where(d => d.ProcessedState != DocModel.PROCESS_START_VALUE).TakeLast(30))
                           .Concat(user.SubscribesToUsers.Select(us => us.ToUser.Documents.Where(d => d.ProcessedState != DocModel.PROCESS_START_VALUE).TakeLast(30))).Aggregate((a, b) => a.Concat(b)).Distinct().OrderBy(d => d.Id).TakeLast(30).Select(d => new DocModel(d)).ToList();
                    }
                    catch (Exception e) { }


                    if (doclist.Count < 24) // not subscribed jet
                    {
                        ViewBag.DisplayNotPersonal = true;
                        doclist = doclist.Concat(_context.Document.Include(d => d.User).Include(d => d.Likes).OrderBy(d => d.Rating).Where(d => d.ProcessedState != DocModel.PROCESS_START_VALUE).Take(100).Select(d => new DocModel(d, false)).TakeRandom(24 - doclist.Count).ToList()).ToList();
                    }

                    ViewData["FromCache"] = false;

                    return doclist;
                });

                return View(doclist);
            }
            else 
             if (HttpContext.Request.Headers.ContainsKey("nativeApp"))
             {
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = HttpContext.Request.Path });
             }

            doclist = await Cache.CacheAsync(CACHE_VARS.FAV_DOCS_CACHE.name, CACHE_VARS.FAV_DOCS_CACHE.time, 0, async () =>
            {
                doclist = _context.Document.Include(d => d.Likes).Include(d => d.Group).Where(d => (d.GroupId == null) || (d.Group.Type == Group.GroupType.Blog) || (d.Group.Type == Group.GroupType.Open)).OrderBy(d => d.Rating).Include(d => d.User).Where(d => d.ProcessedState != DocModel.PROCESS_START_VALUE).Take(100).Select(d => new DocModel(d, false)).TakeRandom(10).ToList();
                ViewData["FromCache"] = false;
                return doclist;
            });

            return View("start", doclist);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }


        public class ContactModel
        {
            [DataType(DataType.EmailAddress)]
            public string Email { get; set; }

            [Required]
            [MaxLength(3000)]
            public string Text { get; set; }
        }

        public IActionResult Contact()
        {

            if (!smngr.IsSignedIn(User))
                ViewData["ShowEmail"] = true;
             return View();
        }

        public static string HelpUrl="";
        public IActionResult Help(string id)
        {
            if (HelpUrl.Length > 0)
                return Redirect(HelpUrl+(id!=null?"#"+id:""));

            return View();
        }

        [HttpPost]
        public async Task <IActionResult> Contact (ContactModel model)
        {

            if (ModelState.IsValid)
            {
                User user = null;
                if (!smngr.IsSignedIn(User)) 
                {
                    if (model.Email.Length == 0)
                    {
                        ModelState.AddModelError("Email", "Enter email address");
                        return View();
                    }
                }
                else
                {
                    user = await smngr.UserManager.GetUserAsync(User);

                }

                string htmlMessage = "You have message!<br>From: " + (user != null ? user.UserName : "") + "," + model.Email
                    + "<br><br>" + model.Text;

                await _emailSender.SendEmailAsync(Configuration["Init:Admin:Email"], "From Server",htmlMessage);
                ViewData["Message"] = "Your message has been sent to admin.";

            }

            return (View(new ContactModel()));
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles="Admin")]
        public IActionResult Log()
        {
            StringBuilder output = new StringBuilder("<pre>\n");


            foreach (string f in Directory.EnumerateFiles("..\\logs", "*.log").Select(f => new FileInfo(f)).OrderBy(f => f.CreationTimeUtc).TakeLast(3).Select(fi => fi.FullName))
            {
                using (Stream fs = new System.IO.FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader read = new StreamReader(fs))
                    {
                        output.AppendLine(read.ReadToEnd());
                    }
                }

                    
            }

            output.Append("\n</pre>");

            return Content(output.ToString());
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}
