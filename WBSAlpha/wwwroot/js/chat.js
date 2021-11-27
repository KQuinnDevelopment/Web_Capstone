"use strict";
/*
Modified By: Quinn Helm
Date:        23-11-2021
*/
var connection = new signalR.HubConnectionBuilder().withUrl("/Chathub").build();
var roomID = -1;
var wasPrivate = false;

document.getElementById("sendMessageButton").disabled = true;

connection.on("ReceiveMessage", function (user, time, message, messageID) {
    var li = document.createElement("li").className("row mx-auto p-1");
    var a = document.createElement("a").className("text-danger mx-auto");
    a.addEventListener("click", function (event) {
        // need to have a popup that asks user for reason input&confirmation
        connection.invoke("ReportMessage", messageID, "reason").catch(function (error) {
            return console.error(error.toString());
        });
        event.preventDefault();
    });
    document.getElementById("chatBox").appendChild(li);
    li.textContent = `${user} - ${time}: ${message}`;
    li.appendChild(a);
});
connection.on("ReceivePrivateMessage", function (user, time, message, messageID, key) {
    if (roomID != key) {
        var li = document.createElement("li").className("nav-item");
        var a = document.createElement("a").className("nav-link").setAttribute("id", key);
        a.innerHTML = user;
        a.addEventListener("click", function (event) {
            swapRoom(key, true);
            event.preventDefault();
        });
    } else {
        var li = document.createElement("li").className("row mx-auto p-1");
        var a = document.createElement("a").className("text-danger mx-auto");
        a.addEventListener("click", function (event) {
            // need to have a pop up that asks user for reason input&confirmation
            connection.invoke("ReportMessage", messageID, "reason").catch(function (error) {
                return console.error(error.toString());
            });
            event.preventDefault();
        });
        document.getElementById("chatBox").appendChild(li);
        li.textContent = `${user} - ${time}: ${message}`;
        li.appendChild(a);
    }
});

// need to be able to add rooms programmatically on first entering
function swapRoom(id, isPrivate) {
    while (document.getElementById("chatBox").childElementCount > 0) {
        document.getElementById("chatBox").removeChild();
    }
    document.getElementById("sendMessageButton").disabled = true;
    connection.invoke("ChangeChats", id, roomID, isPrivate, wasPrivate).catch(function (error) {
        return console.error(error.toString());
    });
}
connection.on("GetNewRoom", function (id, isPrivate) {
    roomID = id;
    document.getElementById("sendMessageButton").disabled = false;
    wasPrivate = isPrivate;
    if (isPrivate) {
        document.getElementById("sendMessageButton").removeEventListener("click", sendMessage());
        document.getElementById("sendMessageButton").addEventListener("click", sendPrivateMessage());
    } else {
        document.getElementById("sendMessageButton").removeEventListener("click", sendPrivateMessage());
        document.getElementById("sendMessageButton").addEventListener("click", sendMessage());
    }
});
// can't have an anonymous function remove/add listeners so this has to be made
function sendMessage() {
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", roomID, message).catch(function (error) {
        return console.error(error.toString());
    });
    event.preventDefault();
}
function sendPrivateMessage() {
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendPrivateMessage", roomID, message).catch(function (error) {
        return console.error(error.toString());
    });
    event.preventDefault();
}
// need to make sure that when user change rooms, update the text chat
// as well as roomID and roomName variables
connection.on("AddChatroom", function (id, name) {
    var li = document.createElement("li").className("nav-item");
    var a = document.createElement("a").className("nav-link").setAttribute("id", id);
    a.innerHTML = name;
    a.addEventListener("click", function (event) {
        swapRoom(id, false);
        event.preventDefault();
    });
    document.getElementById("chatList").appendChild(li);
    if (roomID == -1) {
        roomID = id;
    }
});
connection.on("AddNewUser", function (id, name, isModerator) {
    var li = document.createElement("li").className("row mx-auto p-1");
    var a = document.createElement("a").className("text-info mx-auto").setAttribute("id", id);
    a.innerHTML = name;
    a.addEventListener("click", function (event) {
        swapRoom(id, true);
        event.preventDefault();
    });
    if (isModerator) {
        document.getElementById("mods").appendChild(li);
    } else {
        document.getElementById("normalUsers").appendChild(li);
    }
});

connection.start().then(function () {
    connection.invoke("SpawnChats");
    connection.invoke("UpdateUserList");
    document.getElementById("sendMessageButton").disabled = false;
}).catch(function (error) {
    return console.error(error.toString());
});
document.getElementById("sendMessageButton").addEventListener("click", sendMessage());