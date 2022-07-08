// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.IO;

namespace Counter_Strike_Server
{
    /// <summary>
    /// Used to log messages in a file for debugging
    /// </summary>
    public class Logger
    {
        public const string folderName = "cs_log/";
        public const string fileName = "cs_log.txt";
        public static string fullPath = "";

        /// <summary>
        /// Create the log folder if not exists
        /// </summary>
        /// <param name="folder"></param>
        public static void CreateLogFile(string folder)
        {
            if (!Settings.ENABLE_LOGGING)
                return;

            fullPath = folder + folderName + fileName;
            try
            {
                if (!Directory.Exists(folder + folderName))
                    Directory.CreateDirectory(folder + folderName);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Log an error in the log file
        /// </summary>
        /// <param name="text"></param>
        public static void LogErrorInFile(string text)
        {
            if (!Settings.ENABLE_LOGGING)
                return;

            StreamWriter sw = null;
            try
            {
                //Open steam (append mode), write and close stream
                sw = new StreamWriter(fullPath, true);
                sw.WriteLine($"ERROR [{DateTime.Now}] {text}\n");
            }
            catch (Exception)
            {

            }
            finally
            {
                if(sw != null)
                {
                    sw.Close();
                }
            }
        }
    }
}
