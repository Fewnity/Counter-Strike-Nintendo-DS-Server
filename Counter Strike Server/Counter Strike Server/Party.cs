// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    public enum TeamEnum
    {
        SPECTATOR = -1,
        BOTH = -1,
        TERRORISTS = 0,
        COUNTERTERRORISTS = 1
    };

    public enum RoundState
    {
        TRAINING = -1,
        WAIT_START = 0,
        PLAYING = 1,
        END_ROUND = 2,
        END = 3,
    };

    public class Party
    {
        public List<Client> allConnectedClients = new();
        public PartyCommunicator communicator;
        public bool isPrivate;
        public string password;

        //Party data
        public PartyModeData partyMode;
        public bool partyStarted;
        public RoundState roundState = RoundState.TRAINING;
        public DateTime partyTimer;
        public bool bombSet;
        public bool bombDropped;
        public mapEnum mapId;
        
        public List<Physics.Grenade> allGrenades = new();
        
        //Bomb data
        public Vector4 bombPosition = new (0,0,0,0);
        public BoxCollisions defuseZoneCollisions;

        //Teams data
        public int counterScore;
        public int terroristsScore;
        public int loseCountCounterTerrorists;
        public int loseCountTerrorists;
        public bool needTeamEquilibration;

        public Party()
        {
            communicator = new(this);
        }

        #region Player management

        /// <summary>
        /// Set spawns for players and send positions
        /// </summary>
        /// <param name="party">Party</param>
        public void SetPlayerSpawns()
        {
            int TerroristsSpawn = 0, CounterTerroristsSpawn = 0;

            //for each players
            for (int i = 0; i < allConnectedClients.Count; i++)
            {
                Client client = allConnectedClients[i];
                client.positionErrorCount = 0;

                //Set spawn for player
                if (client.team == TeamEnum.TERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)client.party.mapId].allTerroristsSpawns[TerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)client.party.mapId].allTerroristsSpawns[TerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)client.party.mapId].allTerroristsSpawns[TerroristsSpawn].z * 4096);
                    int angle = MapManager.allMaps[(int)client.party.mapId].terroristsSpawnsAngle;
                    client.position = new Vector3Int(x, y, z);
                    client.angle = angle;

                    TerroristsSpawn++;
                }
                else if (client.team == TeamEnum.COUNTERTERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)client.party.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)client.party.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)client.party.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].z * 4096);
                    int angle = MapManager.allMaps[(int)client.party.mapId].counterTerroristsSpawnsAngle;
                    client.position = new Vector3Int(x, y, z);
                    client.angle = angle;
                    CounterTerroristsSpawn++;
                }
                else
                    continue;
            }

            communicator.SendPlayersPositions();
            communicator.SendSetShopZone();
        }

        /// <summary>
        /// Reset all players
        /// </summary>
        /// <param name="party">Party</param>
        public void ResetPlayers()
        {
            for (int i = 0; i < allConnectedClients.Count; i++)
                allConnectedClients[i].ResetPlayer();
        }

        /// <summary>
        /// Set the bomb to a random player
        /// </summary>
        /// <param name="party"></param>
        public void SetBombForARandomPlayer()
        {
            List<Client> allTerrorists = new();

            //Remove the bomb for all terrorists and add them in the allTerrorists list
            foreach (Client client in allConnectedClients)
            {
                if (client.team == TeamEnum.TERRORISTS)
                {
                    allTerrorists.Add(client);
                }
            }

            //If there is more than 0 terrorist
            if (allTerrorists.Count > 0)
            {
                //Give the bomb to a random one
                int randomTerrorist = new Random().Next(0, allTerrorists.Count);
                allTerrorists[randomTerrorist].inventory.SetBomb(true);
            }
        }

        /// <summary>
        /// Find a client by his id
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="PlayerId">Client id</param>
        /// <returns>Client or null if not found</returns>
        public Client FindClientById(int PlayerId)
        {
            Client FoundClient = null;
            for (int ClientIndex = 0; ClientIndex < allConnectedClients.Count; ClientIndex++)
            {
                if (allConnectedClients[ClientIndex].id == PlayerId)//If current client has the same id
                {
                    //Get this client
                    FoundClient = allConnectedClients[ClientIndex];
                    break;
                }
            }
            return FoundClient;
        }

        #endregion

        #region Other

        /// <summary>
        /// Check after a new round
        /// </summary>
        /// <param name="party"></param>
        public void CheckAfterRound()
        {
            //If a team win
            if ((terroristsScore == Math.Floor(partyMode.maxRound / 2f) + 1) || (counterScore == Math.Floor(partyMode.maxRound / 2f) + 1) || (terroristsScore + counterScore == partyMode.maxRound))
            {
                partyTimer = PartyManager.quitPartyTime;
                roundState = RoundState.END;
                //Send round state to all clients
                communicator.SendPartyRound();
            }
            else if (partyMode.middlePartyTeamSwap && terroristsScore + counterScore == Math.Floor(partyMode.maxRound / 2f)) //If the party needs to swap
            {
                //Swap team
                for (int i = 0; i < allConnectedClients.Count; i++)
                {
                    if (allConnectedClients[i].team == TeamEnum.TERRORISTS)
                    {
                        allConnectedClients[i].team = TeamEnum.COUNTERTERRORISTS;
                    }
                    else if (allConnectedClients[i].team == TeamEnum.COUNTERTERRORISTS)
                    {
                        allConnectedClients[i].team = TeamEnum.TERRORISTS;
                    }

                    allConnectedClients[i].money = partyMode.startMoney;
                }
                loseCountCounterTerrorists = 0;
                loseCountTerrorists = 0;
                int TempsScoreTerrorists = terroristsScore;
                terroristsScore = counterScore;
                counterScore = TempsScoreTerrorists;
                communicator.SendScore();
                communicator.SendTeam();
            }
        }

        /// <summary>
        /// Stop the party
        /// </summary>
        /// <param name="currentParty">Party</param>
        public void Stop()
        {
            for (int KickPlayerIndex = 0; KickPlayerIndex < allConnectedClients.Count; KickPlayerIndex++)
            {
                Call.Create("ENDGAME", allConnectedClients[KickPlayerIndex]);
                allConnectedClients[KickPlayerIndex].RemoveClient(false);
                KickPlayerIndex--;
            }

            PartyManager.allParties.Remove(this);
        }

        /// <summary>
        /// Start a party
        /// </summary>
        /// <param name="currentParty"></param>
        public void Start()
        {
            partyStarted = true;
            roundState = 0;
            partyTimer = partyMode.startRoundWaitingTime;

            //Set values to all players

            AutoTeam();
            ResetPlayers();

            foreach (Client currentClient in allConnectedClients)
            {
                currentClient.money = partyMode.startMoney;
                currentClient.killCount = 0;
                currentClient.deathCount = 0;
                currentClient.needRespawn = false;
                currentClient.communicator.SendKillCountAndDeathCount();
            }

            communicator.SendMoney();
            communicator.SendPartyRound();
            SetBombForARandomPlayer();

            SetPlayerSpawns();
        }

        /// <summary>
        /// Check is the party has no player
        /// </summary>
        /// <param name="party"></param>
        /// <returns>Can delete party?</returns>
        public bool CheckEmptyParty()
        {
            bool Deleted = false;
            if (allConnectedClients.Count == 0)
            {
                PartyManager.allParties.Remove(this);
                Deleted = true;
            }
            return Deleted;
        }

        #endregion

        #region Team management

        /// <summary>
        /// Put all players in the party in a team
        /// </summary>
        /// <param name="party"></param>
        public void AutoTeam()
        {
            CheckTeamCount(out int TerroristsCount, out int CounterTerroristsCount);

            //For every player in the party
            for (int i = 0; i < allConnectedClients.Count; i++)
            {
                //If the player is not in a team
                if (allConnectedClients[i].team == TeamEnum.SPECTATOR)
                {
                    //If there are more terrorists than counter terrorists
                    if (TerroristsCount > CounterTerroristsCount)
                    {
                        //Put the player in counter terrorists team
                        CounterTerroristsCount++;
                        allConnectedClients[i].team = TeamEnum.COUNTERTERRORISTS;
                    }
                    else if (CounterTerroristsCount > TerroristsCount)//If there are more counter terrorists than terrorists
                    {
                        //Put the player in terrorists team
                        TerroristsCount++;
                        allConnectedClients[i].team = TeamEnum.TERRORISTS;
                    }
                    else //If terrorists count equals counter terrorists count
                    {
                        //Put the player in a random team
                        Random newRandom = new ();
                        if (newRandom.Next(2) == 0)
                        {
                            CounterTerroristsCount++;
                            allConnectedClients[i].team = TeamEnum.COUNTERTERRORISTS;
                        }
                        else
                        {
                            TerroristsCount++;
                            allConnectedClients[i].team = TeamEnum.TERRORISTS;
                        }
                    }
                }
            }
            communicator.SendTeam();
        }

        /// <summary>
        /// Get each team players count
        /// </summary>
        /// <param name="party"></param>
        /// <param name="TerroristsCount"></param>
        /// <param name="CounterTerroristsCount"></param>
        public void CheckTeamCount(out int TerroristsCount, out int CounterTerroristsCount)
        {
            TerroristsCount = 0;
            CounterTerroristsCount = 0;

            for (int i = 0; i < allConnectedClients.Count; i++)
            {
                if (allConnectedClients[i].team == TeamEnum.TERRORISTS)
                    TerroristsCount++;
                else if (allConnectedClients[i].team == TeamEnum.COUNTERTERRORISTS)
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
        public void CheckTeamDeathCount(out int TerroristsCount, out int CounterTerroristsCount, out int TerroristDeadCount, out int CounterDeadCount)
        {
            CounterDeadCount = 0;
            TerroristDeadCount = 0;
            TerroristsCount = 0;
            CounterTerroristsCount = 0;

            //Check for each players in the party
            for (int iPlayer = 0; iPlayer < allConnectedClients.Count; iPlayer++)
            {
                if (allConnectedClients[iPlayer].team == TeamEnum.COUNTERTERRORISTS)
                {
                    if (allConnectedClients[iPlayer].isDead)//If a counter is dead, add one to counter dead count
                        CounterDeadCount++;

                    CounterTerroristsCount++;
                }
                else if (allConnectedClients[iPlayer].team == TeamEnum.TERRORISTS) //If a terrorist is dead, add one to terrorist dead count
                {
                    if (allConnectedClients[iPlayer].isDead)
                        TerroristDeadCount++;

                    TerroristsCount++;
                }
            }
        }

        /// <summary>
        /// Equilibrate teams
        /// </summary>
        /// <param name="party"></param>
        public void EquilibrateTeams()
        {
            needTeamEquilibration = false;
            //If there is enought players to equilibrate the party
            if (allConnectedClients.Count >= 3)
            {
                //Get count
                CheckTeamCount(out int terroristsCount, out int couterTerroristsCount);
                Random rd = new();

                while (Math.Abs(couterTerroristsCount - terroristsCount) >= 2)
                {
                    TeamEnum teamToEquilibrate;
                    TeamEnum teamToReduce;
                    if (couterTerroristsCount > terroristsCount)
                    {
                        couterTerroristsCount--;
                        terroristsCount++;
                        teamToEquilibrate = TeamEnum.TERRORISTS;
                        teamToReduce = TeamEnum.COUNTERTERRORISTS;
                    }
                    else
                    {
                        couterTerroristsCount++;
                        terroristsCount--;
                        teamToEquilibrate = TeamEnum.COUNTERTERRORISTS;
                        teamToReduce = TeamEnum.TERRORISTS;
                    }

                    //Pick a random player from the team to reduce
                    Client client;
                    int rdNb;
                    do
                    {
                        rdNb = rd.Next(allConnectedClients.Count);
                        client = allConnectedClients[rdNb];
                    } while (client.team != teamToReduce);

                    //Set the new team of the player
                    client.SetTeam(teamToEquilibrate);
                    client.inventory.ClearInventory();
                }
            }
        }

        /// <summary>
        /// Check if there is an empty team to stop the party
        /// </summary>
        /// <param name="party"></param>
        public void CheckIfThereIsEmptyTeam()
        {
            //If there is not enought players to continue the party (if players are in the same team)
            if (allConnectedClients.Count == 2)
            {
                //Get count
                CheckTeamCount(out int terroristsCount, out int couterTerroristsCount);
                if (Math.Abs(couterTerroristsCount - terroristsCount) == 2)
                {
                    //Finish the party
                    Stop();
                }
            }
        }

        /// <summary>
        /// Check if teams need to be equilibrated
        /// </summary>
        /// <param name="party"></param>
        public void CheckIfTeamsAreEquilibrated()
        {
            //If there is enought players to equilibrate the party
            if (allConnectedClients.Count >= 3)
            {
                //Get count
                CheckTeamCount(out int terroristsCount, out int couterTerroristsCount);
                //If there are to much difference in the player count
                if (Math.Abs(couterTerroristsCount - terroristsCount) >= 2)
                {
                    needTeamEquilibration = true;
                }
                else
                {
                    needTeamEquilibration = false;
                }
            }
            else
            {
                needTeamEquilibration = false;
            }
        }

        /// <summary>
        /// Add money to all players of a team
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="Money">Money to send</param>
        /// <param name="Team">Team</param>
        public void AddMoneyTo(int Money, TeamEnum Team)
        {
            foreach (Client client in allConnectedClients)
            {
                if (client.team == Team)
                {
                    client.money += Money;
                    if (client.money > client.party.partyMode.maxMoney)
                        client.money = client.party.partyMode.maxMoney;
                    client.communicator.SendMoney();
                }
            }
        }

        #endregion
    }
}
