using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Data;
using WBSAlpha.Hubs;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           29-11-2021
*/
namespace WBSAlpha.Controllers
{
    [Authorize(Roles = "Moderator,Administrator")]
    public class ModerationController : Controller
    {
        private readonly UserManager<CoreUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private ChatHub _activeChat;

        [BindProperty]
        public ChatInput ChatroomInput { get; set; }


        public ModerationController(UserManager<CoreUser> manager, ApplicationDbContext context, ChatHub hub)
        {
            _userManager = manager;
            _dbContext = context;
            _activeChat = hub;
        }

        // Get: ModerationController
        public ActionResult Index()
        {
            // force it to default to the metrics view
            return View("Metrics");
        }

        // GET: ModerationController/Metrics
        public ActionResult Metrics()
        {
            return View();
        }

        // manage reports
        public async Task<ActionResult> Reports()
        {
            Report[] reports = _dbContext.Reports.Where(r => r.RespondedTo == false)
                .OrderBy(r => r.MessageID).ToArray();
            // filter messages by those with a report
            Message[] list = _dbContext.Messages.Where(m => reports.Any(r => r.MessageID == m.MessageID)).ToArray();
            string[] messages = new string[list.Length];
            string[] userNames = new string[list.Length];
            string[] userIds = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                messages[i] = list[i].Content;
                userIds[i] = list[i].SentFromUser;
            }
            for (int i = 0; i < userIds.Length; i++)
            {
                userNames[i] = _dbContext.Users.FindAsync(userIds[i]).Result.UserName;
            }
            ViewData["ActiveReports"] = reports;
            ViewData["RudeMessages"] = messages;
            ViewData["ReportedNames"] = userNames;
            return View("Reports");
        }

        public async Task<ActionResult> IgnoreReport(int id)
        {
            Report report = await _dbContext.Reports.FindAsync(id);
            report.RespondedTo = true;
            _dbContext.Reports.Update(report);
            await _dbContext.SaveChangesAsync();
            return View("Reports");
        }
        /// <summary>
        /// Kicks the user from chat and keeps them from rejoining for a set amount of time.
        /// </summary>
        /// <param name="id">Report ID responsible for kick.</param>
        public async Task<ActionResult> KickUser(int id)
        {
            // this is less clean than the banning user because it is necessary that this be tied to the report
            Report report = await _dbContext.Reports.FindAsync(id);
            Message badMessage = await _dbContext.Messages.FindAsync(report.MessageID);
            CoreUser rude = await _dbContext.Users.FindAsync(badMessage.SentFromUser);
            if (rude != null)
            {
                DateTime currentTime = DateTime.Now;
                Standing standing = await _dbContext.Standings.FindAsync(rude.StandingID);
                if (standing.KickCount == 0)
                {
                    standing.KickEnds = currentTime.AddMinutes(5);
                }
                else if (standing.KickCount == 1)
                {
                    standing.KickEnds = currentTime.AddMinutes(30);
                }
                else
                {
                    standing.KickEnds = currentTime.AddHours(12);
                }
                standing.KickCount += 1;
                standing.KickTotal += 1;
                report.RespondedTo = true;
                _dbContext.Standings.Update(standing);
                _dbContext.Reports.Update(report);
                await _dbContext.SaveChangesAsync();
                await _activeChat.DisconnectUser(rude.Id);
            }
            return View("Reports");
        }

        // manage chat history

        public ActionResult ChatHistory()
        {
            return View();
        }

