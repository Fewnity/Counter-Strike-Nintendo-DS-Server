// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Text;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    /// <summary>
    /// Used to send data to clients
    /// </summary>
    public class Call
    {
        private Call(string data)
        {
            this.data = data;
        }
        private List<Client> allClientsDestination = new ();
        private string data;

        #region Call creation

        /// <summary>
        /// Create a call to send data to a client
        /// </summary>
        /// <param name="Data">Data to sent</param>
        /// <param name="Destination">Receiver</param>
        public static void Create(string Data, Client Destination)
        {
            Call NewCall = new(Data);
            NewCall.allClientsDestination.Add(Destination);
            Send(NewCall);
        }

        /// <summary>
        /// Create a call to send data to a client list
        /// </summary>
        /// <param name="Data">Data to sent</param>
        /// <param name="Destinations">Receivers</param>
        public static void Create(string Data, List<Client> Destinations)
        {
            Call NewCall = new(Data);
            NewCall.allClientsDestination.AddRange(Destinations);
            Send(NewCall);
        }

        /// <summary>
        /// Create a call to send data to a client list
        /// </summary>
        /// <param name="Data">Data to sent</param>
        /// <param name="Destinations">Receivers</param>
        /// <param name="DestinationToRemove">Receiver to remove</param>
        public static void Create(string Data, List<Client> Destinations, Client DestinationToRemove)
        {
            Call NewCall = new(Data);
            NewCall.allClientsDestination.AddRange(Destinations);
            NewCall.allClientsDestination.Remove(DestinationToRemove);
            Send(NewCall);
        }

        #endregion

        /// <summary>
        /// Send the call
        /// </summary>
        /// <param name="callToSend">Call to send</param>
        private static void Send(Call callToSend)
        {
            byte[] msg = new List<byte>(Encoding.ASCII.GetBytes("{" + callToSend.data + "}")).ToArray();
            int clientCount = callToSend.allClientsDestination.Count;

            for (int clientIndex = 0; clientIndex < clientCount; clientIndex++)
            {
                //Send packet
                try
                {
                    //Create call data byte list
                    UserInterfaceManager.PrintOutData(callToSend.allClientsDestination[clientIndex], callToSend.data);

                    callToSend.allClientsDestination[clientIndex].currentClientStream.Write(msg);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
