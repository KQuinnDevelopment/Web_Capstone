using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Data;
using WBSAlpha.Hubs;
using WBSAlpha.Models;
using WBSAlpha.ViewModels;
/*
Modified By:    Quinn Helm
Date:           11-01-2022
*/
namespace WBSAlpha.Controllers
{
    [Authorize(Roles = "Moderator,Administrator")]
    public class ModerationController : Controller
    {
        private readonly ILogger<ModerationController> _logging;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly UserManager<CoreUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        [BindProperty]
        public ChatInput ChatroomInput { get; set; }

        [BindProperty]
        public SearchInput ChatSearch { get; set; }

        public ModerationController(UserManager<CoreUser> manager, ApplicationDbContext context, ILogger<ModerationController> logger, IHubContext<ChatHub> hub)
        {
            _userManager = manager;
            _logging = logger;
            _dbContext = context;
            _chatHub = hub;
        }

        /// <summary>
        /// In the event that the index method is called, default to the Reports view.
        /// </summary>
        public ActionResult Index()
        {
            // force it to default to the Reports view
            return View("Reports");
        }

        /// <summary>
        /// Allow the moderator+ to view available metrics.
        /// </summary>
        public async Task<ActionResult> Metrics()
        {
            ViewData["Title"] = "Metrics";
            DateTime current = DateTime.Now;
            DateTime lastPeriod = current.AddDays(-1);
            List<Message> messages = await _dbContext.Messages.Where(m => m.Timestamp >= lastPeriod).ToListAsync();
            int reports = 0;
            int userIDs = 0;
            if (messages != null)
            {
                userIDs = messages.Select(m => m.SentFromUser).Distinct().Count();
                foreach (Message m in messages)
                {
                    reports += await _dbContext.Reports.Where(r => r.MessageID == m.MessageID).CountAsync();
                }
            }
            ViewData["UserCount"] = userIDs;
            ViewData["MessageCount"] = (messages != null) ? messages.Count : 0;
            ViewData["ReportsCount"] = reports;
            return View();
        }

        // manage reports
        /// <summary>
        /// Returns a view that allows moderators+ to see a list of reports that have yet to be responded to.
        /// </summary>
        public async Task<ActionResult> Reports()
        {
            List<Report> r = await _dbContext.Reports.OrderBy(r => r.MessageID).Where(r => r.RespondedTo == false).ToListAsync();
            List<Message> m = new(r.Count);
            foreach (Report result in r)
            {
                m.Add(await _dbContext.Messages.FirstOrDefaultAsync(m => m.MessageID == result.MessageID));
            }
            m.TrimExcess(); // just in case?
            // filter messages by those with a report
            Message[] list = m.ToArray();
            Report[] reports = r.ToArray();
            string[] messages = new string[list.Length];
            string[] userNames = new string[list.Length];
            string[] userIds = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                messages[i] = list[i].Content;
                userIds[i] = list[i].SentFromUser;
            }
            CoreUser u;
            for (int i = 0; i < userIds.Length; i++)
            {
                u = await _dbContext.Users.FindAsync(userIds[i]);
                userNames[i] = u.UserName;
            }
            ViewData["Title"] = "Manage Chat Reports";
            ViewData["ActiveReports"] = reports;
            ViewData["RudeMessages"] = messages;
            ViewData["ReportedNames"] = userNames;
            return View();
        }

