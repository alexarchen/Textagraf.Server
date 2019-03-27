using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SearchServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.IO.Compression;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;

namespace SearchServer.Controllers
{

    public partial struct JsonError
    {
        public string error;
        public int errorcode;
        public JsonError(string err, int code) { error = err; errorcode = code; }
        // document

        static public dynamic OK = new { Status = "Ok", code = 0 };

        public enum ERROR_CODES : int
        {
           OK = 0,
           NOT_FOUND,
           ACCESS_DENIED
        }

        static public JsonError ERROR_NOT_FOUND = new JsonError ("Not found" , (int)ERROR_CODES.NOT_FOUND);
        static public JsonError ERROR_ACCESS_DENIED = new JsonError ("Access denied", (int)ERROR_CODES.ACCESS_DENIED);
        static public JsonError ERROR_FORMAT_NOT_SUP = new JsonError ("Format not supported",3);
        static public JsonError ERROR_NO_PRIVATE_SPACE = new JsonError("Private space limit exeeded. You can purchase it or load document into public space.", 4);
        static public JsonError ERROR_INTERVAL = new JsonError ("Upload interval limit exeeded. You can purchase another plan or wait some time." ,5);
        // group
        static public JsonError ERROR_ADMIN_SUBSCRIBED = new JsonError ("Admin always subscribed", 6);
        static public JsonError NEED_ASK_ADMIN = new JsonError ("Only group admins can add closed group participants. Request has been sent to group admins." ,7);
        static public JsonError ERROR_CANT_SUBSCRIBE = new JsonError ("Can't subscribe to this type of group" ,8);
        static public JsonError ERROR_USER_NOT_FOUND = new JsonError ("User not found" ,9);
        static public JsonError ERROR_ALREADY_EXISTS = new JsonError("Element already exists", 11);


        static public JsonError ERROR_UNKNOWN = new JsonError("Unexpected error", 10000000);
    }

    // shared between controllers
    public class CommonController: Controller
    {
        protected readonly UserContext _context;
        protected readonly UserManager<User> _umngr;
        protected readonly SignInManager<User> _smngr;

        public CommonController(UserContext context, SignInManager<User> mgr) 
        {
            _context = context;
            _smngr = mgr;
            _umngr = _smngr.UserManager;
        }

        virtual protected int? GetUserId()
        {
            string uid = _umngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;
            return userId;
        }

    //    ClaimsPrincipal _User =null;
    //    new internal ClaimsPrincipal User { get => _User!=null?_User:base.User; set => _User = value; }
    }

    public class DocumentsController : CommonController
    {

        private readonly IFileUploader _fileUploader;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IMemoryCache memoryCache;
        private readonly IDocodoService _docodo;

        public static string DocFolder = "Docs/";

        public DocumentsController(UserContext context, SignInManager<User> mgr,IFileUploader fu, IDocumentProcessor dp,IMemoryCache cache,IDocodoService docodo) : base (context,mgr)
        {
            _fileUploader = fu;
            _documentProcessor = dp;
            memoryCache = cache;
            _docodo = docodo;

            
        }

        // Index - current user's documents
        // Index/0 - all documents, admin only
        // Index/id - another user docs, admin only
        [Authorize]
        public async Task<IActionResult> Index(int? Id)
        {
               int userId;
               if ((User.IsInRole("Admin")) && (Id != null))
               {
                   userId = Id.Value;
               }
               else
               {
                    var user = await _umngr.GetUserAsync(User);
                    userId = user.Id;
               }

              if (userId!=0)
              return View(_context.Document.Include(d => d.Likes).Include(d => d.Comments).Include(d => d.Group).Include(d => d.User).Where(d => d.UserId.Equals(userId)).Take(200).Select(d=>new DocModel(d,false)).ToList());


            return View(_context.Document.Include(d => d.Likes).Include(d => d.Comments).Include(d => d.Group).Include(d => d.User).Take(200).Select(d => new DocModel(d, false)).ToList());

        }



        string GetHtmlTags(string html,string tag)
        {
            int q = 0;
            string ret = "";
            do
            {
                q = html.IndexOf("<" + tag, q);
                if (q >= 0)
                {
                    int p = html.IndexOf("</" + tag + ">",q);
                    ret += html.Substring(q, p + tag.Length + 3 - q );
                    q = p;
                }
                
            }
            while (q >= 0);
            return ret;
         }

        string PrepareBodyHtml(string html)
        {
            int c = 1;
            html = Regex.Replace(html, "<div class='pf", (m) => $"<a name=\"{c++}\"></a>" + m.ToString());
            html = html.Replace(" src=", " data-src=");
            //html = html.Replace("<img", "<object");
            html = Regex.Replace(html, @"width='(\d+)' height='(\d+)'", "src=\"data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 $1 $2'></svg>\"");
            html = html.Replace(" style=", " data-style=");

            return html;
        }

