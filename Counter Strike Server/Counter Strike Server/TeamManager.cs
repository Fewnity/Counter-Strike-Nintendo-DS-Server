// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using Counter_Strike_Server;
using System;

namespace Counter_Strike_Server
{
    public enum teamEnum
    {
        SPECTATOR = -1,
        BOTH = -1,
        TERRORISTS = 0,
        COUNTERTERRORISTS = 1
    };

    static class TeamManager
    {
        /// <summary>
        /// Set team for a client
        /// </summary>
        /// <param name="team"></param>
        /// <param name="client"></param>
        public static void SetTeam(string team, Client client)
        {
            teamEnum TempIsCounter = (teamEnum)int.Parse(team);
            SetTeam(TempIsCounter, client);
        }

        /// <summary>
        /// Set team for a client
        /// </summary>
        /// <param name="team"></param>
        /// <param name="client"></param>
        public static void SetTeam(teamEnum team, Client client)
        {
            if (client.team != team)
            {
                int TerroristsCount, CounterTerroristsCount;
                CheckTeamCount(client.clientParty, out TerroristsCount, out CounterTerroristsCount);

                if (team == teamEnum.COUNTERTERRORISTS && TerroristsCount >= CounterTerroristsCount)
                {
                    client.team = teamEnum.COUNTERTERRORISTS;
                }
                else if (team == teamEnum.TERRORISTS && CounterTerroristsCount >= TerroristsCount)
                {
                    client.team = teamEnum.TERRORISTS;
                }
                else
                {
                    //MainWindow.CreateCall(client.Id + $";ERROR;{0}", AllParties[idParty], client);
                }

                if (client.clientParty.roundState == RoundState.TRAINING) //////////////////////////////TODO RE ENABLE THIS
                {
                    PlayerManager.ResetPlayer(client);
                    PlayerManager.SetPlayerSpawn(client, true);
                }
                else
                {
                    client.health = 0;
                    client.isDead = true;
                    PlayerManager.SendHealth(client);
                }
                SendTeam(client.clientParty);
            }
        }

        /// <summary>
        /// Put all players in the party in a team
        /// </summary>
        /// <param name="party"></param>
        public static void AutoTeam(Party party)
        {
            int TerroristsCount = 0, CounterTerroristsCount = 0;

            CheckTeamCount(party, out TerroristsCount, out CounterTerroristsCount);

            //For every player in the party
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                //If the player is not in a team
                if (party.allConnectedClients[i].team == teamEnum.SPECTATOR)
                {
                    //If there are more terrorists than counter terrorists
                    if (TerroristsCount > CounterTerroristsCount)
                    {
                        //Put the player in counter terrorists team
                        CounterTerroristsCount++;
                        party.allConnectedClients[i].team = teamEnum.COUNTERTERRORISTS;
                    }
                    else if (CounterTerroristsCount > TerroristsCount)//If there are more counter terrorists than terrorists
                    {
                        //Put the player in terrorists team
                        TerroristsCount++;
                        party.allConnectedClients[i].team = teamEnum.TERRORISTS;
                    }
                    else //If terrorists count equals counter terrorists count
                    {
                        //Put the player in a random team
                        Random newRandom = new Random();
                        if (newRandom.Next(2) == 0)
                        {
                            CounterTerroristsCount++;
                            party.allConnectedClients[i].team = teamEnum.COUNTERTERRORISTS;
                        }
                        else
                        {
                            TerroristsCount++;
                            party.allConnectedClients[i].team = teamEnum.TERRORISTS;
                        }
                    }
                }
            }
            SendTeam(party);
        }

        /// <summary>
        /// Get each team players count
        /// </summary>
        /// <param name="party"></param>
        /// <param name="TerroristsCount"></param>
        /// <param name="CounterTerroristsCount"></param>
        public static void CheckTeamCount(Party party, out int TerroristsCount, out int CounterTerroristsCount)
        {
            TerroristsCount = 0;
            CounterTerroristsCount = 0;

            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                if (party.allConnectedClients[i].team == teamEnum.TERRORISTS)
                    TerroristsCount++;
                else if (party.allConnectedClients[i].team == teamEnum.COUNTERTERRORISTS)
                    CounterTerroristsCount++;
            }
        }

        /// <summary>
        /// Get each team players count and death players count of each team
        /// </summary>
        /// <param name="party"></param>
        /// <param name="TerroristsCount"></param>
        /// <param name="CounterTerroristsCount"></param>
        /// <param name="TerroristDeadCount"></param>
        /// <param name="CounterDeadCount"></param>
        public static void CheckTeamDeathCount(Party party, out int TerroristsCount, out int CounterTerroristsCount, out int TerroristDeadCount, out int CounterDeadCount)
        {
            CounterDeadCount = 0;
            TerroristDeadCount = 0;
            TerroristsCount = 0;
            CounterTerroristsCount = 0;

            //Check for each players in the party
            for (int iPlayer = 0; iPlayer < party.allConnectedClients.Count; iPlayer++)
            {
                if (party.allConnectedClients[iPlayer].team == teamEnum.COUNTERTERRORISTS)
                {
                    if (party.allConnectedClients[iPlayer].isDead)//If a counter is dead, add one to counter dead count
                        CounterDeadCount++;

                    CounterTerroristsCount++;
                }
                else if (party.allConnectedClients[iPlayer].team == teamEnum.TERRORISTS) //If a terrorist is dead, add one to terrorist dead count
                {
                    if (party.allConnectedClients[iPlayer].isDead)
                        TerroristDeadCount++;

                    TerroristsCount++;
                }
            }
        }

        /// <summary>
        /// Send players team to all client
        /// </summary>
        /// <param name="party"></param>
        public static void SendTeam(Party party)
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
                Call.CreateCall($"TEAM;{party.allConnectedClients[i].id};{(int)party.allConnectedClients[i].team}", party.allConnectedClients);
        }

        /// <summary>
        /// Send players team to the client
        /// </summary>
        /// <param name="client"></param>
        public static void SendTeam(Client client)
        {
            for (int i = 0; i < client.clientParty.allConnectedClients.Count; i++)
                Call.CreateCall($"TEAM;{client.clientParty.allConnectedClients[i].id};{(int)client.clientParty.allConnectedClients[i].team}", client);
        }
    }
}