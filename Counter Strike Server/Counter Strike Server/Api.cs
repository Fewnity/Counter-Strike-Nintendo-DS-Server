// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

namespace Counter_Strike_Server
{
    /// <summary>
    /// Used to send data to a web browser
    /// </summary>
    public class Api
    {
        /// <summary>
        /// Send to the web browser the server status
        /// </summary>
        /// <param name="client">Client (web browser)</param>
        public static void SendServerStatus(Client client)
        {
            //why ConnectionManager.allClients.Count - 1 ? It's to remove the web browser from connected clients
            Call.Create($"STATUS;{(int)Settings.serverStatus};{ConnectionManager.allClients.Count - 1};{Settings.maxConnection};{Settings.SERVER_VERSION};{Settings.GAME_VERSIONS[Settings.GAME_VERSIONS.Count -1]}", client);
        }
    }
}
