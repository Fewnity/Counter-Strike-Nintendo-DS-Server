// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;
using System.Text;

namespace Counter_Strike_Server
{
    public class Call
    {
        public Call(string data)
        {
            this.data = data;
        }
        public List<Client> allClientsDestination = new List<Client>();
        public string data;

        /// <summary>
        /// Create a call to send data to a client
        /// </summary>
        /// <param name="Data">Data to sent</param>
        /// <param name="Destination">Receiver</param>
        public static void CreateCall(string Data, Client Destination)
        {
            Call NewCall = new Call(Data);
            NewCall.allClientsDestination.Add(Destination);
            SendAfterCreateCall(NewCall);
        }

        /// <summary>
        /// Create a call to send data to a client list
        /// </summary>
        /// <param name="Data">Data to sent</param>
        /// <param name="Destinations">Receivers</param>
        public static void CreateCall(string Data, List<Client> Destinations)
        {
            Call NewCall = new Call(Data);
            NewCall.allClientsDestination.AddRange(Destinations);
            SendAfterCreateCall(NewCall);
        }

        /// <summary>
        /// Create a call to send data to a client list
        /// </summary>
        /// <param name="Data">Data to sent</param>
        /// <param name="Destinations">Receivers</param>
        /// <param name="DestinationToRemove">Receiver to remove</param>
        public static void CreateCall(string Data, List<Client> Destinations, Client DestinationToRemove)
        {
            Call NewCall = new Call(Data);
            NewCall.allClientsDestination.AddRange(Destinations);
            NewCall.allClientsDestination.Remove(DestinationToRemove);
            SendAfterCreateCall(NewCall);
        }

        /// <summary>
        /// Send the call
        /// </summary>
        /// <param name="callToSend">Call to send</param>
        public static void SendAfterCreateCall(Call callToSend)
        {
            byte[] msg = new List<byte>(Encoding.ASCII.GetBytes("{" + callToSend.data + "}")).ToArray();
            int clientCount = callToSend.allClientsDestination.Count;

            for (int i = 0; i < clientCount; i++)
            {
                //Send packet
                try
                {
                    //Create call data byte list
                    NetworkDataManager.PrintOutData(callToSend.allClientsDestination[i], callToSend.data);

                    callToSend.allClientsDestination[i].currentClientStream.Write(msg);
                    //callToSend.allClientsDestination[i].currentClientStream.Flush();
                }
                catch (Exception)
                {

                }
            }
        }

        /// <summary>
        /// Send a raw call to a client (not recommended)
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="dataToSend">Data to send</param>
        public static void SendDirectMessageToClient(Client client, string dataToSend)
        {
            byte[] msg = new List<byte>(Encoding.ASCII.GetBytes("{" + dataToSend + "}")).ToArray();
            try
            {
                NetworkDataManager.PrintOutData(client, dataToSend);
                //Send packet
                client.currentClientStream.Write(msg);
            }
            catch (Exception)
            {

            }
        }
    }
}
