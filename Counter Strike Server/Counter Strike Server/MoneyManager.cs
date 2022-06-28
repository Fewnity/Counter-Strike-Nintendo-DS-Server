// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

namespace Counter_Strike_Server
{
    static class MoneyManager
    {
        /// <summary>
        /// Add money to a client and send money to all players
        /// </summary>
        /// <param name="client">Client to add money</param>
        /// <param name="Money">Money to add</param>
        public static void AddMoneyTo(Client client, int Money)
        {
            client.money += Money;

            //Check if the money is too high, set player money to the max capacity
            if (client.money > client.clientParty.partyMode.maxMoney)
                client.money = client.clientParty.partyMode.maxMoney;

            SendMoney(client);
        }

        /// <summary>
        /// Add money to all players of a team
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="Money">Money to send</param>
        /// <param name="Team">Team</param>
        public static void AddMoneyTo(Party party, int Money, teamEnum Team)
        {
            foreach (Client client in party.allConnectedClients)
            {
                if (client.team == Team)
                {
                    client.money += Money;
                    if (client.money > client.clientParty.partyMode.maxMoney)
                        client.money = client.clientParty.partyMode.maxMoney;
                    SendMoney(client);
                }
            }
        }

        /// <summary>
        /// Send money of each players to all players
        /// </summary>
        /// <param name="party"></param>
        public static void SendMoney(Party party)
        {
            //Send teams info to all players
            for (int i = 0; i < party.allConnectedClients.Count; i++)
                Call.CreateCall($"SETMONEY;{party.allConnectedClients[i].money}", party.allConnectedClients[i]);
        }

        /// <summary>
        /// Send money of a client to this client
        /// </summary>
        /// <param name="client">Client</param>
        public static void SendMoney(Client client)
        {
            //Send teams info to all players
            Call.CreateCall($"SETMONEY;{client.money}", client);
        }
    }
}
