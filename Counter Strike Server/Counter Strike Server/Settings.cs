// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    //Do not make int hole like 0,1,3,4
    public enum ServerStatus
    {
        ONLINE = 0,
        MAINTENANCE = 1,
    };

    public class Settings
    {
        /// <summary>
        /// Check if the server is running on Linux
        /// </summary>
        public static void ReadOsInfo()
        {
            Type t = Type.GetType("Mono.Runtime");
            if (t != null)
                isOnLinux = true;
            else
                isOnLinux = false;
        }

        public const int ServerStatusCount = 2;
        public const int serverRefreshRate = 33; // Default : 33
        public const int maxConnection = 1000; // Default 1000
        public const int TimeOutSeconds = 10; // Default 10
        public const string SERVER_VERSION = "1.0.0";
        public static bool isOnLinux = false;
        // Enable this improve the security to avoid custom csds builds,
        // But you need to create your own csds build if you want to host you own server
        public static bool ENABLE_SECURITY_KEY = false;
        public static bool ENABLE_CONSOLE_PRINT = false;
        public static bool ENABLE_LOGGING = true;
        public static ServerStatus serverStatus = ServerStatus.ONLINE;
        public const int maxPlayerPerParty = 6;// DEFAULT 6 (MAX 6)
        public static List<string> GAME_VERSIONS = new() { "1.0.0" };
    }
}
