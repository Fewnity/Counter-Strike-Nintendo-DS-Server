// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;
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

    public static class PartyManager
    {
        //Party settings
        public static List<Party> allParties = new();
        public static Dictionary<string, Party> partiesWithCode = new();
        public static List<PartyModeData> allPartyModesData = new();

        //All timer values
        public static DateTime roundChangeTime = new DateTime(2000, 1, 1, 0, 0, 0).AddSeconds(-1);
        public static DateTime quitPartyTime = new(2000, 1, 1, 0, 0, 20);
        public static DateTime startFullPartyWaitingTime = new(2000, 1, 1, 0, 0, 10);

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
                                currentParty.Stop();

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
                                    currentParty.EquilibrateTeams();

                                currentParty.Start();
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
                                        float Distance = (float)Math.Sqrt(Math.Pow(client.position.x - currentParty.bombPosition.x, 2f) + Math.Pow(client.position.y - currentParty.bombPosition.y, 2f) + Math.Pow(client.position.z - currentParty.bombPosition.z, 2f)) / 8096f;
                                        if (Distance > 19)
                                            Distance = 0;

                                        //If the distance is big enought
                                        if (Distance > 0)
                                        {
                                            //Apply damage to the player
                                            client.health -= (int)Program.Map(Distance, 0.3, 19, 200, 0);
                                            client.CheckAfterDamage(null, false, false);

                                            Thread TimedCallThread = new(() =>
                                            {
                                                Thread.Sleep(1000);
                                                client.communicator.SendHealth();
                                            });
                                            TimedCallThread.Start();
                                        }
                                    }

                                    currentParty.terroristsScore++;

                                    if (currentParty.loseCountCounterTerrorists > 0)
                                        currentParty.loseCountCounterTerrorists--;

                                    if (currentParty.loseCountTerrorists < 4)
                                        currentParty.loseCountTerrorists++;

                                    currentParty.communicator.SendText(TextEnum.TERRORISTS_WIN);

                                    //Add money to all teams
                                    currentParty.AddMoneyTo(currentParty.partyMode.winTheRoundBombMoney, TeamEnum.TERRORISTS);
                                    currentParty.AddMoneyTo(currentParty.partyMode.loseTheRoundMoney + currentParty.partyMode.loseIncrease * currentParty.loseCountTerrorists, TeamEnum.COUNTERTERRORISTS);
                                }
                                else
                                {
                                    currentParty.counterScore++;

                                    if (currentParty.loseCountTerrorists > 0)
                                        currentParty.loseCountTerrorists--;

                                    if (currentParty.loseCountCounterTerrorists < 4)
                                        currentParty.loseCountCounterTerrorists++;

                                    currentParty.communicator.SendText(TextEnum.COUNTER_TERRORISTS_WIN);

                                    //Add money
                                    if (currentParty.partyMode.noMoneyOnTimeEnd)
                                        currentParty.AddMoneyTo(0, 0);
                                    else
                                        currentParty.AddMoneyTo(currentParty.partyMode.loseTheRoundMoney, TeamEnum.TERRORISTS);

                                    currentParty.AddMoneyTo(currentParty.partyMode.winTheRoundMoney, TeamEnum.COUNTERTERRORISTS);
                                }

                                currentParty.CheckAfterRound();
                                //Send score to all clients
                                currentParty.communicator.SendScore();
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
                                    currentParty.EquilibrateTeams();

                                //Update players
                                currentParty.SetPlayerSpawns();
                                currentParty.SetBombForARandomPlayer();
                                currentParty.ResetPlayers();
                                currentParty.communicator.SendMoney();
                            }
                            else if (currentParty.roundState == RoundState.END) //Close party
                            {
                                currentParty.Stop();

                                i--;
                                partyCount--;
                                continue;
                            }

                            //Send round state to all clients
                            currentParty.communicator.SendPartyRound();
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
                                        currentClient.SetPlayerSpawn(true);
                                        currentClient.ResetPlayer();
                                    }
                                }
                            }

                            //Force start party if there are enought votes
                            if ((currentParty.allConnectedClients.Count <= 3 && forceCount >= 2) || (currentParty.allConnectedClients.Count > 3 && forceCount >= currentParty.allConnectedClients.Count - 2))
                            {
                                currentParty.Start();
                            }
                        }

                        if (currentParty.roundState == RoundState.END_ROUND)
                        {
                            //Send timer to all clients
                            currentParty.communicator.SendPartyTimer(0, 0);
                        }
                        else
                        {
                            //Send timer to all clients
                            currentParty.communicator.SendPartyTimer();
                        }
                    }
                }
                //Wait a second
                Thread.Sleep(1000);
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
            Party NewParty = new();
            NewParty.allConnectedClients.Add(currentClient);
            NewParty.partyMode = allPartyModesData[0];
            NewParty.partyTimer = NewParty.partyMode.trainingTime;
            NewParty.loseCountCounterTerrorists = 0;
            NewParty.loseCountTerrorists = 0;
            NewParty.mapId = mapEnum.DUST2;
            NewParty.password = password;
            NewParty.isPrivate = isPrivate;

            allParties.Add(NewParty);
            currentClient.party = NewParty;

            //Send timer
            NewParty.communicator.SendPartyTimer();
        }
    }
}
