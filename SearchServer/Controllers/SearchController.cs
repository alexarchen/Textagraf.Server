using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Docodo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SearchServer.Models;

namespace SearchServer.Controllers
{
    public class SearchController : Controller
    {
        private IDocodoService docodo;
        private UserContext _context;
        /*
        public class SearchResultModel
        {
            public int nFoundDocs;
            public int SearchTime;
            public FoundDocModel[] foundDocs;
            public string Error;
            public bool Success;
            public Index.SearchResult.WordInfo[] words;
        }

        public class FoundDocModel
        {
            public FoundDocModel() { }
            public FoundDocModel(Models.Document doc,Index.ResultDocument sdoc)
            {
                doc = new DocumentsController.DocModel(doc);
                searchinfo = sdoc;
            }
            public DocumentsController.DocModel doc;
            public Index.ResultDocument searchinfo;
        }
        */
        public class SettingsModel
        {
            IDocodoService _service;
            Index index;

            public SettingsModel()
            {

            }


            public SettingsModel(IDocodoService service)
            {
                _service = service;
                langs = _service.getLanguages();
                index = service.getBaseIndex();
            }

            public bool IsCreating { get => index?.IsCreating ?? false; }
            public ulong IndexSize { get => index?.MaxCoord ?? 0; }
            public int nIndexWords { get => index?.Count ?? 0; }
            public int UpdateInterval { get; set; }
            public string[] langs { get; set; }

            public void Save()
            {

            }
        }

        

        public SearchController(IDocodoService docodoService,UserContext context) : base()
        {
            docodo = docodoService;
            _context = context;
        }


        [HttpGet]
        public IActionResult Search(string q,int start,int? group,string doc)
        {
            Index index = docodo.getBaseIndex();

            //string fieldrequest = "";
            /*
            foreach (var key in HttpContext.Request.Query.Keys)
            {
                if ((!key.Equals("q")) && (!key.Equals("start")))
                {
                    if (key.Equals("group"))
                        fieldrequest += $" {{GroupId={HttpContext.Request.Query[key]}}} ";
                    else
                        if (key.Equals("doc"))
                    {

                    }

                    fieldrequest += $" {{{key}={HttpContext.Request.Query[key]}}} ";
                }
            }*/
            if (doc != null) return Redirect("/docs/Item/" + doc + "/q=" + q);

            Regex.Replace(q, @"(#\w+)", (m) => "{Tags=" + m.Value.Substring(1) + "}");
            if (group != null) q += " {GroupId=" + group + "} ";
            if (doc != null) q += " {name=" + doc + "} ";

          //  long t = Environment.TickCount;

            Index.SearchResult res = index.Search(q);
            foreach (var d in res.foundDocs)
            {
                d.Name = d.headers["Name"];
                Dictionary<string, string> hdrs = new Dictionary<string, string>();
                foreach (var h in d.headers)
                    hdrs.Add (h.Key,Regex.Replace(h.Value, @"ˋ(\w+)ˊ", "<b>${1}</b>"));
                d.headers = hdrs;
                if (d.summary != null)
                 d.summary = Regex.Replace(d.summary, @"ˋ(\w+)ˊ", "<b>${1}</b>");
                foreach (var p in d.pages)
                    p.text = Regex.Replace(p.text, @"ˋ(\w+)ˊ", "<b>${1}</b>");
            }
            HashSet<int> u_set = res.foundDocs.Select(fd => int.Parse(fd.headers["UserId"])).ToHashSet();
            ViewBag.Users = _context.User.Where(u => u_set.Contains(u.Id)).Select(u=>new UserModel(u,false)).ToDictionary((u) => u.Id.ToString());
            HashSet<int> g_set = res.foundDocs.Where(d=>d.headers.ContainsKey("GroupId")).Select(fd => int.Parse(fd.headers["GroupId"])).ToHashSet();
            ViewBag.Groups = _context.Group.Where(g => g_set.Contains(g.Id)).Select(g => new GroupModel(g, false)).ToDictionary((g) => g.Id.ToString());

            /*
            var fd = sres.foundDocs.Skip(start).Take(10);
            var ids = fd.Select(d => d.headers["Id"]).ToList();

            var docs = (from d in _context.Document where ids.Contains(d.Id) select d).ToList();

            
            List<FoundDocModel> fdms = new List<FoundDocModel>();

            foreach (var doc in sres.foundDocs)
            {
                //doc.Name = doc.Name.Substring(doc.Name.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
                fdms.Add(new FoundDocModel(docs.FirstOrDefault(d => d.Id.Equals(doc.headers["Id"])), doc));
            }
            SearchResultModel res = new SearchResultModel();
            res.foundDocs = fdms.ToArray();
            res.Error = sres.Error;
            res.Success = sres.Success;
            res.words = sres.words;
            res.nFoundDocs = sres.foundDocs.Count;
            res.SearchTime = (int)( Environment.TickCount - t);
            */
            return View(res);

        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Settings()
        {
            return View(new SettingsModel(docodo));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Settings(SettingsModel model)
        {
            // saving vars
            //...
            return View(new SettingsModel(docodo));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Update()
        {
            docodo.getBaseIndex().CreateAsync();

            return View("Settings", new SettingsModel(docodo));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult GetState()
        {
            return Json(new SettingsModel(docodo));
        }

        [HttpGet]
        public async Task<IActionResult> Suggest()
        {
            try
            {
                return Json(await docodo.getBaseIndex().GetSuggessions(HttpContext.Request.Query["q"]));
            }
            catch (Exception e)
            {

            }

            return Json(new { Error = "No parameter"});
        }



    }
}