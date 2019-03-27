using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchServer.Models;

namespace SearchServer.Areas.Identity.Pages.Account.Manage
{
    public class SendEmailConfirmationModel : PageModel
    {
        UserManager<User> _mngr;
        IEmailSender _emailSender;

        public SendEmailConfirmationModel(UserManager<User> mngr, IEmailSender emailSender) : base()
        {
            _mngr = mngr;
            _emailSender = emailSender;
        }

        [Authorize]   
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _mngr.GetUserAsync(User);
            if (!user.EmailConfirmed)
            {
                var code = await _mngr.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Page(
                              "/Account/ConfirmEmail",
                              pageHandler: null,
                              values: new { userId = user.Id, code = code },
                              protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                               $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>CLICKING HERE</a>.");

            }
            else ViewData["Error"] = "Already Confirmed";
            return Page();
        }

    }
}