        // GET: Documents/Item/5
        [HttpGet("docs/Item/{Id}/{*pg}")]
        [HttpGet("[Controller]/Item/{Id}/{*pg}")]
        public async Task<IActionResult> Item(string id,string pg)
        {
            if ((id == null) || ((pg!=null) && (".pdf".Equals(pg.ToLower())) && (".epub".Equals(pg.ToLower()))))
            {
                return NotFound();
            }

            if ((pg==null) && (!HttpContext.Request.Path.Value.EndsWith("/")))
                return RedirectPermanent(HttpContext.Request.Path.Value + "/");

            if ((pg != null) && (pg.Length > 0) && (!pg.StartsWith("q=")))
            {
                IActionResult res = await apiBody(id, pg); 
                if (res.GetType().Equals(typeof(JsonResult)))
                {
                    JsonError err = (JsonError)((JsonResult)res).Value;
                    switch (err.errorcode)
                    {
                        case (int)JsonError.ERROR_CODES.NOT_FOUND:
                        return NotFound();
                        case (int)JsonError.ERROR_CODES.ACCESS_DENIED:
                        return Forbid();
                        default:
                        return NoContent();
                    }
                }
                return res;
            }

             
            dynamic document;

            string uid = _umngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;

            User _user =  null;

                // full document information
                _user = await _umngr.GetUserAsync(User);
                document = await _context.Document.Include(d => d.Group).ThenInclude(g => g.Subscribers).Include(d => d.Group).ThenInclude(g => g.Participants).Include(d => d.Group).ThenInclude(g => g.Admins).Include(d => d.Comments).ThenInclude(c => c.User).Include(d => d.Likes)
                    .Include(d => d.User).FirstOrDefaultAsync(d => d.Id.Equals(id));

            if (document == null)
            {
                return NotFound();
            }

/*            if ((document.Group!=null) && (document.Group.Type==Models.Group.GroupType.Private))
            {
                // TODO: check if document is in allowed space, document.Group can be undefined

            }
            */
            ViewBag.CanEdit = false;
            ViewBag.searchResult = null;
            if ((pg != null)  && (pg.StartsWith("q=")))
            {
                Docodo.Index.SearchResult res = _docodo.getBaseIndex().Search(pg.Substring(2) + " {name=" + id + "}");
                foreach (var p in res.foundPages)
                    if (p.text!=null)
                       p.text = Regex.Replace(p.text, @"ˋ(\w+)ˊ", "<b>${1}</b>");
                ViewBag.searchResult = res;
                ViewBag.foundWords = res.foundDocs.First().foundWords;
                pg = null;
            }

            if (document.ProcessedState != 0)
            {
                if (document.ProcessedState != Document.PROCESS_START_VALUE)
                {
                    if (document.ProcessedState == 257)
                        ViewData["Error"] = "File has copy-protection";
                    else
                        ViewData["Error"] = "Error "+ document.ProcessedState;
                }
                else
                 return View("Processing", new DocModel(document));
            }

            if ((!User.IsInRole("Admin")) && (!CanUserReadDocument(document, userId)))
            {
                return new ForbidResult();
            }

            ViewBag.IsEPub = document.File.ToLower().EndsWith(".epub");
            if (_user != null)
            {
                ViewBag.User = new UserModel(_user);
                ViewBag.User.Documents.Clear();
            }
            ViewBag.CanEdit = await CanUserEditDocument(document);
            ViewBag.UserId = (int ?) (_smngr.IsSignedIn(User) ? (int ?)(await _umngr.GetUserAsync(User)).Id : null);
            if ((userId == null) || (((Document)document).Likes.Where(l => l.UserId.Equals(userId.Value)).Count() == 0))
                ViewBag.ILiked = false;
            else ViewBag.ILiked = true;

            //    ViewBag.DocPages = _documentProcessor.GetDocStructure(DocFolder + document.UserId + "/" + id);
            bool b=false;
            ViewBag.CanPostComment = (User.IsInRole("Admin")) || (document.GroupId==null) || (GroupsController.CheckUserRightsToPost(document.Group,userId,out b));
            document.Comments.Reverse();

            // loading html doc
            string opf = "";
            if (!ViewBag.IsEPub)
            {
                string html = _documentProcessor.GetDocHtml(DocFolder + document.File.Substring(0, document.File.LastIndexOf('/') + 1));
                // prepare html
                if ((html == null) || (html.Length==0))
                {
                    ViewData["Error"] = "Internal error";
                    ViewData["body"] = "";
                }
                else
                {

                    string headers = GetHtmlTags(html, "style");// +GetHtmlTags(html, "script");
                    ViewData["Headers"] = headers;

                    if (html.LastIndexOf("</style>")>=0)
                     html = html.Substring(html.LastIndexOf("</style>") + 8);

                    html = PrepareBodyHtml(html);
                   
                    //html = html.Substring(0, html.LastIndexOf("</body>"));

                    ViewData["body"] = html;

                }
            }
            else
            {
                opf = _documentProcessor.GetDocHtml(DocFolder + document.File.Substring(0, document.File.LastIndexOf('/') + 1));
            }

            string Host = HttpContext.Connection.RemoteIpAddress.ToString();
            DateTime? lastview = ((Document)document).Reads.LastOrDefault(r => (r.UserId == userId || ((r.UserId == null) && (r.Host == Host))))?.DateTime;
            if ((lastview == null) || (DateTime.UtcNow.Subtract(lastview.Value).TotalHours>24))
            {
                document.Reads.Add(new DocumentRead() { Document = document, Host = Host, DateTime = DateTime.Now, UserId = userId });
                await _context.SaveChangesAsync();
            }

            
           // await ((Document)document).RefreshRating(_context);


            //            _context.Update(document);
            //           await _context.SaveChangesAsync();

            List<BookmarkModel> bookmarks = null;
            if (userId != null)
                bookmarks = _context.Bookmark.Where(bm => (bm.DocumentId.Equals(id) && bm.UserId.Equals(userId))).Select(bm=>new BookmarkModel(bm)).ToList();
            
            return View("Item",new DocModel(document){Bookmarks = bookmarks, Format = ViewBag.IsEPub?DocModel.DocFormat.EPub:DocModel.DocFormat.Pdf, opfUrl = Url.RouteUrl("GetDocBody",new { id = document.Id, act = opf })});
        }


