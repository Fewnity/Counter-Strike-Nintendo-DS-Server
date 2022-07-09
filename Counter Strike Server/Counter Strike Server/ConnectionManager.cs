// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    /// <summary>
    /// Used to manage connections (allow new clients)
    /// </summary>
    static class ConnectionManager
    {
        //Server
        public static TcpListener server;
        public static int totalConnection = 0;

        //Clients informations
        public static List<Client> allClients = new();


        public static List<string> bannedIps = new();
        public static List<string> bannedMac = new();

        public static List<string> connectedIps = new();
        public static List<int> connectionCount = new();


        /// <summary>
        /// Start the server (to call once)
        /// </summary>
        public static void StartServer()
        {
            //Set server Ip
            string ServerIp = "";

            //Create a socket to get the server ip
            using (Socket socket = new (AddressFamily.InterNetwork, SocketType.Dgram, 0))
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
            Thread TimerThread = new (new ThreadStart(PartyManager.PartyTimerTick));
            TimerThread.Start();

            //Start thread to send ping requests to players
            Thread PingThread = new (new ThreadStart(NetworkDataManager.Ping));
            PingThread.Start();

            //Start thread to send ping requests to players
            Thread PhysicsThread = new (new ThreadStart(PhysicsManager.CheckPhysics));
            PhysicsThread.Start();

            //Start thread to check for incoming client data
            Thread ReadThread = new (new ThreadStart(NetworkDataManager.CheckForClientData));
            ReadThread.Start();

            //Start thread to check for incoming new client
            Thread CheckForClientThread = new (new ThreadStart(CheckForNewClient));
            CheckForClientThread.Start();

            UserInterfaceManager.PrintFirstMessage(ServerIp, serverPort);

            //Read input
            while (true)
            {
                try
                {
                    UserInterfaceManager.ReadConsole();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Command error : {e.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
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
                Client NewClient = new()
                {
                    currentClientTcp = client,
                    currentClientStream = client.GetStream()
                };

                int ClientCount = allClients.Count;
                //Get client ip
                try
                {
                    NewClient.ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    if (string.IsNullOrWhiteSpace(NewClient.ip) || NewClient.ip == "null")
                    {
                        NewClient.communicator.SendError(NetworkDataManager.ErrorType.Null);
                        throw new Exception("Ip empty");
                    }
                    if (Settings.serverStatus == ServerStatus.MAINTENANCE)
                    {
                        NewClient.communicator.SendError(NetworkDataManager.ErrorType.ServerStopped);
                        throw new Exception("Server in maintenance");
                    }
                    if (ClientCount >= Settings.maxConnection)
                    {
                        NewClient.communicator.SendError(NetworkDataManager.ErrorType.ServerFull);
                        throw new Exception("Max connection count reached");
                    }
                    if (bannedIps.Contains(NewClient.ip))
                    {
                        NewClient.communicator.SendError(NetworkDataManager.ErrorType.Ban);
                        throw new Exception("A banned client tried to connect");
                    }

                    //Check how many connections this ip has on the server to block the client to reduce a DDOS effect
                    if (!connectedIps.Contains(NewClient.ip))
                    {
                        connectionCount.Add(1);
                        connectedIps.Add(NewClient.ip);
                    }
                    else
                    {
                        int connectionCountIndex = connectedIps.IndexOf(NewClient.ip);
                        connectionCount[connectionCountIndex]++;

                        //Reject the connection if there are too many connections
                        if (connectionCount[connectionCountIndex] >= 15)
                        {
                            NewClient.communicator.SendError(NetworkDataManager.ErrorType.ServerFull);
                            throw new Exception("Max connection count on this IP reached");
                        }
                    }
                }
                catch (Exception)
                {
                    NewClient.RemoveClient(true);
                    //NetworkDataManager.PrintError($"{e.Message}\n{e.StackTrace.Replace("in ", "\nin ")}");
                    continue;
                }

                NewClient.id = totalConnection;
                //Init inventory
                NewClient.inventory.allSlots[0] = Inventory.KNIFE_ID;

                for (int i = 0; i < 2; i++)
                {
                    NewClient.inventory.AllAmmoMagazine[i] = new Inventory.AmmoMagazine();
                }

                for (int i = 1; i < Inventory.INVENTORY_SIZE; i++)
                    NewClient.inventory.allSlots[i] = -1;

                //Add client to clients list
                allClients.Add(NewClient);
                totalConnection++;
                if (totalConnection >= int.MaxValue - 1)
                {
                    totalConnection = 0;
                }

                NewClient.sentKey = Security.GetBaseKey();
                Call.Create($"KEY;{NewClient.sentKey}", NewClient);

                UserInterfaceManager.PrintMessage($"A client connected, Id : {NewClient.id}, Clients count : {ClientCount + 1}");
            }
        }
    }
}
