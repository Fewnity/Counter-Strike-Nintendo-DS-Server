// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

namespace Counter_Strike_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            //Get os info
            Settings.ReadOsInfo();
            Logger.CreateLogFile(System.AppDomain.CurrentDomain.BaseDirectory);

            //Load game data
            MapManager.LoadMapsData();
            PartyManager.SetAllPartyModesData();
            ShopManager.AddAllShopElements();

            //Start server
            ConnectionManager.StartServer();
        }

        /// <summary>
        /// Re-maps a number from one range to another.<br></br>
        /// <see href="https://www.arduino.cc/reference/en/language/functions/math/map/">Arduino doc here!</see>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="in_min"></param>
        /// <param name="in_max"></param>
        /// <param name="out_min"></param>
        /// <param name="out_max"></param>
        /// <returns></returns>
        public static double Map(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        public static float Map(float x, float in_min, float in_max, float out_min, float out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        public static int MapInt(int x, int in_min, int in_max, int out_min, int out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}