        [HttpGet]
        public async Task<IActionResult> Object(string id,string title,string href,string flow)
        {
            ViewBag.Flow = flow;
            ViewBag.title = title;
            ViewBag.href = href;

            if ((flow != null) && (flow.Equals("vertical")))
            {
                DocumentInfo doc = _context.DocumentInfo.FromSql(DOCINFOSHORTSQL, id).First();
                if ((doc.GroupType!=Models.Group.GroupType.Open)
                    || (doc.GroupType != Models.Group.GroupType.Closed)
                    || (doc.GroupType != Models.Group.GroupType.Blog))
                {
                    return Forbid();
                }

                string html = _documentProcessor.GetDocHtml(DocFolder + doc.File.Substring(0, doc.File.LastIndexOf('/') + 1));
                html = PrepareBodyHtml(html);
                ViewData["body"] = html;
           }
           else
            {
                if (href == null) href = Url.Action("Item", "docs", new { Id = id });
            }
            ViewBag.Id = id;
            return View();
        }

            /// <summary>
            /// this is javascript to render, docs/Render/Id?[to={divid}][&width={width}]
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public async Task<IActionResult> Render(string id, string to, string width, string height)
        {
            IActionResult res = await Item(id,null);
            if (res.GetType().Equals(typeof(ViewResult)))
            {
                return View(((ViewResult)res).Model);
            }
            return res;
        }



        public async Task<IActionResult> Download(string id,string format)
        {
            if (id == null)
            {
                return NotFound();
            }

            Document doc = await _context.Document.FindAsync(id);
            if (doc== null)
            {
                return NotFound();
            }
            if ((doc.Access!=null) && ((((int)doc.Access.Value)&Document.DOWN_USER)==0))
            {
                if ((((int)doc.Access.Value) & Document.DOWN_GROUP) == 0) return new ForbidResult();
                // check if group user
                if (_smngr.IsSignedIn(User))
                {
                    var user = await _smngr.UserManager.GetUserAsync(User);
                    Models.Group group = await _context.Group.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == doc.GroupId);
                    if (group.Participants.FirstOrDefault(p => p.UserId.Equals(user.Id))==null)
                    {
                        return new ForbidResult();

                    }
                }

            }

            doc.Downloads++;
            _context.Update(doc);
            await _context.SaveChangesAsync();

            Stream stream = System.IO.File.OpenRead(DocFolder + doc.File);
            return new FileStreamResult(stream, GetMimeByFileName(doc.File));
        }

        public static string GetMimeByFileName(string filename)
        {
            filename = filename.ToLower();
            if (filename.EndsWith(".pdf"))
            {
                return ("application/pdf");
            }
            else
            if (filename.EndsWith(".epub"))
            {
                return ("application/epub+zip");
            }
            else
                if (filename.EndsWith(".opf")) return "application/xml";
            else
                if (filename.EndsWith(".svg")) return "image/svg+xml; charset=utf-8";

            string mime;
            if (!new FileExtensionContentTypeProvider().TryGetContentType(filename, out mime))
                mime = "text/html; charset=utf-8";

            return mime;
        }

        // GET: Documents/Create
        [Authorize]
        public async Task<IActionResult> Create(int? GroupId,string returnURL)
        {
            ViewBag.returnURL = returnURL;
            User user = await _umngr.GetUserAsync(User);
            if (!user.EmailConfirmed) return Redirect(Url.Action("RequireConfirmEmail", "Users"));

            if (GroupId != null)
            {
                var group = await _context.Group.Include(g=>g.Admins).Include(g=>g.Participants).FirstOrDefaultAsync(g=>g.Id.Equals(GroupId));


                if (group != null)
                {
                    bool be;
                    if (GroupsController.CheckUserRightsToPost(group, user.Id, out be))
                      return View(new DocModel() { GroupId = group.Id, Group = new GroupModel(group) });

                }

                return Forbid();
            }

            return View(new DocModel());

        }


