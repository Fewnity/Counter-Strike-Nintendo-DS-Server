// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Counter_Strike_Server
{
    public enum PartyModeName
    {
        NO_DEFAULT_PARTY_MODE = -1,
        COMPETITIVE = 0,
        OCCASIONAL = 1,
        TUTORIAL_TRAINING = 2
    }

    public enum VoteType
    {
        ForceStart = 0,
    }

    static class PartyManager
    {
        //Party settings
        public static List<Party> allParties = new List<Party>();
        public static Dictionary<string, Party> partiesWithCode = new Dictionary<string, Party>();
        public static List<PartyModeData> allPartyModesData = new List<PartyModeData>();

        //All timer values
        public static DateTime roundChangeTime = new DateTime(2000, 1, 1, 0, 0, 0).AddSeconds(-1);
        public static DateTime quitPartyTime = new DateTime(2000, 1, 1, 0, 0, 20);
        public static DateTime startFullPartyWaitingTime = new DateTime(2000, 1, 1, 0, 0, 10);

        public enum TextEnum
        {
            TERRORISTS_WIN = 0,
            COUNTER_TERRORISTS_WIN = 1,
            BOMB_PLANTED = 2,
            BOMB_DEFUSED = 3,
        };

        /// <summary>
        /// Load party mode data
        /// </summary>
        public static void SetAllPartyModesData()
        {
            allPartyModesData.Add(new PartyModeData(true, 30, 800, 16000, 3250, 3500, 1400, 500, 300, 300, 800, 300, true, true));//Competitive
            PartyModeData currentPartyModeData = allPartyModesData[0];
            currentPartyModeData.trainingTime = new DateTime(2000, 1, 1, 0, 2, 0);
            currentPartyModeData.startRoundWaitingTime = new DateTime(2000, 1, 1, 0, 0, 15);
            currentPartyModeData.roundTime = new DateTime(2000, 1, 1, 0, 1, 55);
            currentPartyModeData.endRoundWaitingTime = new DateTime(2000, 1, 1, 0, 0, 5);
            currentPartyModeData.bombWaitingTime = new DateTime(2000, 1, 1, 0, 0, 40);
            currentPartyModeData.trainingRespawnTimer = new DateTime(2000, 1, 1, 0, 0, 5);

            allPartyModesData.Add(new PartyModeData(false, 15, 1000, 10000, 2700, 2700, 2400, 0, 200, 200, 200, 0, false, false));//Casual
            currentPartyModeData = allPartyModesData[1];
            currentPartyModeData.trainingTime = new DateTime(2000, 1, 1, 0, 0, 20);
            currentPartyModeData.startRoundWaitingTime = new DateTime(2000, 1, 1, 0, 0, 15);
            currentPartyModeData.roundTime = new DateTime(2000, 1, 1, 0, 2, 15);
            currentPartyModeData.endRoundWaitingTime = new DateTime(2000, 1, 1, 0, 0, 5);
            currentPartyModeData.bombWaitingTime = new DateTime(2000, 1, 1, 0, 0, 40);
            currentPartyModeData.trainingRespawnTimer = new DateTime(2000, 1, 1, 0, 0, 5);

            allPartyModesData.Add(new PartyModeData(false, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false));//Test
            currentPartyModeData = allPartyModesData[2];
            currentPartyModeData.trainingTime = new DateTime(2000, 1, 1, 0, 0, 0);
            currentPartyModeData.startRoundWaitingTime = new DateTime(2000, 1, 1, 0, 0, 0);
            currentPartyModeData.roundTime = new DateTime(2000, 1, 1, 0, 0, 0);
            currentPartyModeData.endRoundWaitingTime = new DateTime(2000, 1, 1, 0, 0, 0);
            currentPartyModeData.bombWaitingTime = new DateTime(2000, 1, 1, 0, 0, 40);
            currentPartyModeData.trainingRespawnTimer = new DateTime(2000, 1, 1, 0, 0, 0);
        }

        /// <summary>
        /// Send text to show to all client
        /// </summary>
        /// <param name="text">Text enum to show</param>
        /// <param name="party">Party</param>
        public static void SendText(TextEnum text, Party party)
        {
            Call.CreateCall($"TEXT;{(int)text}", party.allConnectedClients);
        }

        /// <summary>
        /// Show party score
        /// </summary>
        /// <param name="party">Party</param>
        public static void SendScore(Party party)
        {
            Call.CreateCall($"SCORE;{party.counterScore};{party.terroristsScore}", party.allConnectedClients);
        }

        /// <summary>
        /// Thread for party tick
        /// </summary>
        public static void PartyTimerTick()
        {
            while (true)
            {
                lock (allParties)
                {
                    //For each party
                    int partyCount = allParties.Count;
                    for (int i = 0; i < partyCount; i++)
                    {
                        Party currentParty = allParties[i];

                        if(currentParty.allConnectedClients.Count != 1)
                        {
                            //Reduce timer from 1 second
                            currentParty.partyTimer = currentParty.partyTimer.AddSeconds(-1);
                        }
                        else
                        {
                            //If there is only one player and if the party is already started, delete the party
                            if (currentParty.partyStarted)
                            {
                                StopParty(currentParty);

                                i--;
                                partyCount--;
                                continue;
                            }
                            currentParty.partyTimer = currentParty.partyMode.trainingTime;
                        }


                        //If timer is ended
                        if (currentParty.partyTimer == roundChangeTime)
                        {
                            if (!currentParty.partyStarted) //Start game and set wait for next round
                            {
                                currentParty.allGrenades.Clear();
                                if (currentParty.needTeamEquilibration)
                                    ConnectionManager.EquilibrateTeams(currentParty);

                                StartParty(currentParty);
                            }
                            else if (currentParty.roundState == RoundState.WAIT_START) //Set start round timer
                            {
                                currentParty.partyTimer = currentParty.partyMode.roundTime;
                                currentParty.roundState = RoundState.PLAYING;
                            }
                            else if (currentParty.roundState == RoundState.PLAYING) //Set end round timer
                            {
                                currentParty.partyTimer = currentParty.partyMode.endRoundWaitingTime;
                                currentParty.roundState = RoundState.END_ROUND;
                                //If the bomb is planted, make explode the bomb
                                if (currentParty.bombSet)
                                {
                                    //For each players
                                    for (int i2 = 0; i2 < currentParty.allConnectedClients.Count; i2++)
                                    {
                                        Client client = currentParty.allConnectedClients[i2];
                                        //Get distance between the bomb and the player
                                        float Distance = (float)Math.Sqrt(Math.Pow(client.Position.x - currentParty.bombPosition.x, 2f) + Math.Pow(client.Position.y - currentParty.bombPosition.y, 2f) + Math.Pow(client.Position.z - currentParty.bombPosition.z, 2f)) / 8096f;
                                        if (Distance > 19)
                                            Distance = 0;

                                        //If the distance is big enought
                                        if (Distance > 0)
                                        {
                                            //Apply damage to the player
                                            client.health -= (int)Program.map(Distance, 0.3, 19, 200, 0);
                                            PlayerManager.CheckAfterDamage(null, client, false, false);

                                            Thread TimedCallThread = new Thread(() =>
                                            {
                                                Thread.Sleep(1000);
                                                PlayerManager.SendHealth(client);
                                            });
                                            TimedCallThread.Start();
                                        }
                                    }

                                    currentParty.terroristsScore++;

                                    if (currentParty.loseCountCounterTerrorists > 0)
                                        currentParty.loseCountCounterTerrorists--;

                                    if (currentParty.loseCountTerrorists < 4)
                                        currentParty.loseCountTerrorists++;

                                    SendText(TextEnum.TERRORISTS_WIN, currentParty);

                                    //Add money to all teams
                                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.winTheRoundBombMoney, teamEnum.TERRORISTS);
                                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.loseTheRoundMoney + currentParty.partyMode.loseIncrease * currentParty.loseCountTerrorists, teamEnum.COUNTERTERRORISTS);
                                }
                                else
                                {
                                    currentParty.counterScore++;

                                    if (currentParty.loseCountTerrorists > 0)
                                        currentParty.loseCountTerrorists--;

                                    if (currentParty.loseCountCounterTerrorists < 4)
                                        currentParty.loseCountCounterTerrorists++;

                                    //Call.CreateCall($"TEXT;1", currentParty.allConnectedClients);
                                    SendText(TextEnum.COUNTER_TERRORISTS_WIN, currentParty);

                                    //Add money
                                    if (currentParty.partyMode.noMoneyOnTimeEnd)
                                        MoneyManager.AddMoneyTo(currentParty, 0, 0);
                                    else
                                        MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.loseTheRoundMoney, teamEnum.TERRORISTS);

                                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.winTheRoundMoney, teamEnum.COUNTERTERRORISTS);
                                }
                                CheckAfterRound(currentParty);
                                //Send score to all clients
                                SendScore(currentParty);
                            }
                            else if (currentParty.roundState == RoundState.END_ROUND) //Set wait for next round and teleport players to spawns
                            {
                                //Remove all grenades
                                currentParty.allGrenades.Clear();

                                //Reset party
                                currentParty.partyTimer = currentParty.partyMode.startRoundWaitingTime;
                                currentParty.roundState = RoundState.WAIT_START;
                                currentParty.bombSet = false;

                                if (currentParty.needTeamEquilibration)
                                    ConnectionManager.EquilibrateTeams(currentParty);

                                //Update players
                                PlayerManager.SetPlayerSpawns(currentParty);
                                BombManager.SetBombForARandomPlayer(currentParty);
                                PlayerManager.ResetPlayers(currentParty);
                                MoneyManager.SendMoney(currentParty);
                            }
                            else if (currentParty.roundState == RoundState.END) //Close party
                            {
                                StopParty(currentParty);

                                i--;
                                partyCount--;
                                continue;
                            }

                            //Send round state to all clients
                            SendPartyRound(currentParty);
                        }
                        else if (!currentParty.partyStarted) //If the party is not started
                        {
                            int forceCount = 0;
                            //Check every players
                            foreach (Client currentClient in currentParty.allConnectedClients)
                            {
                                //If he want to start now
                                if (currentClient.wantStartNow)
                                {
                                    forceCount++;
                                }

                                //Respawn player
                                if (currentClient.needRespawn)
                                {
                                    currentClient.respawnTimer = currentClient.respawnTimer.AddSeconds(-1);
                                    if (currentClient.respawnTimer == roundChangeTime)
                                    {
                                        currentClient.needRespawn = false;
                                        PlayerManager.SetPlayerSpawn(currentClient, true);
                                        PlayerManager.ResetPlayer(currentClient);
                                    }
                                }
                            }

                            //Force start party if there are enought votes
                            if ((currentParty.allConnectedClients.Count <= 3 && forceCount >= 2) || (currentParty.allConnectedClients.Count > 3 && forceCount >= currentParty.allConnectedClients.Count - 2))
                            {
                                StartParty(currentParty);
                            }
                        }

                        if (currentParty.roundState == RoundState.END_ROUND)
                        {
                            //Send timer to all clients
                            SendPartyTimer(currentParty, 0, 0);
                        }
                        else
                        {
                            //Send timer to all clients
                            SendPartyTimer(currentParty);
                        }
                    }
                }
                //Wait a second
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Send party timer
        /// </summary>
        /// <param name="party">Party</param>
        public static void SendPartyTimer(Party party)
        {
            SendPartyTimer(party, party.partyTimer.Minute, party.partyTimer.Second);
        }

        /// <summary>
        /// Send party timer
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="minutes">Minutes</param>
        /// <param name="seconds">Seconds</param>
        public static void SendPartyTimer(Party party, int minutes, int seconds)
        {
            Call.CreateCall($"TimerA;{minutes};{seconds}", party.allConnectedClients);
        }

        /// <summary>
        /// Stop the party
        /// </summary>
        /// <param name="currentParty">Party</param>
        public static void StopParty(Party currentParty)
        {
            int test = currentParty.allConnectedClients.Count;

            for (int KickPlayerIndex = 0; KickPlayerIndex < currentParty.allConnectedClients.Count; KickPlayerIndex++)
            {
                Call.SendDirectMessageToClient(currentParty.allConnectedClients[KickPlayerIndex], "ENDGAME");

                ConnectionManager.RemoveClient(currentParty.allConnectedClients[KickPlayerIndex], false);
                KickPlayerIndex--;
            }

            allParties.Remove(currentParty);
        }

        /// <summary>
        /// Send current vote result to all clients
        /// </summary>
        /// <param name="currentParty">Party</param>
        /// <param name="type">Vote type</param>
        public static void SendVoteResult(Party currentParty, VoteType type)
        {
            if (type == VoteType.ForceStart)
            {
                //Count every vote
                int forceCount = 0;
                foreach (Client currentClient in currentParty.allConnectedClients)
                {
                    if (currentClient.wantStartNow)
                    {
                        forceCount++;
                    }
                }
                int count = currentParty.allConnectedClients.Count - 2;
                if (currentParty.allConnectedClients.Count <= 3)
                {
                    count = 2;
                }

                //Send data
                Call.CreateCall($"VOTERESULT;{0};{forceCount};{count}", currentParty.allConnectedClients);
            }
        }

        /// <summary>
        /// Start a party
        /// </summary>
        /// <param name="currentParty"></param>
        public static void StartParty(Party currentParty)
        {
            currentParty.partyStarted = true;
            currentParty.roundState = 0;
            currentParty.partyTimer = currentParty.partyMode.startRoundWaitingTime;

            //Set values to all players

            TeamManager.AutoTeam(currentParty);
            PlayerManager.ResetPlayers(currentParty);

            foreach (Client currentClient in currentParty.allConnectedClients)
            {
                currentClient.money = currentParty.partyMode.startMoney;
                currentClient.killCount = 0;
                currentClient.deathCount = 0;
                currentClient.needRespawn = false;
                PlayerManager.SendKillCountAndDeathCount(currentClient);
            }

            MoneyManager.SendMoney(currentParty);
            SendPartyRound(currentParty);
            BombManager.SetBombForARandomPlayer(currentParty);

            //Send party has started to all clients
            
            PlayerManager.SetPlayerSpawns(currentParty);
        }

        /// <summary>
        /// Send party data to a client
        /// </summary>
        /// <param name="client">Client</param>
        public static void UpdatePartyToClient(Client client)
        {
            TeamManager.SendTeam(client);
            PlayerManager.SendKillCountAndDeathCount(client.clientParty);
            SendScore(client.clientParty);
            PlayerManager.SendPlayersPositions(client);
            InventoryManager.SendClientsCurrentGunToClient(client);
            InventoryManager.SendClientsAmmoToClient(client);
        }

        /// <summary>
        /// Check after a new round
        /// </summary>
        /// <param name="party"></param>
        public static void CheckAfterRound(Party party)
        {
            //If a team win
            if ((party.terroristsScore == Math.Floor(party.partyMode.maxRound / 2f) + 1) || (party.counterScore == Math.Floor(party.partyMode.maxRound / 2f) + 1) || (party.terroristsScore + party.counterScore == party.partyMode.maxRound))
            {
                party.partyTimer = quitPartyTime;
                party.roundState = RoundState.END;
                //Send round state to all clients
                SendPartyRound(party);
            }
            else if (party.partyMode.middlePartyTeamSwap && party.terroristsScore + party.counterScore == Math.Floor(party.partyMode.maxRound / 2f)) //If the party needs to swap
            {
                //Swap team
                for (int i = 0; i < party.allConnectedClients.Count; i++)
                {
                    if (party.allConnectedClients[i].team == teamEnum.TERRORISTS)
                    {
                        party.allConnectedClients[i].team = teamEnum.COUNTERTERRORISTS;
                    }
                    else if (party.allConnectedClients[i].team == teamEnum.COUNTERTERRORISTS)
                    {
                        party.allConnectedClients[i].team = teamEnum.TERRORISTS;
                    }

                    party.allConnectedClients[i].money = party.partyMode.startMoney;
                }
                party.loseCountCounterTerrorists = 0;
                party.loseCountTerrorists = 0;
                int TempsScoreTerrorists = party.terroristsScore;
                party.terroristsScore = party.counterScore;
                party.counterScore = TempsScoreTerrorists;
                SendScore(party);
                TeamManager.SendTeam(party);
            }
        }

        /// <summary>
        /// Send party current round to all clients
        /// </summary>
        /// <param name="party">Party</param>
        public static void SendPartyRound(Party party)
        {
            Call.CreateCall($"PartyRound;{(int)party.roundState}", party.allConnectedClients);
        }

        /// <summary>
        /// Send party current round to a client
        /// </summary>
        /// <param name="client">Client</param>
        public static void SendPartyRound(Client client)
        {
            Call.CreateCall($"PartyRound;{(int)client.clientParty.roundState}", client);
        }

        /// <summary>
        /// Put a client in a party
        /// </summary>
        /// <param name="CurrentClient">Client</param>
        /// <param name="code">Code of the private party</param>
        /// <exception cref="Exception"></exception>
        public static void PutClientIntoParty(Client CurrentClient, string code)
        {
            //Security check : Check the code
            if(!string.IsNullOrEmpty(code) && code.Length != 5)
            {
                ConnectionManager.SendError(CurrentClient, NetworkDataManager.ErrorType.IncorrectCode);
                throw new Exception("Code too long or too short.");
            }

            //check for a available party
            lock (allParties)
            {
                //Scan for a available party
                for (int i = 0; i < allParties.Count; i++)
                {
                    Party currentParty = allParties[i];

                    //If the party is not full and not started and if the party is private check the password
                    if (currentParty.allConnectedClients.Count < Settings.maxPlayerPerParty && ((!currentParty.partyStarted && !currentParty.isPrivate) || (currentParty.isPrivate && currentParty.password == code.ToUpper())))
                    {
                        currentParty.allConnectedClients.Add(CurrentClient);
                        CurrentClient.clientParty = currentParty;
                        return;
                    }
                }
            }

            //If there is no party found, create new one
            if (string.IsNullOrEmpty(code))
                CreateParty(CurrentClient, false);
            else //Or the password is just wrong
            {
                ConnectionManager.SendError(CurrentClient, NetworkDataManager.ErrorType.IncorrectCode);
                ConnectionManager.RemoveClient(CurrentClient, true);
            }
        }

        /// <summary>
        /// Create a party
        /// </summary>
        /// <param name="currentClient">Client</param>
        /// <param name="isPrivate">Is private</param>
        public static void CreateParty(Client currentClient, bool isPrivate)
        {
            //Generate a random password for the party
            string password;
            bool passwordOk;
            do
            {
                passwordOk = true;
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var stringChars = new char[5];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                password = new string(stringChars);
                foreach (Party party in allParties)
                {
                    if (password == party.password)
                    {
                        passwordOk = false;
                        continue;
                    }
                }
            } while (!passwordOk);

            //Create new party
            Party NewParty = new Party();
            NewParty.allConnectedClients.Add(currentClient);
            NewParty.partyMode = allPartyModesData[0];
            NewParty.partyTimer = NewParty.partyMode.trainingTime;
            NewParty.loseCountCounterTerrorists = 0;
            NewParty.loseCountTerrorists = 0;
            NewParty.mapId = mapEnum.DUST2;
            NewParty.password = password;
            NewParty.isPrivate = isPrivate;

            allParties.Add(NewParty);
            currentClient.clientParty = NewParty;

            //Send timer
            SendPartyTimer(NewParty);
        }

        /// <summary>
        /// Check is the party has no player
        /// </summary>
        /// <param name="party"></param>
        /// <returns>Can delete party?</returns>
        public static bool CheckEmptyParty(Party party)
        {
            bool Deleted = false;
            if (party.allConnectedClients.Count == 0)
            {
                allParties.Remove(party);
                Deleted = true;
            }
            return Deleted;
        }
    }
}
