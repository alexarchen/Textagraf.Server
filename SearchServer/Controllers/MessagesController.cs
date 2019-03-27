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
    public class MessagesController : Controller
    {
        public static async Task PostMessageAsync(UserContext context,Message msg)
        {
            context.Message.Add(msg);
            await context.SaveChangesAsync();
        }

        UserContext _context;
        UserManager<User> _umngr;
        public MessagesController(UserContext context, UserManager<User> umngr)
        {
            _context = context;
            _umngr = umngr;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(_umngr.GetUserId(User));
            User user = await _context.User.Include(u => u.Messages).ThenInclude(m => m.fromUser).Include(u => u.Messages).ThenInclude(m=>m.group).FirstOrDefaultAsync(u => u.Id.Equals(userId));
            return View(user.Messages.Select(m=>new MessageModel(m)).ToList());
        }
        private object partAddUserLock = new object();

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete(int Id)
        {
            int userId = int.Parse(_umngr.GetUserId(User));
             User user = await _context.User.Include(u => u.Messages).ThenInclude(m => m.fromUser).FirstOrDefaultAsync(u => u.Id.Equals(userId));
            try
            {
                user.Messages.Remove(user.Messages.First(m => m.Id == Id && m.Type == Message.MessageType.Message));
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {

            }
            return View("Index",user.Messages.Select(m => new MessageModel(m)).ToList());
        }

        [Authorize]
        [HttpGet("/api/Messages")]
        [Produces("application/json")]
        public async Task<JsonResult> apiIndex()
        {
            int userId = int.Parse(_umngr.GetUserId(User));
            User user = await _context.User.Include(u => u.Messages).ThenInclude(m => m.fromUser).Include(u => u.Messages).ThenInclude(m => m.group).FirstOrDefaultAsync(u => u.Id.Equals(userId));
            return Json(user.Messages.Select(m => new MessageModel(m)).ToList());
        }


        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Accept(int Id, bool accept)
        {
            int userId = int.Parse(_umngr.GetUserId(User));
            User user = await _context.User.Include(u => u.Messages).ThenInclude(m => m.fromUser).Include(u => u.Messages).ThenInclude(m => m.group).FirstOrDefaultAsync(u => u.Id.Equals(userId));
            Message mess = user.Messages.FirstOrDefault(m => m.Id == Id);
            if (mess.fromUserId != null)
            {
                if (mess.Type == Message.MessageType.ParticipateGroup)
                {
                    // add participant
                    if ((mess.groupId != null) && (accept))
                    {
                        lock (partAddUserLock) {
                            if (!_context.GroupUser.Any(gu => (gu.GroupId == mess.groupId) && (gu.UserId.Equals(mess.fromUserId.Value))))
                                _context.GroupUser.Add(new GroupUser() { UserId = mess.fromUserId.Value, GroupId = mess.groupId.Value });
                        }
                    }
                    // remove message
                    user.Messages.Remove(mess);
                    // remove such messages
                    _context.Message.RemoveRange(_context.Message.Where(m=>m.fromUserId.Equals(mess.fromUserId) && m.groupId.Equals(mess.groupId) && m.Type.Equals(mess.Type)));
                    // notify user
                    PostMessageAsync(_context, new Message() { toUserId = mess.fromUserId.Value, fromUserId = userId, Type = accept?Message.MessageType.ParticipateGroupAccepted:Message.MessageType.ParticipateGroupDeclined, groupId = mess.groupId });
                    await _context.SaveChangesAsync();
                }
            }
            return View("Index",user.Messages.Select(m => new MessageModel(m)).ToList());
        }

    }
}