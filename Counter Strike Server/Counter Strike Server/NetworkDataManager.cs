// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    public static class NetworkDataManager
    {
        public enum ErrorType
        {
            Null = -1,
            Ban = 0,
            WrongVersion = 1,
            MacAddressMissing = 2,
            WrongSecurityKey = 3,
            ServerFull = 4,
            ServerStopped = 5,
            SaveCorrupted = 6,
            IncorrectCode = 7,
            KickTeamKill = 8,
        }

        public enum JoinType
        {
            JOIN_RANDOM_PARTY = 0,
            CREATE_PRIVATE_PARTY = 1,
            JOIN_PRIVATE_PARTY = 2,
        };

        /// <summary>
        /// Thread to get data from all clients
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void CheckForClientData()
        {
            while (true)
            {
                lock (ConnectionManager.allClients)
                {
                    List<Client> allClients = new();
                    try
                    {
                        allClients = new List<Client>(ConnectionManager.allClients);
                    }
                    catch (Exception e)
                    {
                        UserInterfaceManager.PrintError($"{e.Message})\n{e.StackTrace.Replace("in ", "\nin ")}");
                    }
                    //List<Client> allClients = new List<Client>(ConnectionManager.allClients);
                    int count = allClients.Count;

                    //For all client :
                    for (int i = 0; i < count; i++)
                    {
                        //Check client connection (Client need to be connected)
                        //if (AllClients[i].CurrentClientTcp.Connected)
                        //{

                        Client client = null;

                        try
                        {
                            client = allClients[i];
                            //If data is incoming
                            if (client.currentClientTcp.Available > 1)
                            {
                                //Create byte array for incoming data
                                byte[] bytes = new byte[client.currentClientTcp.Available];

                                //Read incoming data
                                client.currentClientStream.Read(bytes, 0, bytes.Length);

                                //translate data bytes to string
                                client.data += Encoding.UTF8.GetString(bytes);

                                UserInterfaceManager.PrintInData(client, client.data);

                                do
                                {
                                    //Faster index of
                                    int StartIndex = -1, EndIndex = -1;
                                    int len = client.data.Length;
                                    for (int i2 = 0; i2 < len; i2++)
                                    {
                                        if (client.data[i2] == '{')
                                        {
                                            StartIndex = i2;
                                            break;
                                        }
                                    }

                                    if (StartIndex == -1)
                                    {
                                        throw new Exception("Wrong data format : StartIndex not found");
                                    }

                                    for (int i2 = StartIndex; i2 < len; i2++)
                                    {
                                        if (client.data[i2] == '}')
                                        {
                                            EndIndex = i2;
                                            break;
                                        }
                                    }

                                    if (EndIndex == -1)
                                    {
                                        throw new Exception("Wrong data format : EndIndex not found");
                                    }

                                    string[] tempDataSplit = client.data.Substring(StartIndex + 1, EndIndex - StartIndex - 1).Split(';');
                                    int dataLenght = tempDataSplit.Length;
                                    if (dataLenght == 0)
                                    {
                                        throw new Exception("Wrong data format : empty request");
                                    }
                                    Router.RouteData(tempDataSplit, client, dataLenght);

                                    //Remove used data from buffer
                                    client.data = client.data.Remove(StartIndex, EndIndex - StartIndex + 1);
                                } while (client.data.Contains('{') && client.data.Contains('}'));
                                //While data contains a complet packet
                            }
                            if (client.NeedRemoveConnection)
                            {
                                int removedClientId = client.id;
                                UserInterfaceManager.PrintMessage($"Client {removedClientId} disconnected");
                                client.RemoveClient(true);

                                continue;
                            }
                        }
                        catch (Exception e)//Get error
                        {
                            if (client != null)
                            {
                                int errorClientId = client.id;
                                client.RemoveClient(true);
                                UserInterfaceManager.PrintError($"Connection {errorClientId} blocked ({e.Message})\n{e.StackTrace.Replace("in ", "\nin ")}");
                                UserInterfaceManager.PrintMessage($"Connection {errorClientId} blocked");
                            }
                        }
                    }
                }
                //Wait for better CPU performance
                Thread.Sleep(Settings.serverRefreshRate);
            }
        }

        /// <summary>
        /// Thread for all clients to check if clients are still connected
        /// </summary>
        public static void Ping()
        {
            while (true)
            {
                //For each party
                lock (ConnectionManager.allClients)
                {
                    List<Client> allClients = new();
                    try
                    {
                        allClients = new List<Client>(ConnectionManager.allClients);
                    }
                    catch (Exception e)
                    {
                        UserInterfaceManager.PrintError($"{e.Message})\n{e.StackTrace.Replace("in ", "\nin ")}");
                    }
                    int count = allClients.Count;
                    for (int clientIndex = 0; clientIndex < count; clientIndex++)
                    {
                        Client currentClient = null;
                        try
                        {
                            currentClient = allClients[clientIndex];
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        if (currentClient != null)
                        {
                            //Check if the last response is too old
                            if ((int)(DateTime.Now - currentClient.lastPing).TotalSeconds >= Settings.TimeOutSeconds)
                            {
                                //Remove client
                                int removedClientId = currentClient.id;
                                currentClient.RemoveClient(true);

                                UserInterfaceManager.PrintMessage($"Connection {removedClientId} blocked (Timeout)");
                            }
                            else if (currentClient.ping != -1)
                            {
                                //Send new ping request to the client to get a response
                                currentClient.communicator.SendPing();
                                currentClient.lastPing = DateTime.Now;
                                currentClient.ping = -1;
                            }
                        }
                    }
                }
                //Wait 4 seconds
                Thread.Sleep(4000);
            }
        }
    }
}