        // manage user bans
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageBans()
        {
            // get list of standings with 3 or more kicks
            Standing[] rudeness = _dbContext.Standings.Where(s => s.KickCount >= 3).ToArray();
            // get the users that match up with those standing IDs
            CoreUser[] rudeUsers = _dbContext.Users.Where(u => rudeness.Any(r => r.StandingID == u.StandingID)).ToArray();
            // get all responded to reports to aid with filtering
            List<Report> reports = _dbContext.Reports.Where(r => r.RespondedTo == true).ToList();
            // get all messages from the rude users, grouped by user, select the most recent only, where reports share the distinct message ID
            List<Message> messages = _dbContext.Messages.Where(m => rudeUsers.Any(r => r.Id == m.SentFromUser))
                .GroupBy(u => u.SentFromUser).Select(g => g.OrderByDescending(t => t.Timestamp).First())
                .Where(m => reports.Any(r => r.MessageID == m.MessageID)).ToList();
            
            List<string> organized = new List<string>(messages.Count);
            List<string> reasons = new List<string>(messages.Count);
            Report outReport;
            Message outMessage;
            string outString;
            foreach (CoreUser user in rudeUsers)
            {
                outMessage = messages.FirstOrDefault(m => m.SentFromUser == user.Id);
                outReport = reports.FirstOrDefault(r => r.MessageID == outMessage.MessageID);
                if (outMessage != null && outReport != null)
                {
                    organized.Add(outMessage.Content);
                    reasons.Add(outReport.Reason);
                }
            }
            reasons.TrimExcess();
            organized.TrimExcess();

            ViewData["RudeUsers"] = rudeUsers;
            ViewData["Reasons"] = reasons.ToArray();
            ViewData["RudeMessages"] = organized.ToArray();
            return View();
        }
        /// <summary>
        /// Attempts to ban the user for a varied length of time depending on number of offenses.
        /// </summary>
        /// <param name="id">Id of User to Ban.</param>
        public async Task<ActionResult> BanUser(string id)
        {
            CoreUser rude = await _dbContext.Users.FindAsync(id);
            if (rude != null)
            {
                DateTime currentTime = DateTime.Now;
                Standing standing = await _dbContext.Standings.FindAsync(rude.StandingID);
                if (standing.BanCount == 0)
                {
                    standing.BanEnds = currentTime.AddDays(1); // first offense, punish weakly
                } 
                else if (standing.BanCount == 1)
                {
                    standing.BanEnds = currentTime.AddDays(7); // second offense, punish weekly
                }
                else
                {
                    if (standing.BanCount > 4)
                    {
                        standing.BanEnds = currentTime.AddYears(1); // user is so egregious they need to stay away
                    } else
                    {
                        standing.BanEnds = currentTime.AddMonths(1); // user is banned for a month
                    }
                }
                standing.BanCount += 1;
                standing.BanTotal += 1;
                _dbContext.Standings.Update(standing);
                await _dbContext.SaveChangesAsync();
                await _activeChat.DisconnectUser(id);
            }
            return View();
        }

        // manage moderators
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> ManageModerators()
        {
            CoreUser[] users = _dbContext.Users.ToArray();
            bool[] isMod = new bool[users.Length];
            for (int i = 0; i < users.Length; i++)
            {
                isMod[i] = await _userManager.IsInRoleAsync(users[i], "Moderator");
            }
            ViewData["IsAMod"] = isMod;
            ViewData["Users"] = users;
            ViewData["Standing"] = _dbContext.Standings.ToArray();
            return View();
        }
        /// <summary>
        /// Attempts to promote the user to moderator if they are not already a moderator.
        /// </summary>
        /// <param name="id">ID of user to promote.</param>
        public async Task<ActionResult> PromoteUser(string id)
        {
            var promotedUser = await _userManager.FindByIdAsync(id);
            if (promotedUser != null)
            {
                // there should be no reason why the user is null, but in case...
                await _userManager.AddToRoleAsync(promotedUser, "Moderator");
            }
            ViewBag.Promoted = await _userManager.IsInRoleAsync(promotedUser, "Moderator");
            ViewBag.UserName = promotedUser.UserName;
            return View();
        }
        /// <summary>
        /// Attempts to demote a given user from moderator if they are a moderator.
        /// </summary>
        /// <param name="id"></param>
        public async Task<ActionResult> DemoteUser(string id)
        {
            var demotedUser = await _userManager.FindByIdAsync(id);
            if (demotedUser != null)
            {
                // there should be no reason why the user is null, but in case...
                await _userManager.RemoveFromRoleAsync(demotedUser, "Moderator");
            }
            ViewBag.Demoted = await _userManager.IsInRoleAsync(demotedUser, "Moderator");
            ViewBag.UserName = demotedUser.UserName;
            return View();
        }

        // manage chatrooms
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageChatrooms()
        {
            return View();
        }
        /// <summary>
        /// Attempt to add a chatroom with the given information.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddChatroom()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("ManageChatrooms");
                }
                Chatroom newChat = new Chatroom();
                newChat.ChatName = ChatroomInput.Name;
                if (ChatroomInput.Description != null)
                {
                    newChat.Description = ChatroomInput.Description;
                }
                _dbContext.Chatrooms.Add(newChat);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("ManageChatrooms");
            }
            catch
            {
                return View("ManageChatrooms");
            }
        }
        /// <summary>
        /// Delete the given chatroom based on provided ID.
        /// </summary>
        /// <param name="id">Id of chatroom to delete.</param>
        public async Task<ActionResult> DeleteChatroom(int id)
        {
            try
            {
                Chatroom chat = await _dbContext.Chatrooms.FindAsync(id);
                _dbContext.Chatrooms.Remove(chat);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("ManageChatrooms");
            }
            catch
            {
                return View("ManageChatrooms");
            }
        }
        public class ChatInput
        {
            [Required]
            [DataType(DataType.Text)]
            [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
            [Display(Name = "Chat Name")]
            public string Name { get; set; }
            [DataType(DataType.Text)]
            [StringLength(90, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [Display(Name = "Description")]
            public string? Description { get; set; }
        }

        // manage builds -- other build methods are in games controller
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageBuilds()
        {
            return View();
        }

        // POST: ModerationController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ModerationController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ModerationController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ModerationController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ModerationController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
