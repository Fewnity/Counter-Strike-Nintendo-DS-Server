// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;

namespace Counter_Strike_Server
{
    static class Security
    {
        static readonly Random rand = new ();

        /// <summary>
        /// Verify the final client key by generating the final key on the server.
        /// !!! CAN KICK PLAYER !!!
        /// </summary>
        /// <param name="client">Client to check</param>
        /// <param name="clientKey"></param>
        /// <exception cref="Exception"></exception>
        public static void CheckClientKey(Client client, int clientKey)
        {
            if (Settings.ENABLE_SECURITY_KEY)
            {
                client.checkedKey = clientKey == GetKey(client.sentKey);
                if (!client.checkedKey)
                {
                    client.communicator.SendError(NetworkDataManager.ErrorType.WrongSecurityKey);
                    throw new Exception("Wrong key");
                }
            }
            else
            {
                client.checkedKey = true;
            }
        }

        /// <summary>
        /// Get final key from base key
        /// </summary>
        /// <param name="baseKey"></param>
        /// <returns>Final key</returns>
        private static int GetKey(int baseKey)
        {
            //Secret code, create you own
            //Same code as the game's security code
            //Generate a key from a base key, this new key is used to check if the game is an official version
            return -1;
        }

        /// <summary>
        /// Get a random base key
        /// </summary>
        /// <returns>Base key</returns>
        public static int GetBaseKey()
        {
            return rand.Next(99999, 9999999);
        }
    }
}
