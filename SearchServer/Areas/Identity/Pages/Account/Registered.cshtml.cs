using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SearchServer.Areas.Identity.Pages.Account
{
    public class RegisteredModel : PageModel
    {
        public string returnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            this.returnUrl = returnUrl;
         
        }
    }
}