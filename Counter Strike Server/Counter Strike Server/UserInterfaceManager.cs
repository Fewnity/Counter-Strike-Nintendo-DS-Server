// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;

namespace Counter_Strike_Server
{
    static class UserInterfaceManager
    {
        /// <summary>
        /// Read for input in the console
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void ReadConsole()
        {
            //Split the user input
            string[] userInput = Console.ReadLine().ToLower().Split(" ");

            Console.ForegroundColor = ConsoleColor.White;

            if (userInput[0] == "help") //Stop the server
            {
                PrintCommandsList();
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
                }
                else if (userInput[1] == "security")
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

        /// <summary>
        /// Print some informations after the server startup
        /// </summary>
        /// <param name="ServerIp"></param>
        /// <param name="serverPort"></param>
        public static void PrintFirstMessage(string ServerIp, int serverPort)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Server has started on {ServerIp}:{serverPort}.\nWaiting for connections...");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("For debug (if enabled):");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("client.id -> Data sent by the client");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("client.id <- Data sent to the client");

            Console.ForegroundColor = ConsoleColor.White;
            UserInterfaceManager.PrintAskHelp();
        }


        /// <summary>
        /// Print the help message
        /// </summary>
        public static void PrintAskHelp()
        {
            Console.WriteLine("Type 'help' to get commands list");
        }

        /// <summary>
        /// Print commands list
        /// </summary>
        public static void PrintCommandsList()
        {
            Console.WriteLine("\ncommand_name [param] : Utility.\n" +
                           "stop : Stop the server.\n" +
                           "status [online/maintenance or 0/1] : Set the server status.\n" +
                           "disable/enable [logging/security/console] : Disable or enable a setting." +
                           "\n");
        }

        /// <summary>
        /// Print a message in the console
        /// </summary>
        /// <param name="data">Message</param>
        public static void PrintMessage(string data)
        {
            if (!Settings.ENABLE_CONSOLE_PRINT)
                return;//Disable text to improve performance

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now}] - {data}");
        }

        /// <summary>
        /// Print incoming data in the console
        /// </summary>
        /// <param name="data">Received data</param>
        public static void PrintInData(Client client, string data)
        {
            if (!Settings.ENABLE_CONSOLE_PRINT)
                return;//Disable text to improve performance

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now}] {client.id} -> {data}");
        }

        /// <summary>
        /// Print sent data
        /// </summary>
        /// <param name="data">Sent data</param>
        public static void PrintOutData(Client client, string data)
        {
            if (!Settings.ENABLE_CONSOLE_PRINT)
                return;//Disable text to improve performance

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now}] {client.id} <- {data}");
        }

        /// <summary>
        /// Print error
        /// </summary>
        /// <param name="errorText">Error text</param>
        public static void PrintError(string errorText)
        {
            Logger.LogErrorInFile(errorText);

            if (!Settings.ENABLE_CONSOLE_PRINT)
                return;//Disable text to improve performance

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now}] {errorText}\n");
        }
    }
}
