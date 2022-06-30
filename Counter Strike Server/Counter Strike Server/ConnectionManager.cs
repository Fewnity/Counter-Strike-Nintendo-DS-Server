// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Counter_Strike_Server
{
    static class ConnectionManager
    {
        //Server
        public static TcpListener server;
        public static int totalConnection = 0;

        //Clients informations
        public static List<Client> allClients = new List<Client>();


        public static List<string> bannedIps = new List<string>();
        public static List<string> bannedMac = new List<string>();

        public static List<string> connectedIps = new List<string>();
        public static List<int> connectionCount = new List<int>();


        /// <summary>
        /// Start the server (to call once)
        /// </summary>
        public static void StartServer()
        {
            //Set server Ip
            string ServerIp = "";

            //Create a socket to get the server ip
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                ServerIp = endPoint.Address.ToString();
                socket.Close();
            }

            //Create server
            int serverPort = 6003;
            server = new TcpListener(IPAddress.Parse(ServerIp), serverPort);

            //int serverPort = 1080; //Android port
            //server = new TcpListener(IPAddress.Parse("192.168.43.1"), serverPort);//For android (change the ip)

            //Start server
            server.Start();

            //Start thread for party timer system
            Thread TimerThread = new Thread(new ThreadStart(PartyManager.PartyTimerTick));
            TimerThread.Start();

            //Start thread to send ping requests to players
            Thread PingThread = new Thread(new ThreadStart(NetworkDataManager.Ping));
            PingThread.Start();

            //Start thread to send ping requests to players
            Thread PhysicsThread = new Thread(new ThreadStart(PhysicsManager.CheckPhysics));
            PhysicsThread.Start();

            //Start thread to check for incoming client data
            Thread ReadThread = new Thread(new ThreadStart(NetworkDataManager.CheckForClientData));
            ReadThread.Start();

            //Start thread to check for incoming new client
            Thread CheckForClientThread = new Thread(new ThreadStart(CheckForNewClient));
            CheckForClientThread.Start();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Server has started on {ServerIp}:{serverPort}.\nWaiting for connections...");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("For debug (if enabled):");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("client.id -> Data sent by the client");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("client.id <- Data to send to the client");

            Console.ForegroundColor = ConsoleColor.White;
            PrintAskHelp();

            //Read input
            while (true)
            {
                try
                {
                    //Split the user input
                    string[] userInput = Console.ReadLine().ToLower().Split(" ");

                    Console.ForegroundColor = ConsoleColor.White;

                    if (userInput[0] == "help") //Stop the server
                    {
                        PrintHelpCommand();
                    }
                    else if (userInput[0] == "stop") //Stop the server
                    {
                        Environment.Exit(0);
                    }
                    else if (userInput[0] == "status") //Change the server status
                    {
                        if (userInput[1] == "online")
                        {
                            Settings.serverStatus = ServerStatus.ONLINE;
                        }
                        else if (userInput[1] == "maintenance")
                        {
                            Settings.serverStatus = ServerStatus.MAINTENANCE;
                        }
                        else
                        {
                            int status = int.Parse(userInput[1]);
                            if (status < 0 || status >= Settings.ServerStatusCount)
                                throw new Exception("Wrong status id");

                            Settings.serverStatus = (ServerStatus)int.Parse(userInput[1]);
                            Console.WriteLine($"Server status set to : {Enum.GetName(typeof(ServerStatus), Settings.serverStatus)}");
                        }
                    }
                    else if (userInput[0] == "disable" || userInput[0] == "enable") //Change the server status
                    {
                        bool enable = userInput[0] == "enable";
                        if (userInput[1] == "logging")
                        {
                            Settings.ENABLE_LOGGING = enable;
                        }else if (userInput[1] == "security")
                        {
                            Settings.ENABLE_SECURITY_KEY = enable;
                        }
                        else if (userInput[1] == "console")
                        {
                            Settings.ENABLE_CONSOLE_PRINT = enable;
                        }
                        else
                        {
                            throw new Exception("Wrong command argument");
                        }
                    }
                    else
                    {
                        PrintAskHelp();
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Command error : {e.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
        private static void PrintAskHelp()
        {
            Console.WriteLine("Type 'help' to get commands list");
        }

        private static void PrintHelpCommand()
        {
            Console.WriteLine("\ncommand_name [param] : Utility.\n" +
                           "stop : Stop the server.\n" +
                           "status [online/maintenance or 0/1] : Set the server status.\n" +
                           "disable/enable [logging/security/console] : Disable or enable a setting." +
                           "\n");
        }

        /// <summary>
        /// Thread to check new client who want to join the server
        /// </summary>
        public static void CheckForNewClient()
        {
            while (true)
            {
                //Wait for a incoming client and accept it
                TcpClient client = server.AcceptTcpClient();

                //NetworkDataManager.PrintMessage($"COUNT {totalConnection}");
                //Get client connections
                Client NewClient = new Client();
                NewClient.currentClientTcp = client;
                NewClient.currentClientStream = client.GetStream();

                int ClientCount = allClients.Count;
                //Get client ip
                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                NetworkDataManager.ErrorType errorType = NetworkDataManager.ErrorType.Null;
                try
                {
                    if (Settings.serverStatus == ServerStatus.MAINTENANCE)
                    {
                        SendError(NewClient, NetworkDataManager.ErrorType.ServerStopped);
                        errorType = NetworkDataManager.ErrorType.ServerStopped;
                        throw new Exception("Server in maintenance");
                    }
                    if (ClientCount >= Settings.maxConnection)
                    {
                        SendError(NewClient, NetworkDataManager.ErrorType.ServerFull);
                        errorType = NetworkDataManager.ErrorType.ServerFull;
                        throw new Exception("Max connection count reached");
                    }
                    if (bannedIps.Contains(clientIp))
                    {
                        SendError(NewClient, NetworkDataManager.ErrorType.Ban);
                        errorType = NetworkDataManager.ErrorType.Ban;
                        throw new Exception("A banned client tried to connect");
                    }

                    //Check how many connections this ip has on the server to block the client to reduce a DDOS effect
                    if (!connectedIps.Contains(clientIp))
                    {
                        connectionCount.Add(1);
                        connectedIps.Add(clientIp);
                    }
                    else
                    {
                        int connectionCountIndex = connectedIps.IndexOf(clientIp);
                        connectionCount[connectionCountIndex]++;
                        //Reject the connection if there are too many connections
                        if (connectionCount[connectionCountIndex] >= 15)
                        {
                            SendError(NewClient, NetworkDataManager.ErrorType.ServerFull);
                            throw new Exception("Max connection count on this IP reached");
                        }
                    }
                }
                catch (Exception e)
                {
                    RemoveClient(NewClient, true);
                    //NetworkDataManager.PrintError($"{e.Message}\n{e.StackTrace.Replace("in ", "\nin ")}");
                    continue;
                }

                NewClient.id = totalConnection;
                //Init inventory
                NewClient.allGunsInInventory[0] = InventoryManager.KNIFE_ID;

                for (int i = 0; i < 2; i++)
                {
                    NewClient.AllAmmoMagazine[i] = new AmmoMagazine();
                }

                for (int i = 1; i < InventoryManager.INVENTORY_SIZE; i++)
                    NewClient.allGunsInInventory[i] = -1;

                //Add client to clients list
                allClients.Add(NewClient);
                totalConnection++;
                if (totalConnection >= int.MaxValue - 1)
                {
                    totalConnection = 0;
                }

                NewClient.sentKey = Security.GetBaseKey();
                Call.CreateCall($"KEY;{NewClient.sentKey}", NewClient);

                NetworkDataManager.PrintMessage($"A client connected, Id : {NewClient.id}, Clients count : {ClientCount + 1}");
            }
        }

        /// <summary>
        /// Remove a client from the party
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="notifyClients">Notify the client of the disconnection</param>
        /// <returns>Return if the client party is deleted now</returns>
        public static bool RemoveClient(Client client, bool notifyClients)
        {
            Debug.WriteLine($"Disconnect : {client.id}");
            //Notify the client of the disconnection
            if (client.clientParty != null && notifyClients)
            {
                Call.CreateCall($"LEAVE;{client.id}", client.clientParty.allConnectedClients);
            }
            string clientIp = "null";
            //Remove the client connection count of the total connection count of his ip
            try
            {
                clientIp = ((IPEndPoint)client.currentClientTcp.Client.RemoteEndPoint).Address.ToString();
                connectionCount[connectedIps.IndexOf(clientIp)]--;
            }
            catch (Exception e)
            {
                Logger.LogErrorInFile($"Not able to reduce client's connection count (client IP : {clientIp}) {e.Message} {e.StackTrace}");
            }

            //Close stream
            client.currentClientStream.Close();
            client.currentClientTcp.Close();
            allClients.Remove(client);

            //Remove the client from his party
            if (client.clientParty != null)
            {
                client.clientParty.allConnectedClients.Remove(client);
                PlayerManager.OnPlayerKilled(client.clientParty, null);

                //Update vote
                if (!client.clientParty.partyStarted)
                {
                    PartyManager.SendVoteResult(client.clientParty, VoteType.ForceStart);
                }
                else
                {
                    CheckIfTeamsAreEquilibrated(client.clientParty);
                    CheckIfThereIsEmptyTeam(client.clientParty);
                }

                lock (PartyManager.allParties)
                {
                    return PartyManager.CheckEmptyParty(client.clientParty);
                }
            }
            return false;
        }

        public static void EquilibrateTeams(Party party)
        {
            party.needTeamEquilibration = false;
            //If there is enought players to equilibrate the party
            if (party.allConnectedClients.Count >= 3)
            {
                //Get count
                int couterTerroristsCount, terroristsCount;
                TeamManager.CheckTeamCount(party, out terroristsCount, out couterTerroristsCount);
                Random rd = new Random();

                while (Math.Abs(couterTerroristsCount - terroristsCount) >= 2)
                {
                    if (couterTerroristsCount > terroristsCount)
                    {
                        couterTerroristsCount--;
                        terroristsCount++;

                        int rdNb;
                        do
                        {
                            rdNb = rd.Next(party.allConnectedClients.Count);
                        } while (party.allConnectedClients[rdNb].team != teamEnum.COUNTERTERRORISTS);

                        TeamManager.SetTeam(teamEnum.TERRORISTS, party.allConnectedClients[rdNb]);
                        InventoryManager.ClearInventory(party.allConnectedClients[rdNb]);
                    }
                    else
                    {
                        couterTerroristsCount++;
                        terroristsCount--;

                        int rdNb;
                        do
                        {
                            rdNb = rd.Next(party.allConnectedClients.Count);
                        } while (party.allConnectedClients[rdNb].team != teamEnum.TERRORISTS);

                        TeamManager.SetTeam(teamEnum.COUNTERTERRORISTS, party.allConnectedClients[rdNb]);
                        InventoryManager.ClearInventory(party.allConnectedClients[rdNb]);
                    }
                }
            }
        }

        public static void CheckIfThereIsEmptyTeam(Party party)
        {
            //If there is not enought players to continue the party (if players are in the same team)
            if (party.allConnectedClients.Count == 2)
            {
                //Get count
                int couterTerroristsCount, terroristsCount;
                TeamManager.CheckTeamCount(party, out terroristsCount, out couterTerroristsCount);
                if (Math.Abs(couterTerroristsCount - terroristsCount) == 2)
                {
                    //Finish the party
                    PartyManager.StopParty(party);
                }
            }
        }

        public static void CheckIfTeamsAreEquilibrated(Party party)
        {
            //If there is enought players to equilibrate the party
            if (party.allConnectedClients.Count >= 3)
            {
                //Get count
                int couterTerroristsCount, terroristsCount;
                TeamManager.CheckTeamCount(party, out terroristsCount, out couterTerroristsCount);
                if (Math.Abs(couterTerroristsCount - terroristsCount) >= 2)
                {
                    party.needTeamEquilibration = true;
                }
                else
                {
                    party.needTeamEquilibration = false;
                }
            }
            else
            {
                party.needTeamEquilibration = false;
            }
        }

        /// <summary>
        /// Check if Client info are valid
        /// </summary>
        /// <param name="macAddress">Client mac adress</param>
        /// <param name="version">Client version</param>
        /// <param name="client">Client</param>
        /// <exception cref="Exception"></exception>
        public static void CheckPlayerInfo(string macAddress, string version, Client client)
        {
            if (string.IsNullOrEmpty(version))//If the mac address of the nintendo ds is banned
            {
                SendError(client, NetworkDataManager.ErrorType.WrongVersion);
                throw new Exception("The game version is missing");
            }
            else if (!Settings.GAME_VERSIONS.Contains(version))            //Check is the game version match with the server version
            {
                SendError(client, NetworkDataManager.ErrorType.WrongVersion);
                throw new Exception("Wrong game version");
            }
            else if (string.IsNullOrEmpty(macAddress))//If the mac address of the nintendo ds is banned
            {
                SendError(client, NetworkDataManager.ErrorType.MacAddressMissing);
                throw new Exception("The MAC address is missing");
            }
            else if (macAddress.Length > 14)//If the mac address of the nintendo ds is banned
            {
                SendError(client, NetworkDataManager.ErrorType.MacAddressMissing);
                throw new Exception("The MAC address is wrong");
            }
            else if (bannedMac.Contains(client.macAddress))//If the mac address of the nintendo ds is banned
            {
                SendError(client, NetworkDataManager.ErrorType.Ban);
                throw new Exception("A banned client tried to connect");
            }

            //Get Mac address from data
            client.macAddress = macAddress;
        }

        /// <summary>
        /// Send an error to the client
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="errorType">Error type</param>
        public static void SendError(Client client, NetworkDataManager.ErrorType errorType)
        {
            Call.CreateCall($"ERROR;{(int)errorType}", client);
        }

        /// <summary>
        /// Send party data to the new client
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="client">Client</param>
        public static void SendPartyData(Party party, Client client)
        {
            Call.CreateCall($"ADDRANGE;{client.id}", client.clientParty.allConnectedClients, client);

            //Send new client ID to new client
            Call.CreateCall($"SETID;{client.id}", client);

            Call.CreateCall($"SETMAP;{(int)party.mapId}", client);
            Call.CreateCall($"SETMODE;{PartyManager.allPartyModesData.IndexOf(party.partyMode)}", client);

            Call.CreateCall($"SETNAME;{client.id};{client.name}", client.clientParty.allConnectedClients, client);
            PartyManager.SendVoteResult(party, VoteType.ForceStart);
            PartyManager.sendPartyRound(client);


            if (party.isPrivate)
                Call.CreateCall($"SETCODE;{party.password}", client);

            //Send all party's clients ID to new client
            StringBuilder NewCallData = new StringBuilder();

            NewCallData.Append("ADDRANGE;");
            for (int ClientI = 0; ClientI < client.clientParty.allConnectedClients.Count; ClientI++)
                if (client.clientParty.allConnectedClients[ClientI] != client)
                {
                    NewCallData.Append(client.clientParty.allConnectedClients[ClientI].id);
                    NewCallData.Append(";");
                }

            NewCallData.Remove(NewCallData.Length - 1, 1);
            Call.CreateCall(NewCallData.ToString(), client);

            //Send all clients names to the client
            for (int ClientI = 0; ClientI < client.clientParty.allConnectedClients.Count; ClientI++)
            {
                Client client1 = client.clientParty.allConnectedClients[ClientI];
                if (client1 != client)
                {
                    Call.CreateCall($"SETNAME;{client1.id};{client1.name}", client);
                }
            }

            client.money = client.clientParty.partyMode.maxMoney;
            MoneyManager.SendMoney(client);

            PlayerManager.ResetPlayer(client);

            InventoryManager.SendClientsInventoryToClient(client);

            PartyManager.UpdatePartyToClient(client);
            if (party.bombDropped || party.bombSet)
                BombManager.SendBombPosition(client);
            else
            {
                BombManager.SendWhoHasTheBomb(client);
            }
            PartyManager.SendPartyTimer(party);
            Call.CreateCall($"ENDUPDATE", client);
        }

        /// <summary>
        /// Update the last time the client sent a response to the server's ping request
        /// </summary>
        /// <param name="client"></param>
        public static void UpdateClientPing(Client client)
        {
            client.ping = (int)(DateTime.Now - client.lastPing).TotalMilliseconds;
        }
    }
}
