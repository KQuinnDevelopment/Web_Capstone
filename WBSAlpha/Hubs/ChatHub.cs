﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Data;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           01-12-2021
*/
namespace WBSAlpha.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;
        private DateTime _logonTime; // so that chatrooms can be populated according to login

        public ChatHub(ApplicationDbContext context)
        {
            _dbContext = context;
            _logonTime = DateTime.Now;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public async Task SpawnChats()
        {
            try
            {
                List<Chatroom> availableChats = _dbContext.Chatrooms.ToList();
                foreach (Chatroom room in availableChats)
                {
                    await Clients.User(Context.ConnectionId).SendAsync("AddChatroom", room.ChatID, room.ChatName);
                }
            } 
            catch (Exception ex)
            {
                // should log when errors occur but idk how
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public async Task DisconnectUser(string id)
        {
            try
            {
                await Clients.User(id).SendAsync("Disconnect");
            }
            catch (Exception ex)
            {
                // should log when errors occur but idk how
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task UpdateUserList()
        {
            try
            {
                CoreUser newUser = _dbContext.Users.FindAsync(Context.ConnectionId).Result;
                await Clients.All.SendAsync("AddNewUser", newUser.StandingID, 
                    newUser.NormalizedUserName, (Context.User.IsInRole("Moderator") || Context.User.IsInRole("Admin")));
            } 
            catch (Exception ex)
            {
                // should log when errors occur but idk how
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="join"></param>
        /// <param name="leave"></param>
        public async Task ChangeChats(int join, int leave, bool isPrivate, bool wasPrivate)
        {
            try
            {
                if (isPrivate)
                {
                    CoreUser myself = _dbContext.Users.FindAsync(Context.ConnectionId).Result;
                    CoreUser other = _dbContext.Users.FirstOrDefault(u => u.StandingID == join);

                    if (!wasPrivate)
                    {
                        Chatroom exit = _dbContext.Chatrooms.FirstOrDefault(c => c.ChatID == leave);
                        if (exit != null)
                        {
                            await Groups.RemoveFromGroupAsync(Context.ConnectionId, exit.ChatName);
                        }
                    }

                    List<Message> sinceLogin = _dbContext.Messages.Where(m => m.SentFromUser == Context.ConnectionId
                            || m.SentFromUser == other.Id).Where(m => m.Timestamp >= _logonTime).ToList();
                    string from = "";
                    foreach (Message m in sinceLogin)
                    {
                        from = _dbContext.Users.FindAsync(m.SentFromUser).Result.NormalizedUserName;
                        await Clients.User(Context.ConnectionId).SendAsync("ReceivePrivateMessage", from, m.Timestamp, m.Content, m.MessageID, join);
                    }
                } 
                else
                {
                    Chatroom chat = _dbContext.Chatrooms.FirstOrDefault(c => c.ChatID == join);
                    Chatroom exit = _dbContext.Chatrooms.FirstOrDefault(c => c.ChatID == leave);
                    if (chat != null && exit != null)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, chat.ChatName);
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, exit.ChatName);
                    }
                    // collect last 10 messages that were sent while user was online, send to user
                    List<Message> sinceLogin = _dbContext.Messages.Where(m => m.ChatID == chat.ChatID)
                        .Where(m => m.Timestamp >= _logonTime).OrderByDescending(m => m.Timestamp)
                        .Take(10).ToList();
                    string from = "";
                    foreach (Message m in sinceLogin)
                    {
                        from = _dbContext.Users.FindAsync(m.SentFromUser).Result.NormalizedUserName;
                        await Clients.User(Context.ConnectionId).SendAsync("ReceiveMessage", from, DateTime.Now, m.Content, m.MessageID);
                    }
                }
                // this way the user can only start sending messages again at the end of having their chat set up
                await Clients.User(Context.ConnectionId).SendAsync("GetNewRoom", join, isPrivate);
            } 
            catch (Exception ex)
            {
                // should log when errors occur but idk how
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        public async Task ReportMessage(int id, string reason)
        {
            try
            {
                Message reported = await _dbContext.Messages.FindAsync(id);
                if (reported != null)
                {
                    Report reportOut = new Report();
                    reportOut.Reason = reason;
                    // I could have it be the ID parameter but I want to make sure it finds a result first
                    reportOut.MessageID = reported.MessageID;
                    _dbContext.Reports.Add(reportOut);
                    await _dbContext.SaveChangesAsync();
                }
            } 
            catch (Exception ex)
            {
                // should log when errors occur but idk how
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toUser"></param>
        /// <param name="message"></param>
        public async Task SendPrivateMessage(int toUser, string message)
        {
            if (!message.Equals(""))
            {
                int messageID = -1;
                string from = "Empty";
                int fKey = -1;
                string to = "Empty2";
                DateTime time = DateTime.Now;
                int outKey = toUser;
                try
                {
                    CoreUser fUser = await _dbContext.Users.FindAsync(Context.UserIdentifier);
                    CoreUser tUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.StandingID == toUser);
                    from = fUser.NormalizedUserName;
                    fKey = fUser.StandingID;
                    to = tUser.Id;
                    
                    Message sent = new Message();
                    sent.Content = message;
                    sent.Timestamp = time;
                    sent.SentFromUser = Context.UserIdentifier;
                    sent.SentToUser = to;

                    messageID = sent.MessageID;
                    _dbContext.Messages.Add(sent);
                    await _dbContext.SaveChangesAsync();
                } 
                catch (Exception ex)
                {
                    // should log when errors occur but idk how
                }
                // send the message to just the two users
                if (messageID > -1)
                {
                    await Clients.User(Context.ConnectionId).SendAsync("ReceivePrivateMessage", 
                        from, time, message, messageID, outKey);
                    await Clients.User(to).SendAsync("ReceivePrivateMessage",
                        from, time, message, messageID, fKey);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomNumber"></param>
        /// <param name="message"></param>
        public async Task SendMessage(int roomNumber, string message)
        {
            if (!message.Equals(""))
            {
                int messageID = 0;
                string userName = "Empty";
                string chatroom = "Default";
                DateTime mTime = DateTime.Now;
                try
                {
                    CoreUser person = await _dbContext.Users.FindAsync(Context.UserIdentifier);
                    Chatroom room = await _dbContext.Chatrooms.FindAsync(roomNumber);
                    if (person != null && room != null)
                    {
                        chatroom = room.ChatName;
                        userName = person.NormalizedUserName;
                        Message sent = new Message();
                        sent.Content = message;
                        sent.Timestamp = mTime;
                        sent.SentFromUser = Context.UserIdentifier;
                        messageID = sent.MessageID;
                        sent.ChatID = roomNumber;
                        _dbContext.Add(sent);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // should log when errors occur but idk how
                }

                await Clients.Group(chatroom).SendAsync("ReceiveMessage", userName, mTime, message, messageID);
            }
        }
    }
}