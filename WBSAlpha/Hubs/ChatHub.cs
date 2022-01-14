using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Data;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           14-01-2022
*/
namespace WBSAlpha.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logging;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<CoreUser> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<CoreUser> userManager, ILogger<ChatHub> logger)
        {
            _dbContext = context;
            _userManager = userManager;
            _logging = logger;
        }
        
        public override async Task OnConnectedAsync()
        {
            await UpdateChats();
            await UpdateUserList();
            await Clients.Caller.SendAsync("ReceiveMessage", new 
            { 
                name = "System", 
                message = "Welcome!",
                time = DateTime.Now.ToShortDateString(),
                mId = -1,
            });
            ConnectionManager.Ids.Add(Context.UserIdentifier);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception) 
        {
            ConnectionManager.Ids.Remove(Context.UserIdentifier);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Attempt to send a message to the given chatroom.
        /// </summary>
        /// <param name="roomNumber">ID of the room to send a message into.</param>
        /// <param name="message">Message to send to the chat.</param>
        public async Task SendMessage(int roomNumber, string message)
        {
            if (!message.Equals(""))
            {
                int id = 0;
                string userName = "Empty";
                string chatroom = "Default";
                DateTime mTime = DateTime.Now;
                string m = message;
                try
                {
                    CoreUser person = await _dbContext.Users.FindAsync(Context.UserIdentifier);
                    Chatroom room = await _dbContext.Chatrooms.FindAsync(roomNumber);
                    if (person != null && room != null)
                    {
                        chatroom = room.ChatName;
                        userName = person.UserName;
                        Message sent = new();
                        sent.Content = message;
                        sent.Timestamp = mTime;
                        sent.SentFromUser = Context.UserIdentifier;
                        id = sent.MessageID;
                        sent.ChatID = roomNumber;
                        _dbContext.Add(sent);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logging.LogInformation($"Failed to send message to room {roomNumber} @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
                }
                await Clients.Group(chatroom).SendAsync("ReceiveMessage", new
                {
                    name = userName,
                    time = mTime.ToShortTimeString(),
                    message = m,
                    mID = id
                });
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    name = userName,
                    time = mTime.ToShortTimeString(),
                    message = m,
                    mID = id
                });
            }
        }

        /// <summary>
        /// Attempt to create a report for the given message.
        /// </summary>
        /// <param name="id">ID of message to report.</param>
        /// <param name="reason">Justification for report.</param>
        public async Task ReportMessage(int id, string reason)
        {
            try
            {
                Message reported = await _dbContext.Messages.FindAsync(id);
                if (reported != null)
                {
                    Report reportOut = new();
                    reportOut.Reason = reason;
                    // I could have it be the ID parameter but I want to make sure it finds a result first
                    reportOut.MessageID = reported.MessageID;
                    _dbContext.Reports.Add(reportOut);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logging.LogInformation($"Failed to report message {id} @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
            }
        }

        /// <summary>
        /// Attempt to send a message privately -- from one user to another, in their own room.
        /// </summary>
        /// <param name="toUser">Standing of the user in question.</param>
        /// <param name="messageContents">Message to send privately.</param>
        public async Task SendPrivateMessage(int toUser, string messageContents)
        {
            if (!messageContents.Equals(""))
            {
                int messageID = -1; // the ID of this new message, if applicable
                string from = "Empty";
                int fKey = -1; // standingID of this user
                string to = "Empty2";
                DateTime sentAt = DateTime.Now;
                int outKey = toUser; // this key should remain consistent when these two users are chatting
                try
                {
                    CoreUser fUser = await _dbContext.Users.FindAsync(Context.UserIdentifier);
                    CoreUser tUser = await _dbContext.Users.FirstAsync(u => u.StandingID == toUser);
                    from = fUser.UserName;
                    fKey = fUser.StandingID;
                    to = tUser.Id;

                    Message sent = new();
                    sent.Content = messageContents;
                    sent.Timestamp = sentAt;
                    sent.SentFromUser = Context.UserIdentifier;
                    sent.SentToUser = to;
                    messageID = sent.MessageID;

                    _dbContext.Messages.Add(sent);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logging.LogInformation($"Failed to send private message @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
                }
                // send the message to just the two users
                if (messageID > -1)
                {
                    await Clients.User(Context.UserIdentifier).SendAsync("ReceivePrivateMessage", new {
                        name = from, time = sentAt.ToShortTimeString(), message = messageContents, mId = messageID, key = outKey
                    });
                    await Clients.User(to).SendAsync("ReceivePrivateMessage", new {
                        name = from, time = sentAt.ToShortTimeString(), message = messageContents, mId = messageID, key = outKey
                    });
                }
            }
        }

        /// <summary>
        /// Allows the user to swap what chatroom they are in at a given time.
        /// </summary>
        /// <param name="join">ID of the chatroom the user is entering.</param>
        /// <param name="leave">ID of the chatroom the user is leaving.</param>
        /// <param name="isPrivate">Whether the chat the user is entering is private.</param>
        /// <param name="wasPrivate">Whether the chat the user is leaving was private.</param>
        public async Task ChangeChats(int join, int leave, bool isPrivate, bool wasPrivate)
        {
            try
            {
                if (isPrivate)
                {
                    CoreUser myself = await _dbContext.Users.FindAsync(Context.UserIdentifier);
                    CoreUser other = await _dbContext.Users.FirstOrDefaultAsync(u => u.StandingID == join);

                    if (!wasPrivate)
                    {
                        Chatroom exit = _dbContext.Chatrooms.FirstOrDefault(c => c.ChatID == leave);
                        if (exit != null)
                        {
                            await Groups.RemoveFromGroupAsync(Context.UserIdentifier, exit.ChatName);
                        }
                    }

                    DateTime _today = DateTime.Today;
                    List<Message> sinceLogin = _dbContext.Messages.Where(m => m.SentFromUser == Context.UserIdentifier
                            || m.SentFromUser == other.Id).Where(m => m.Timestamp >= _today).ToList();
                    CoreUser user;
                    foreach (Message m in sinceLogin)
                    {
                        user = await _dbContext.Users.FindAsync(m.SentFromUser);
                        await Clients.Caller.SendAsync("ReceivePrivateMessage", new {
                            name = user.UserName,
                            time = m.Timestamp.ToShortTimeString(),
                            message = m.Content,
                            mId = m.MessageID,
                            key = join
                        });
                    }
                }
                else
                {
                    Chatroom chat = _dbContext.Chatrooms.FirstOrDefault(c => c.ChatID == join);
                    Chatroom exit = _dbContext.Chatrooms.FirstOrDefault(c => c.ChatID == leave);
                    if (chat != null && exit != null)
                    {
                        await Groups.AddToGroupAsync(Context.UserIdentifier, chat.ChatName);
                        await Groups.RemoveFromGroupAsync(Context.UserIdentifier, exit.ChatName);
                        await Clients.Caller.SendAsync("JoinRoom", new
                        {
                            id = chat.ChatID
                        });
                    }
                    // collect last 10 messages that were sent within the last hour, send to user
                    await SendMessageHistory(chat, 10);
                }
            }
            catch (Exception ex)
            {
                _logging.LogInformation($"User failed to change chatrooms between {leave}->{join} (private: {isPrivate}) @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
            }
        }

        /// <summary>
        /// This is responsible for keeping the user up-to-date on the last messages sent in the chatroom so that
        /// they don't feel lost when joining a conversation/new room.
        /// </summary>
        /// <param name="chat">Chatroom the user is in.</param>
        /// <param name="x">Number of messages to send.</param>
        private async Task SendMessageHistory(Chatroom chat, int x)
        {
            // collect last 10 messages that were sent within the last hour, send to user
            DateTime _lastHour = DateTime.Now.AddHours(-1);
            Console.WriteLine($"{DateTime.Now.ToShortTimeString()} - finding messages since {_lastHour.ToShortTimeString()}");
            List<Message> sinceLogin = await _dbContext.Messages.Where(m => m.ChatID == chat.ChatID)
                .Where(m => m.Timestamp >= _lastHour).OrderBy(m => m.Timestamp).Take(x).ToListAsync();
            Console.WriteLine($"There are {sinceLogin.Count} messages to send to the user.");
            CoreUser user;
            foreach (Message m in sinceLogin)
            {
                user = await _dbContext.Users.FindAsync(m.SentFromUser);
                Console.WriteLine($"{user.UserName} ({m.Timestamp.ToShortTimeString()}): {m.Content}");
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    name = user.UserName,
                    time = m.Timestamp.ToShortTimeString(),
                    message = m.Content,
                    mId = m.MessageID
                });
            }
        }

        /// <summary>
        /// Called when the user enters chat to populate a list of available chat rooms 
        /// for the newly joined user.
        /// </summary>
        private async Task UpdateChats()
        {
            List<Chatroom> chats = await _dbContext.Chatrooms.ToListAsync();
            foreach (Chatroom chat in chats)
            {
                await Clients.Caller.SendAsync("AddChatroom", new
                {
                    id = chat.ChatID,
                    name = chat.ChatName
                });
            }
            Chatroom chatroom = chats.First();
            await Groups.AddToGroupAsync(Context.UserIdentifier, chatroom.ChatName);
            await Clients.Caller.SendAsync("JoinRoom", new
            {
                id = chatroom.ChatID
            });
            await SendMessageHistory(chatroom, 10);
        }

        /// <summary>
        /// When a user enters the chatroom, add them to every user's client.
        /// </summary>
        private async Task UpdateUserList()
        {
            try
            {
                CoreUser newUser = await _dbContext.Users.FindAsync(Context.UserIdentifier);
                await Clients.AllExcept(Context.ConnectionId).SendAsync("AddNewUser", new
                {
                    id = newUser.StandingID,
                    name = newUser.UserName,
                    special = (Context.User.IsInRole("Moderator") || Context.User.IsInRole("Admin"))
                });
                CoreUser other;
                foreach (string id in ConnectionManager.Ids)
                {
                    other = await _dbContext.Users.FindAsync(id);
                    if (other != null)
                    {
                        await Clients.Caller.SendAsync("AddNewUser", new
                        {
                            id = other.StandingID,
                            name = other.UserName,
                            special = (await _userManager.IsInRoleAsync(other, "Moderator") || await _userManager.IsInRoleAsync(other, "Administrator"))
                        });
                    }
                }
            } 
            catch (Exception ex)
            {
                _logging.LogInformation($"Update User List failed @ {DateTime.Now.ToLongTimeString()} - {ex.Message}");
            }
        }

        /// <summary>
        /// A simple way of keeping track of active chat users.
        /// </summary>
        private static class ConnectionManager
        {
            public static HashSet<string> Ids = new HashSet<string>();
        }
    }
}