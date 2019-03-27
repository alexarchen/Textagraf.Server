using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Docodo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SearchServer.Models;

namespace SearchServer.Controllers
{
    public class SearcherController : Controller
    {
       private IDocodoService docodo;
       private SignInManager<User> signInManager;
       private UserManager<User> userManager;


       public SearcherController(IDocodoService docodoService, SignInManager<User> mgr): base()
        {
            docodo = docodoService;
            signInManager = mgr;
            userManager = mgr.UserManager;
        }

        Index getIndexForUser()
        {

            ViewBag.UserName = "";

            Index index = null;
            if (signInManager.IsSignedIn(User))
            {
                ViewBag.UserName = userManager.GetUserName(User);// User.Claims.Where((i) => { return (ClaimTypes.Name.Equals(i.Type)); }).First().Value;
                index = docodo.getIndex(ViewBag.UserName);
            }

            return index;

        }
        Index createIndexForUser()
        {

            ViewBag.UserName = "";

            Index index = null;
            if (signInManager.IsSignedIn(User))
            {
                ViewBag.UserName = userManager.GetUserName(User);// User.Claims.Where((i) => { return (ClaimTypes.Name.Equals(i.Type)); }).First().Value;
                index = docodo.createIndex(ViewBag.UserName,new string[] { "ru","en"});
            }

            return index;

        }

        public IActionResult Index()
        {
            if (!signInManager.IsSignedIn(User))
                return new RedirectResult("Try");

             Index index = getIndexForUser();

              ViewResult res = View(new SessionIndexModel(index));
              ViewData.Add("Docodo", docodo.getVersion());

              return res;
        }


        struct IndexStatus
        {
            public IndexStatus(bool cs,Index.Status s)
            {
                canSearch = cs;
                status = s;
            }
            public bool canSearch;
            public Index.Status status;
        }

        public IActionResult GetState()
        {

            Index index = getIndexForUser();
            return Json(new IndexStatus(index.CanSearch,index.status));
        }

        [Authorize]
        public IActionResult New()
        {
            Index index = getIndexForUser();
            ViewData.Add("Docodo", docodo.getVersion());
            ViewBag.Languages=  docodo.getLanguages();
            ViewResult res = View(new SessionIndexModel(index));

            return res;
        }

        [Authorize]
        [HttpPost]
        public IActionResult New(SessionIndexModel model)
        {

            Index index = createIndexForUser();
            
            ViewData.Add("Docodo", docodo.getVersion());
            ViewResult res = View(new SessionIndexModel(index));
            ViewBag.Languages = docodo.getLanguages();

            switch (model.Type)
            {
                case SessionIndexModel.TypeEnum.Web:

                    WebDataSource web = new WebDataSource("web", model.Path);
                    web.MaxItems = 20;
                    index.AddDataSource(web);
                    index.CreateAsync();
                    break;
                default:
                    ViewData.Add("Error", $"Data Source {model.Type} is not supported");
                    break;

            }

            
           return res;
        }

        [HttpGet]
        public IActionResult Search(string Id,string q)
        {
            Index ind = docodo.getIndex(Id);
            if (ind == null)
            {
                return Json(new { Error = "No index" });
            }
           return Json(ind.Search(q));
           
        }

        public IActionResult Try()
        {
            ViewBag.UserName = "";
            return View();

        }

        // show search results
        [Authorize]
        [HttpGet]
        public IActionResult Show(string q)
        {
            ViewBag.UserName = userManager.GetUserName(User);
            return View();
        }


    }
}