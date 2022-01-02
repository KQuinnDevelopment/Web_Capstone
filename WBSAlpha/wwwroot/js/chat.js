"use strict";
/*
Modified By: Quinn Helm
Date:        03-12-2021
*/
var connection = new signalR.HubConnectionBuilder().withUrl("/ChatClient").build();
var roomID = -1;
var isPrivate = false;
var wasPrivate = false;

document.getElementById("userSend").disabled = true;

// receive message
connection.on("ReceiveMessage", (content) => {
    var li = document.createElement("li");
    li.className = "row mx-auto";
    li.id = content.name;
    var a = document.createElement("a");
    a.className = "text-info mx-auto";
    a.id = content.mId;
    a.innerHTML = content.name;
    a.addEventListener("click", function (event) {
        reportMe(content.mId);
        event.preventDefault();
    });
    li.textContent = `${content.name} - ${content.time}: ${rebuildString(content.message)} `;
    li.appendChild(a);
    document.getElementById("chatBox").appendChild(li);
});
// receive private message
connection.on("ReceivePrivateMessage", (pm) => {
    if (roomID != parseInt(pm.key)) {
        var li = document.createElement("li");
        li.className = "nav-item";
        li.id = pm.name;
        if (content.mId != -1) {
            var a = document.createElement("a");
            a.className = "text-info";
            a.id = parseInt(pm.key);
            a.innerHTML = pm.name;
            a.addEventListener("click", function (event) {
                reportMe(pm.mId);
                event.preventDefault();
            });
            li.appendChild(a);
        }
        li.appendChild(a);
        document.getElementById("chatList").appendChild(li);
    } else {
        var li = document.createElement("li");
        li.className = "row mx-auto";
        li.id = pm.mId;
        if (content.mId != -1) {
            var a = document.createElement("a");
            a.className = "text-info";
            a.id = parseInt(pm.key);
            a.innerHTML = pm.name;
            a.addEventListener("click", function (event) {
                reportMe(pm.mId);
                event.preventDefault();
            });
            li.appendChild(a);
        }
        li.textContent = `${pm.name} - ${pm.time}: ${rebuildString(pm.message)}`;
        document.getElementById("chatBox").appendChild(li);
    }
});
// ensure that any malicious code cannot run on user end
function rebuildString(string) {
    // taken from https://stackoverflow.com/a/48226843
    const mapper = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#x27;',
        "/": '&#x2F;',
        "`": '&grave;'
    };
    const expression = /[&<>"'/`]/ig;
    return string.replace(expression, (match) => (mapper[match]));
}
// can't have an anonymous function remove/add listeners so this has to be made
function sendMessage() {
    var message = document.getElementById("messageInput").value;
    if (isPrivate) {
        connection.invoke("SendPrivateMessage", roomID, message).catch(function (error) {
            return console.error(error.toString());
        });
    }
    else {
        connection.invoke("SendMessage", roomID, message).catch(function (error) {
            return console.error(error.toString());
        });
    }
    console.log(message + ", is private: " + isPrivate);
    document.getElementById("messageInput").value = "";
}
// need to be able to add rooms programmatically on first entering
function swapRoom(id, isPrivate) {
    document.getElementById("userSend").disabled = true;
    document.getElementById("chatBox").innerHTML = "";
    console.log("joining " + id);
    connection.invoke("ChangeChats", id, roomID, isPrivate, wasPrivate).catch(function (error) {
        return console.error(error.toString());
    });
    wasPrivate = isPrivate;
    document.getElementById("userSend").disabled = false;
}
// report message prompt!
function reportMe(id) {
    let reason = prompt("Reason for Report?");
    if (reason != null && id != -1) {
        connection.invoke("ReportMessage", id, reason).catch(function (error) {
            return console.error(error.toString());
        });
    }
}

// need to make sure that when user change rooms, update the text chat
// as well as roomID and roomName variables
connection.on("AddChatroom", (room) => {
    var li = document.createElement("li");
    li.className = "nav-item";
    li.id = room.name;
    var a = document.createElement("a");
    a.className = "nav-link";
    a.id = room.id;
    a.innerHTML = room.name;
    a.addEventListener("click", function (event) {
        swapRoom(room.id, false);
        event.preventDefault();
    });
    li.appendChild(a);
    document.getElementById("chatList").appendChild(li);
    if (roomID == -1) {
        roomID = parseInt(room.id);
    }
});
connection.on("AddNewUser", (user) => {
    var li = document.createElement("li");
    li.className = "row mx-auto p-1";
    li.id = user.name;
    var a = document.createElement("a");
    a.className = "text-info";
    a.id = user.id;
    a.innerHTML = user.name;
    a.addEventListener("click", function (event) {
        console.log("user clicked!");
        event.preventDefault();
    });
    li.appendChild(a);
    if (user.special) {
        document.getElementById("mods").appendChild(li);
    } else {
        document.getElementById("normalUsers").appendChild(li);
    }
});

connection.on("Disconnect", function () {
    connection.stop();
});

connection.start().then(function () {
    document.getElementById("userSend").disabled = false;
    document.getElementById("userSend").addEventListener("click", function (event) {
        sendMessage();
        event.preventDefault();
    });
    document.getElementById("userMessageForm").addEventListener("submit", function (event) {
        event.preventDefault();
    });
}).catch(err => console.error(err));