        static string ConvertToBase64Arithmetic(ulong i)
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~";
            StringBuilder sb = new StringBuilder();

            do
            {
                sb.Insert(0, alphabet[(int)(i % 64)]);
                i = i / 64;
            } while (i != 0);
            return sb.ToString();
        }

        public static string GenFileName()
        {
            ulong timems = (ulong) Math.Round((DateTime.Now - new DateTime(2010, 1, 1)).TotalMilliseconds*100);
            return ConvertToBase64Arithmetic(timems); 
        }
        /// <summary>
        /// User must have PayedSubscribes and Documents with Groups loaded
        /// </summary>
        /// <param name="user"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        bool CheckAllowedPrivateSpace(User user, long size)
        {
            long maxSpace = user.PayedSubscribes.Where(ps => ps.IsValid()).Append(new PayedSubscribe()).Select(ps => ps.getOptions().AllowedPrivateSpaceMb).Max()*1000000L;
            long currSpace = user.Documents.Where(d => (d.GroupId!=null) && (d.Group.Type == Models.Group.GroupType.Private)).Select(d => d.Size).Sum();
            return (currSpace + size <= maxSpace);
        }

        bool CheckUploadIntervalLimit(User user)
        {
            int maxUploadsPerDay = user.PayedSubscribes.Where(ps => ps.IsValid()).Append(new PayedSubscribe()).Select(ps => ps.getOptions().MaxUploadsPerDay).Max();
            if (user.Documents.Count >= maxUploadsPerDay-1)
            {
                if ((DateTime.UtcNow - user.Documents.TakeLast(maxUploadsPerDay - 1).First().DateTime).TotalDays < 1)
                    return false;
             }


            return true;
        }

        bool CheckFileSizeLimit(User user, long size)
        {
            long maxSize = user.PayedSubscribes.Where(ps => ps.IsValid()).Append(new PayedSubscribe()).Select(ps => ps.getOptions().MaxFileSizeMb).Max() * 1000000L;
            return (size<=maxSize);
        }


        bool CheckFileFormat(string filename)
        {
            string ext = filename.ToLower().Substring(filename.ToLower().LastIndexOf('.'));
            return ((ext.Equals(".pdf") || (ext.Equals(".epub"))));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([Bind("Upload")] DocModel docmodel)
        {
            User user = await _umngr.GetUserAsync(User);
            if (!user.EmailConfirmed) return Redirect(Url.Action("RequireConfirmEmail", "Users"));

            string ext = docmodel.Upload.FileName.ToLower().Substring(docmodel.Upload.FileName.ToLower().LastIndexOf('.'));

            if (docmodel.GroupId!=null) docmodel.Group = new GroupModel(await _context.Group.FirstOrDefaultAsync(g=>g.Id.Equals(docmodel.GroupId)));

            if (CheckFileFormat(docmodel.Upload.FileName))
            {
                user = await _context.User.Include(u => u.PayedSubscribes).Include(u=>u.Documents).ThenInclude(d=>d.Group).FirstAsync(u => u.Id.Equals(user.Id));
                if ((User.IsInRole("Admin")) || (docmodel.Group==null) || (docmodel.Group.Type!=Models.Group.GroupType.Private) || (CheckAllowedPrivateSpace(user, docmodel.Upload.Length)))
                {
                    if (((docmodel.Group!=null) && (docmodel.Group.Type==Models.Group.GroupType.Private)) || (CheckUploadIntervalLimit(user)))
                    {
                        string fname = GenFileName() + ext;


                        await _fileUploader.Upload(fname, docmodel.Upload);


                        return Json(new { Status = "Ok", FileName = fname });
                    }
                    else
                        return Json(JsonError.ERROR_INTERVAL);
                }
                else
                    return Json(JsonError.ERROR_NO_PRIVATE_SPACE);
            }
            else
                return Json(JsonError.ERROR_FORMAT_NOT_SUP);

        }

        public static string  GetDocIdByFileName(string filename)
        {// format: .../id/.ext

            try
            {
                return Regex.Matches(filename, @"/(\w+)/\.\w+").Last().Groups[1].Value;
            }
            /*            int end = filename.LastIndexOf("/");
                        if (end <= 0) return null;
                        int start = filename.LastIndexOf('/',end-1);
                        if (start < 0) return null;

                        return (filename.Substring(start, filename.LastIndexOf("/")- DocFolder.Length));
                        */
            catch (Exception e) { }
            return null;
        }

    
        [Authorize]
        public IActionResult DropTempFile(string Id)
        {
            _fileUploader.DropFile(Id);
            return new OkResult();
        }

        // POST: Documents/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Tags,Description,TempFileName,Access,GroupId")] DocModel docmodel,string returnURL)
        {

            if (ModelState.IsValid)
            {
                Document document = (Document)docmodel;
                document.User = await _umngr.GetUserAsync(User);
                if (!document.User.EmailConfirmed) return Redirect(Url.Action("RequireConfirmEmail", "Users"));


                if ((docmodel.TempFileName!=null) && (CheckFileFormat(docmodel.TempFileName)))
                {
                    long size = new FileInfo(_fileUploader.Folder + "/" + docmodel.TempFileName).Length;

                    Models.Group group=null;
                    if (docmodel.GroupId != null)
                    {
                        group = await _context.Group.Include(g=>g.Admins).Include(g=>g.Participants).FirstOrDefaultAsync(g => g.Id.Equals(docmodel.GroupId));
                        docmodel.Group = new GroupModel(group);
                    }
                    document.User = await _context.User.Include(u => u.PayedSubscribes).Include(u => u.Documents).ThenInclude(d => d.Group).FirstAsync(u => u.Id.Equals(document.User.Id));
                    bool b;
                    // access rights
                    if ((User.IsInRole("Admin")) || (docmodel.Group == null) || (GroupsController.CheckUserRightsToPost(group, document.User.Id, out b)))
                    {
                        // upload interval and space
                        if (((docmodel.Group == null) && (CheckUploadIntervalLimit(document.User)))
                            || ((docmodel.Group != null) && ((docmodel.Group.Type == Models.Group.GroupType.Private) || (CheckAllowedPrivateSpace(document.User, size)))))
                        {
                            document.Id = docmodel.TempFileName;
                            document.Id = document.Id.Substring(0, document.Id.LastIndexOf('.'));
                            string ext = docmodel.TempFileName.Substring(docmodel.TempFileName.LastIndexOf('.'));

                            string folder = DocFolder + $"{document.User.Id}/{document.Id}";
                            Directory.CreateDirectory(folder);
                            System.IO.File.Move(_fileUploader.Folder + "/" + docmodel.TempFileName, folder + "/" + ext);
                            document.File = $"{document.User.Id}/{document.Id}/{ext}";
                            document.ProcessedState = Document.PROCESS_START_VALUE;
                            document.DateTime = DateTime.UtcNow;
                            document.Size = new System.IO.FileInfo(folder + "/" + ext).Length;

                            _documentProcessor.ProcessDocument(folder + "/" + ext, (f, s, u) =>
                            {
                                Document doc = u.Document.Find(GetDocIdByFileName(f));
                                doc.ProcessedState = s.Code;
                                doc.Pages = s.nPages;
                                doc.TitlePage = s.Title;
                                u.Document.Update(doc);
                                u.SaveChanges();
                            });

                            _context.Add(document);
                            await _context.SaveChangesAsync();
                            if ((returnURL != null) && (returnURL.Length > 0))
                                return Redirect(returnURL);
                            else
                                return RedirectToAction("Index");

                        }
                        ModelState.AddModelError("Upload", "Upload limits exeeded");
                    }
                    else return Forbid();
                }
                else ModelState.AddModelError("Upload", "Must be pdf or epub file");

            }

            if (docmodel.GroupId != null)
            {
                docmodel.Group = new GroupModel(_context.Group.Find(docmodel.GroupId));
            }

            return View(docmodel);

        }

        /// <summary>
        /// Fast variant of CanUserReadDocument
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool CanUserReadDocument(DocumentInfo doc, int? userId)
        {
            // everybody can read document not in group
            if (doc.GroupId == null) return true;
            return GroupsController.CheckUserRightsToRead(doc.GroupType.Value, doc.IsGAdmin != null, doc.IsGUser != null, userId);
        }
        /// <summary>
        /// Checks if user can read document, whose Group must be loaded
        /// </summary>
        /// <param name="doc">document to read</param>
        /// <param name="user">user</param>
        /// <returns>true if user can read document</returns>
        public static bool CanUserReadDocument(Document doc, int? userId)
        {
            // everybody can read document not in group
            if (doc.GroupId == null) return true;
            if ((doc.GroupId != null) && (doc.Group == null)) throw new Exception("Group must not be null");
            return GroupsController.CheckUserRightsToRead(doc.Group, userId);
        }
        /// <summary>
        /// Check if user can edit document, document Group and group admins must be loaded
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        async Task<bool> CanUserEditDocument(Document document)
        {
            if (User.IsInRole("Admin")) return true;
            string uid = _umngr.GetUserId(User);
            int? userId = uid!=null?(int ?)int.Parse(uid):null;
            //User _user = await _umngr.GetUserAsync(User);
            if (userId == null) return false;
            if (userId.Equals(document.UserId)) return true;
            if ((document.GroupId!=null) && (document.Group.Admins.Any(ga => ga.UserId.Equals(userId)))) return true;
            return false;
        }
        /// <summary>
        /// Check if user can delete document permanently (only global admin and doc's creator), document Group and group admins must be loaded
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        async Task<bool> CanUserDeleteDocument(Document document)
        {
            if (User.IsInRole("Admin")) return true;
            string uid = _umngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;
            //User _user = await _umngr.GetUserAsync(User);
            if (userId == null) return false;
            if (userId.Equals(document.UserId)) return true;
            return false;
        }

        // GET: Documents/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Document document = await _context.Document.Include(d=>d.Group).FirstOrDefaultAsync(d=>d.Id.Equals(id));

            if (document == null)
            {
                return NotFound();
            }

            if (!await CanUserEditDocument(document))
                return new ForbidResult();

            return View(new DocModel(document));
        }

        // POST: Documents/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Title,TempFileName,Tags,Access,Additional,Description")] DocModel docmodel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Document document = await _context.Document.Include(d => d.Group).FirstOrDefaultAsync(d => d.Id.Equals(id));

                    if (! await CanUserEditDocument(document))
                     return new ForbidResult();
                    _context.Update(docmodel.Update(document));
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Title", "Error Update, try later");
                }
            }
            return View(docmodel);
        }

        private bool DocumentExists(string id)
        {
            return _context.Document.Any(e => e.Id == id);
        }


        /* API */
        int? GetUserId()
        {
            string uid = _umngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;
            return userId;
        }

        internal async Task TryDeletePermanent(Document document)
        {
            // look if bookmarks are exit
            //bool deletepermanent = false;
            if (_context.Bookmark.Any(b => (b.DocumentId.Equals(document.Id) && (b.UserId != document.UserId))))
            {
                // just set deleted status, if last bookmark will be deleted, document will be also
                document.DocStatus = Document.DocStatusEnum.Deleted;
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Document.Remove(document);
                await _context.SaveChangesAsync();
                Directory.Delete(DocFolder + document.File.Substring(0, document.File.LastIndexOfAny(new char[] { '/', '\\' })), true);
            }
        }

        [Authorize]
        [HttpDelete("api/docs/{Id}/delete", Name = "DeleteDoc")]
        [Produces("application/json")]
        public async Task<JsonResult> apiDelete(string id)
        {
            var document = await _context.Document.FindAsync(id);
            if (document == null) return Json(JsonError.ERROR_NOT_FOUND);
            if (!await CanUserEditDocument(document)) return Json(JsonError.ERROR_ACCESS_DENIED);
            if (await CanUserDeleteDocument(document))
            {

                try
                {
                    TryDeletePermanent(document);
                }
                catch (Exception e)
                {
                    return Json(JsonError.ERROR_UNKNOWN);

                }
            }
            else
            { // not owner
                // just make it invisible in group
                document.DocStatus = Document.DocStatusEnum.Invisible;
                _context.Update(document);
                await _context.SaveChangesAsync();

                await MessagesController.PostMessageAsync(_context, new Message()
                {
                    toUserId = document.UserId,
                    fromUserId = 0,
                    DateTime = DateTime.UtcNow,
                    Type = Message.MessageType.DocRemoved,
                    text = document.Id
                });
            }
            return Json(JsonError.OK);
        }




        [Authorize]
        [HttpGet("api/docs/list/{id?}", Name = "DocsList")]
        public IActionResult apiList(int? Id)
        {

            int userId = 0;
            if ((User.IsInRole("Admin")) && (Id != null))
            {
                userId = Id.Value;
            }
            else
            {
                _umngr.GetUserAsync(User).ContinueWith(
                    (u) => userId = u.Result.Id).Wait();

            }

            var sett = new Newtonsoft.Json.JsonSerializerSettings();
            sett.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            if (userId != 0)
                return Json(_context.Document.Include(d => d.Likes).Include(d => d.Comments).Include(d => d.Group).Include(d => d.User).Where(d => d.UserId.Equals(userId)).Take(200).Select(d => new DocModel(d,false)).ToList(), sett);


            return Json(_context.Document.Include(d => d.Likes).Include(d => d.Comments).Include(d => d.Group).Include(d => d.User).Take(200).Select(d => new DocModel(d,false)).ToList(), sett);

        }
        [HttpGet("api/docs/item/{Id}", Name = "GetDoc")]
        public async Task<IActionResult> apiItem(string id)
        {
            if (id == null)
            {
                return Json(JsonError.ERROR_NOT_FOUND);
            }

            var document = await _context.Document.Include(d => d.Group).ThenInclude(g => g.Subscribers).Include(d => d.Group).ThenInclude(g => g.Admins).Include(d => d.Comments).ThenInclude(c => c.User).Include(d => d.Likes)
                .Include(d => d.User).FirstOrDefaultAsync(d => d.Id.Equals(id));
            if (document == null)
            {
                return Json(JsonError.ERROR_NOT_FOUND);
            }

            string uid = _umngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;
            if ((!User.IsInRole("Admin")) && (!CanUserReadDocument(document, userId)))
            {
                return Json(JsonError.ERROR_ACCESS_DENIED);
            }

            bool IsEPub = document.File.ToLower().EndsWith(".epub");

            dynamic res = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(new DocModel(document)));

            ViewBag.UserId = _smngr.IsSignedIn(User) ? (await _umngr.GetUserAsync(User)).Id : 0;
            if (document.Likes.Where(l => l.UserId.Equals(userId)).Count() == 0)
                res.ILiked = false;
            else res.ILiked = true;


            res.Format = IsEPub ? DocModel.DocFormat.EPub : DocModel.DocFormat.Pdf;
            if (IsEPub)
                res.opfUrl = Url.RouteUrl("GetDocBody", new { id = document.Id, act = _documentProcessor.GetDocHtml(DocFolder + document.File.Substring(0, document.File.LastIndexOf('/') + 1)) });


            string Host = HttpContext.Connection.RemoteIpAddress.ToString();
            DateTime lastview = ((Document)document).Reads.LastOrDefault(r => (r.UserId == userId || ((r.UserId == null) && (r.Host == Host)))).DateTime;
            if ((lastview == null) || (DateTime.UtcNow.Subtract(lastview).TotalHours > 24))
            {
                document.Reads.Add(new DocumentRead() { Document = document, Host = Host, DateTime = DateTime.Now, UserId = userId });
            }

            return Json(res);
        }

        const string DOCINFOSQL = "SELECT TOP(1) d.*, g.[Type] AS GroupType,ga.[UserId] AS IsGAdmin, gu.GroupId AS IsGUser FROM dbo.Document AS d " +
                    "LEFT JOIN dbo.[Group] AS g ON g.Id=d.GroupId LEFT JOIN dbo.GroupAdmin AS ga ON(ga.GroupId = g.Id AND ga.UserId = {0}) " +
                    "LEFT JOIN dbo.GroupUser AS gu ON(gu.GroupId = g.Id AND gu.UserId = {0})" +
                    " WHERE d.Id = {1}";

        const string DOCINFOSHORTSQL = "SELECT TOP(1) d.*, g.[Type] AS GroupType FROM dbo.Document AS d " +
                    "LEFT JOIN dbo.[Group] AS g ON g.Id=d.GroupId WHERE d.Id = {0}";

        [HttpGet("api/docs/{Id}/body/{*act}", Name = "GetDocBody")]
        [Produces("application/json")]
        public async Task<IActionResult> apiBody(string id, string act)
        {
            dynamic doc;
            
            string uid = _umngr.GetUserId(User);
            int? userId = uid != null ? (int?)int.Parse(uid) : null;


            if ((act == null) || (act.Length == 0) || (_context.IsInMem))
                doc = await _context.Document.Include(d => d.Group).ThenInclude(g => g.Subscribers).Include(d => d.Group).ThenInclude(g => g.Participants).Include(d => d.Group).ThenInclude(g => g.Admins).Include(d => d.Comments).ThenInclude(c => c.User).Include(d => d.Likes)
                   .Include(d => d.User).FirstOrDefaultAsync(d => d.Id.Equals(id));
            else
            {
                // faster request
                doc =  _context.DocumentInfo.FromSql(DOCINFOSQL,userId!=null?userId:0,id).First();
                //await _context.Document.Include(d => d.Group).ThenInclude(g => g.Subscribers).Include(d => d.Group).ThenInclude(g => g.Participants).Include(d => d.Group).ThenInclude(g => g.Admins).Include(d => d.User).FirstOrDefaultAsync(d => d.Id.Equals(id));
            }
            if (doc == null) return Json(JsonError.ERROR_NOT_FOUND);

            if ((!User.IsInRole("Admin")) && (!CanUserReadDocument(doc, userId)))
            {
                return Json(JsonError.ERROR_ACCESS_DENIED);
            }

            bool IsEPub = doc.File.ToLower().EndsWith(".epub");

            if ((act == null) || (act.Length==0) || (act.Equals("body")))
            {
                act = ".html";
            }

            try
            {
                string filename = DocFolder + doc.File.Substring(0, doc.File.LastIndexOf('/') + 1) + Uri.UnescapeDataString(act);
                if ((filename.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) && (System.IO.File.Exists(filename + "z")))
                    filename += "z";
                Stream stream = System.IO.File.OpenRead(filename);
                if (!IsEPub)
                {
                    if (filename.EndsWith(".svgz",StringComparison.OrdinalIgnoreCase))
                     ControllerContext.HttpContext.Response.Headers.Add("Content-Encoding", "gzip");
                    
                    return File(stream, GetMimeByFileName(act));
                }
                else
                {
                    return File(stream, GetMimeByFileName(Uri.UnescapeDataString(act)));

                }
            }
            catch (IOException e)
            {
                if (!IsEPub)
                { // pdf exception

                    if (act.ToLower().EndsWith(".fore.png"))
                    {
                        //HttpContext.Response.Headers.Add("Content-Transfer-Encoding", "base64");
                        // transparent pixel
                        return File(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="), "image/png");
                    }
                    else
                    if (act.ToLower().EndsWith(".back.jpg"))
                    {
                        HttpContext.Response.Headers.Add("Content-Transfer-Encoding", "base64");
                        // white pixel
                        return File(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII="), "image/png");

                    }
                }
            }
            
            return NoContent();
            
        }

        [HttpGet("api/docs/{Id}/Like", Name = "DocLike")]
        [Produces("application/json")]
        [Authorize]
        // Like or dislike document
        public async Task<IActionResult> apiLike(string id, string apiKey)
        {
            Document doc = await _context.Document.Include(d=>d.User).Include(d => d.Likes).FirstAsync(d => d.Id.Equals(id));
            if (doc == null) return Json(JsonError.ERROR_NOT_FOUND);

            if (_smngr.IsSignedIn(User))
            {
                User _user = await _umngr.GetUserAsync(User);

                DocumentLike like = _context.DocumentLike.FirstOrDefault(l => ((l.UserId == _user.Id) && (l.DocumentId.Equals(doc.Id))));
                int nLikes = doc.Likes.Count();

                if (like == null)
                {
                    // adding doc like
                    doc.Likes.Add(new DocumentLike(_user.Id, doc.Id));
                    //                    _context.DocumentLike.Add(new DocumentLike(_user.Id,doc.Id));
                    nLikes++;
                }
                else
                {
                    // removing doc like
                    doc.Likes.Remove(like);
                    nLikes--; if (nLikes < 0) nLikes = 0;
                }

                _context.Update(_user); // update user cache
                await doc.RefreshRating(_context);

 //               _context.User.RemoveFromCache(memoryCache, _user.Id);
//                await _context.SaveChangesAsync();

                JsonSerializerSettings set = new JsonSerializerSettings();
                set.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                return Json(new
                {
                    Likes = doc.Likes.Select(l => new DocLikeModel(l)).ToList(),
                    ILiked = (like == null),
                    Liker = new UserModel(_user)
                }, set);
            }

            return Json(JsonError.ERROR_ACCESS_DENIED);
        }


        /*
        [HttpGet("api/docs/{Id}/page/{n}",Name = "GetDocPage")]
        public async Task <IActionResult> Page(string id,int n)
        {
            //Document doc = await _context.Document.Include(d=>d.User).FirstAsync(d=>d.Id.Equals(id));
            Document doc = await _context.Document.FindAsync(id);
            if (doc==null) return new NotFoundResult();
            
            ContentResult cnt = new ContentResult();
            cnt.Content = _documentProcessor.GetPageImage(DocFolder + doc.UserId + "/"+id, n);
            cnt.ContentType = "text/html";
            cnt.StatusCode = 200;
            return cnt;
        }*/

        [Route("api/docs/{Id}/thumb/{n}", Name = "GetDocThumb")]
        public async Task<IActionResult> apiThumb(string id, int n)
        {
            Document doc = await _context.Document.FindAsync(id);
            if (doc == null) return Json(JsonError.ERROR_NOT_FOUND);
            var thumb = _documentProcessor.GetThumbnail(DocFolder + doc.UserId + "/" + id, n);
            if (thumb == null) return Redirect("/images/processing.jpg");
            return new FileContentResult(thumb, "image/jpeg");

        }

        

        [Route("api/docs/{Id}/preview/{n}", Name = "GetDocPreview")]
        public async Task<IActionResult> apiPreview(string id, int n)
        {
            Document doc = await _context.Document.FindAsync(id);
            if (doc == null) return Json(JsonError.ERROR_NOT_FOUND);
            var thumb = _documentProcessor.GetThumbnail(DocFolder + doc.UserId + "/" + id, n, 1);
            if (thumb == null) return Redirect("/images/processing.jpg");
            return new FileContentResult(thumb, "image/jpeg");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("api/docs/{Id}/process", Name = "ProcessDoc")]
        public async Task<IActionResult> apiProcess(string id)
        {
            //Document doc = await _context.Document.Include(d=>d.User).FirstAsync(d=>d.Id.Equals(id));
            Document doc = await _context.Document.FindAsync(id);
            if (doc == null) return Json(JsonError.ERROR_NOT_FOUND);
            doc.ProcessedState = Document.PROCESS_START_VALUE;
            _context.Update(doc);
            await _context.SaveChangesAsync();

            _documentProcessor.ProcessDocument(DocFolder + doc.File, async (docid, val, c) => {
                Document d = (await c.Document.Where(dd => dd.Id.Equals(id)).FirstOrDefaultAsync());
                if (d != null)
                {
                    d.ProcessedState = val.Code;
                    d.Pages = val.nPages;
                    d.TitlePage = val.Title;
                    c.Update(d);
                    await c.SaveChangesAsync();
                }

            });

            return Json(new DocModel(doc));
        }



    }
}