        /// <summary>
        /// If this report is not justified, acknowledge its existence but do not punish
        /// the user who sent the offending message and mark it as responded to.
        /// </summary>
        /// <param name="id">ID of Report to ignore.</param>
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
                standing.Justification = id;
                report.RespondedTo = true;
                _dbContext.Standings.Update(standing);
                _dbContext.Reports.Update(report);
                await _dbContext.SaveChangesAsync();
                try
                {
                    await _chatHub.Clients.User(rude.Id).SendAsync("Disconnect");
                }
                catch (Exception ex)
                {
                    _logging.LogInformation($"Failed to disconnect {rude.UserName} from chat @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
                }
            }
            return RedirectToAction("Reports");
        }

        // manage chat history
        /// <summary>
        /// Allows a moderator+ to view the last 200 messages from the first available chatroom.
        /// This is called the first time a user looks at the chat history.
        /// </summary>
        public async Task<ActionResult> ChatHistory()
        {
            List<Chatroom> rooms = _dbContext.Chatrooms.ToList();
            int id = (rooms.Count > 0) ? rooms.First().ChatID : -1;
            List<Message> messages = (rooms.Count > 0) ? new(100) : new(0);
            string[] userNames = (rooms.Count > 0) ? new string[100] : Array.Empty<string>();
            if (id > -1)
            {
                messages = await _dbContext.Messages.Where(m => m.ChatID == id).ToListAsync();
                userNames = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    CoreUser u = await _dbContext.Users.FindAsync(messages[i].SentFromUser);
                    userNames[i] = u.UserName;
                }
            }
            ViewData["Title"] = "View Chat History";
            ViewData["Chats"] = rooms;
            ViewData["Messages"] = messages.ToArray();
            ViewData["UserNames"] = userNames;
            return View();
        }

        /// <summary>
        /// Allows a moderator+ to view the last 200 messages from a given chatroom.
        /// </summary>
        /// <param name="id">Chatroom ID to filter to.</param>
        public async Task<ActionResult> ChatroomHistory(int id)
        {
            List<Chatroom> rooms = _dbContext.Chatrooms.ToList();
            List<Message> messages = await _dbContext.Messages.Where(m => m.ChatID == id)
                .OrderByDescending(m => m.Timestamp).Take(200).ToListAsync();
            string[] userNames = new string[messages.Count];
            for (int i = 0; i < messages.Count; i++)
            {
                CoreUser u = await _dbContext.Users.FindAsync(messages[i].SentFromUser);
                userNames[i] = u.UserName;
            }
            List<Chatroom> outRooms = (rooms != null) ? rooms : new(0);
            ViewData["Title"] = "View Chat History";
            ViewData["Chats"] = outRooms;
            ViewData["Messages"] = messages.ToArray();
            ViewData["UserNames"] = userNames;
            return View("ChatHistory");
        }

        /// <summary>
        /// Search the given chatroom for messages that fit the desired filter.
        /// </summary>
        /// <param name="id">Chat ID to filter into.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SearchHistory(int id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("ChatHistory");
                }
                List<Message> messages;
                if (ChatSearch.On != null)
                {
                    DateTime check = new(ChatSearch.On.Value.Year,
                        ChatSearch.On.Value.Month, ChatSearch.On.Value.Day);
                    // ensure this gets priority over any other search type
                    ChatSearch.Before = null;
                    ChatSearch.After = null;
                    messages = _dbContext.Messages.Where(m => m.ChatID == id)
                        .Where(m => m.Timestamp == check).ToList();
                }
                else
                {
                    if (ChatSearch.Before != null && ChatSearch.After != null)
                    {
                        DateTime checkAfter = new(ChatSearch.After.Value.Year, ChatSearch.After.Value.Month, ChatSearch.After.Value.Day);
                        DateTime checkBefore = new(ChatSearch.Before.Value.Year,
                        ChatSearch.Before.Value.Month, ChatSearch.Before.Value.Day);

                        messages = _dbContext.Messages.Where(m => m.ChatID == id)
                        .Where(m => m.Timestamp >= checkAfter).Where(m => m.Timestamp <= checkBefore).ToList();
                    }
                    else if (ChatSearch.After != null)
                    {
                        DateTime checkAfter = new(ChatSearch.After.Value.Year,
                        ChatSearch.After.Value.Month, ChatSearch.After.Value.Day);

                        messages = _dbContext.Messages.Where(m => m.ChatID == id)
                        .Where(m => m.Timestamp >= checkAfter).ToList();
                    }
                    else if (ChatSearch.Before != null)
                    {
                        DateTime checkBefore = new(ChatSearch.Before.Value.Year,
                        ChatSearch.Before.Value.Month, ChatSearch.Before.Value.Day);

                        messages = _dbContext.Messages.Where(m => m.ChatID == id)
                        .Where(m => m.Timestamp <= checkBefore).ToList();
                    }
                    else
                    {
                        messages = _dbContext.Messages.Where(m => m.ChatID == id).TakeLast(200).ToList();
                    }
                }
                if (messages == null)
                {
                    messages = new List<Message>(0);
                }
                else
                {
                    messages.TrimExcess(); // just in case
                }
                List<Chatroom> rooms = _dbContext.Chatrooms.ToList();
                string[] userNames = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    CoreUser u = await _dbContext.Users.FindAsync(messages[i].SentFromUser);
                    userNames[i] = u.UserName;
                }
                ViewData["Title"] = "View Chat History";
                ViewData["Chats"] = rooms;
                ViewData["Messages"] = messages.ToArray();
                ViewData["UserNames"] = userNames;
                return RedirectToAction("ChatHistory");
            }
            catch
            {
                return View("ChatHistory");
            }
        }

        /// <summary>
        /// DateTimes to search for and between, as necessary for the chat history feature.
        /// </summary>
        public class SearchInput
        {
            [DataType(DataType.Date)]
            [Display(Name = "On Date")]
            public DateTime? On { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Before Date")]
            public DateTime? Before { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "After Date")]
            public DateTime? After { get; set; }
        }

        // manage user bans
        /// <summary>
        /// Provides a view to an administrator of a list of users worthy of being banned from chat.
        /// </summary>
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> ManageBans()
        {
            // get list of standings with 3 or more kicks
            List<Standing> rudeness = await _dbContext.Standings.Where(s => s.KickCount >= 3).ToListAsync();
            List<Report> reports = (rudeness != null) ? new(rudeness.Count) : new(0);
            UserBanViewModel userBans = new();
            userBans.RudeUsers = (rudeness != null) ? new(rudeness.Count) : new(0);
            userBans.RudeMessages = (rudeness != null) ? new(rudeness.Count) : new(0);
            userBans.Reasons = (rudeness != null) ? new(rudeness.Count) : new(0);
            // get the users & reports that match up with those standing IDs
            foreach (Standing s in rudeness)
            {
                userBans.RudeUsers.Add(await _dbContext.Users.FirstOrDefaultAsync(u => u.StandingID == s.StandingID));
                reports.Add(await _dbContext.Reports.FirstOrDefaultAsync(r => r.ReportID == s.Justification));
            }
            Message m;
            foreach (Report r in reports)
            {
                m = await _dbContext.Messages.FirstOrDefaultAsync(msg => msg.MessageID == r.MessageID);
                userBans.RudeMessages.Add(m.Content);
                userBans.Reasons.Add(r.Reason);
            }
            return View(userBans);
        }

        /// <summary>
        /// Attempts to ban the user for a varied length of time depending on number of offenses.
        /// This will also remove the user from chat if they are currently present in it.
        /// </summary>
        /// <param name="id">Id of User to Ban.</param>
        [Authorize(Roles = "Administrator")]
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
                    }
                    else
                    {
                        standing.BanEnds = currentTime.AddMonths(1); // user is banned for a month
                    }
                }
                standing.BanCount += 1;
                standing.BanTotal += 1;
                _dbContext.Standings.Update(standing);
                await _dbContext.SaveChangesAsync();
                try
                {
                    await _chatHub.Clients.User(rude.Id).SendAsync("Disconnect");
                }
                catch (Exception ex)
                {
                    _logging.LogInformation($"Failed to disconnect {rude.UserName} from chat @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
                }
            }
            return RedirectToAction("ManageBans");
        }

        // manage moderators
        /// <summary>
        /// Provides a view to an administrator that allows them to promote and demote users
        /// to and from the Moderator role. Moderators will continue to have moderation
        /// privileges until their cookie expires.
        /// </summary>
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> ManageModerators()
        {
            CoreUser[] users = _dbContext.Users.ToArray();
            bool[] isMod = new bool[users.Length];
            for (int i = 0; i < users.Length; i++)
            {
                isMod[i] = await _userManager.IsInRoleAsync(users[i], "Moderator");
            }
            ViewData["IsMod"] = isMod;
            ViewData["Users"] = users;
            ViewData["Standing"] = _dbContext.Standings.ToArray();
            return View();
        }

        /// <summary>
        /// Attempts to promote the user to moderator if they are not already a moderator.
        /// </summary>
        /// <param name="id">ID of user to promote.</param>
        [Authorize(Roles = "Administrator")]
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
            return RedirectToAction("ManageModerators");
        }

        /// <summary>
        /// Attempts to demote a given user from moderator if they are a moderator.
        /// </summary>
        /// <param name="id">ID of user to demote from moderator role.</param>
        [Authorize(Roles = "Administrator")]
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
            return RedirectToAction("ManageModerators");
        }

        /// <summary>
        /// Attempts to view the profile of a user tied to a given id.
        /// </summary>
        /// <param name="view">The view that called this action.</param>
        /// <param name="id">ID of user to view the profile of.</param>
        public async Task<ActionResult> UserProfileView(string id, string view)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                ProfileViewModel uProfile = new();
                uProfile.TargetUser = user;
                uProfile.UserStanding = await _dbContext.Standings.FindAsync(user.StandingID);
                uProfile.UserAge = user.Age.ToShortDateString(); // better formatting
                uProfile.IsAModerator = await _userManager.IsInRoleAsync(user, "Moderator");
                if (uProfile.UserStanding != null)
                {
                    ViewData["CalledFrom"] = view;
                    return View(uProfile);
                }
            }
            return RedirectToAction(view);
        }

        // manage chatrooms
        /// <summary>
        /// Provides a view to an administrator that allows them to add or remove a chatroom.
        /// </summary>
        [Authorize(Roles = "Administrator")]
        public ActionResult ManageChatrooms()
        {
            ViewData["Chatrooms"] = _dbContext.Chatrooms.ToList();
            ChatInput chatIn = new();
            return View(chatIn);
        }

        /// <summary>
        /// Attempt to add a chatroom with the given information.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> AddChatroom()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return RedirectToAction("ManageChatrooms");
                }
                Chatroom newChat = new();
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
        [Authorize(Roles = "Administrator")]
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

        /// <summary>
        /// Used by administrators to create chatrooms by setting the relevant properties.
        /// </summary>
        public class ChatInput
        {
            [Required]
            [DataType(DataType.Text)]
            [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 4)]
            [Display(Name = "Chat Name")]
            public string Name { get; set; }

            [DataType(DataType.Text)]
            [StringLength(90, ErrorMessage = "The {0} must be at most {1} characters long.")]
            [Display(Name = "Description")]
            public string Description { get; set; }
        }
    }
}