# Counter Strike Nintendo DS Server
 
This Visual Studio C# project is the Counter Strike Nintendo DS server for [CSDS](https://github.com/Fewnity/Counter-Strike-Nintendo-DS), to allow Nintendo DS players to play with each other.

This is my first server, so the structure of the code is pretty bad, sorry about that.
I think it's a misake to do static functions like PlayerManager.SetName(client, name) instead of client.SetName(name).<br>

### Any help is welcome!

## How to compile :

### For Windows :
- Install [Visual Studio](https://visualstudio.microsoft.com/fr/downloads/)
- Compile from **Visual Studio**

### For Linux :
- Install [Mono](https://www.mono-project.com/)
- Run the **make.bat** in the project folder

## How to use :
- Open the **6003** port of your computer and your router

### On Windows :
- Launch **Counter Strike Server.exe**
- Set your own restart system if the server crashs

### On Linux :
- Install [Tmux](https://doc.ubuntu-fr.org/tmux) if not installed (apt install tmux)
- Set paths in **cs.server.service** and **start_cs_server.sh**
- Put the **cs.server.service** file in **/etc/sustemd/system/**
- Put the **start_cs_server.sh** in the server executable directory
- Execute **"systemctl daemon-reload"**, **"systemctl enable cs.server"** and **"systemctl start cs.server"**
- To access to the server console, write **"tmux attach-session -t cs_server"**
- To quit the console without stoping the server : Ctrl + b then d

Note : The used ip is automatically found by the server and showed in the console.

### Commands :
Command example : "status maintenance"
- **"help"** to have the commands list
- **"stop"** to stop the server
- **"disable/enable [param]"** to disable or enable a setting param list : **logging/security/console**
- **"status [param]"** to set the server status param list : **online/maintenance or 0/1**

### Settings :
- **Logging** : Log every errors in the cs_log/cs_log.txt file in the executable folder
- **Security** : if enabled, block unofficials game builds
- **Console** : Print all sent and receivied data in the console

## Server status web page :
- Set the Counter Strike Server IP in the index.php  at line 20
- Put the cs_status folder in your server ex : [Wamp](https://www.wampserver.com/) or [Lamp](https://doc.ubuntu-fr.org/lamp)
- Install the websocket support to your server

## Todo :
- [ ] Rework the structure of the server (Avoid static functions, make client.SetName(name) instead of PlayerManager.SetName(client, name)), or something better for clarity
- [ ] Improve security (more check on player action)
- [ ] Improve stability (better variable lock between threads)
- [ ] Improve performance
- [ ] Use the implemented ban system (ban players from IP if they are trying to cheat)
- [ ] Add bots

## Security
Please, if you have found a security vulnerability, contact me here : fewnity@gmail.com
