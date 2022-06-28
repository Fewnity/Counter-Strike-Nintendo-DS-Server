// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Diagnostics;

namespace Counter_Strike_Server
{
    static class PlayerManager
    {
        /// <summary>
        /// Send step sound to all clients except client who send the step
        /// </summary>
        /// <param name="client"></param>
        public static void SendStep(Client client)
        {
            Call.CreateCall($"STEP;{client.id}", client.clientParty.allConnectedClients, client);
        }

        /// <summary>
        /// Set the position of a client (string input)
        /// </summary>
        /// <param name="client">Target client</param>
        /// <param name="xPos">X position</param>
        /// <param name="yPos">Y position</param>
        /// <param name="zPos">Z position</param>
        /// <param name="angle">Angle</param>
        /// <exception cref="Exception"></exception>
        public static void SetPosition(Client client, string xPos, string yPos, string zPos, string angle, string cameraAngle)
        {
            //Parse data
            Vector3Int newPos = new Vector3Int(int.Parse(xPos), int.Parse(yPos), int.Parse(zPos));

            //If the player is playing
            if (client.team != teamEnum.SPECTATOR && client.clientParty.roundState != RoundState.WAIT_START)
            {
                client.angle = int.Parse(angle);

                //Get camera angle
                client.cameraAngle = int.Parse(cameraAngle);
                //Limit the camera angle
                if (client.cameraAngle < 9)
                {
                    client.cameraAngle = 9;
                }
                else if (client.cameraAngle > 245)
                {
                    client.cameraAngle = 245;
                }

                //Security check : check if the player is not moving too fast
                if (Math.Abs(newPos.x - client.Position.x) < 3000 && Math.Abs(newPos.z - client.Position.z) < 3000)
                {
                    client.Position = newPos;
                }
                else
                {
                    //Sometime the player can move too fast (connection bug or server teleportation), so kick the player if the player has a high error rate
                    client.positionErrorCount++;
                    Debug.WriteLine("Wrong player positon warning");
                    if (client.positionErrorCount >= 40)
                    {
                        throw new Exception("Wrong player positon");
                    }
                    else if (client.positionErrorCount >= 6) //Do a rollback
                    {
                        SendPlayerPosition(client, true);
                    }
                }

                // If the player pass through the map, teleport the player
                if (newPos.y <= -5)
                {
                    SetPlayerSpawn(client, false);
                    //setPlayerPositionAtSpawns(0);
                }

                SendPlayerPosition(client, false);
            }
        }

        /// <summary>
        /// Set player name
        /// !!! CAN KICK PLAYER !!!
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="name">Client name</param>
        /// <exception cref="Exception"></exception>
        public static void SetName(Client client, string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 14)
            //if (string.IsNullOrWhiteSpace(name) || name.Length > 14) //TODO enable this, but check in game if the name is only space
            {
                ConnectionManager.SendError(client, NetworkDataManager.ErrorType.SaveCorrupted);
                throw new Exception("Name is too long or empty.");
            }
            client.name = name;

            //Send name to all clients
            if (client.clientParty != null)
                Call.CreateCall($"SETNAME;{client.id};{client.name}", client.clientParty.allConnectedClients, client);
        }

        /// <summary>
        /// Send clients position to the given client
        /// </summary>
        /// <param name="client"></param>
        public static void SendPlayersPositions(Client client)
        {
            for (int i = 0; i < client.clientParty.allConnectedClients.Count; i++)
            {
                Client tempClient = client.clientParty.allConnectedClients[i];
                if (client != tempClient)
                    Call.CreateCall($"POS;{tempClient.id};{tempClient.Position.x};{tempClient.Position.y};{tempClient.Position.z};{tempClient.angle};{tempClient.cameraAngle}", client);
            }
        }

        /// <summary>
        /// Send client position to clients
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="includeHim">Include the client in the clients list of the call?</param>
        public static void SendPlayerPosition(Client client, bool includeHim)
        {
            if (includeHim)
                Call.CreateCall($"POS;{client.id};{client.Position.x};{client.Position.y};{client.Position.z};{client.angle};{client.cameraAngle}", client.clientParty.allConnectedClients);
            else
                Call.CreateCall($"POS;{client.id};{client.Position.x};{client.Position.y};{client.Position.z};{client.angle};{client.cameraAngle}", client.clientParty.allConnectedClients, client);
        }

        /// <summary>
        /// Send clients position to all clients
        /// </summary>
        /// <param name="client"></param>
        public static void SendPlayersPositions(Party party)
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                Client client = party.allConnectedClients[i];
                Call.CreateCall($"POS;{client.id};{client.Position.x};{client.Position.y};{client.Position.z};{client.angle};{client.cameraAngle}", party.allConnectedClients);
            }
        }

        /// <summary>
        /// Check player health after damage
        /// </summary>
        /// <param name="killerClient">Client who shoot the client</param>
        /// <param name="HittedClient">Shooted client</param>
        /// <param name="CheckScore">Check if the server can add a point to a team</param>
        /// <param name="sendHealth">Send health to all clients?</param>
        public static void CheckAfterDamage(Client killerClient, Client HittedClient, bool CheckScore, bool sendHealth)
        {
            Party currentParty = HittedClient.clientParty;
            if (HittedClient.health <= 0)
                HittedClient.health = 0;

            if (sendHealth)
                SendHealth(HittedClient);

            if (HittedClient.health == 0 && !HittedClient.isDead)
            {
                //Set client has dead
                HittedClient.isDead = true;
                HittedClient.deathCount++;

                //Drop the bomb
                if (HittedClient.haveBomb)
                {
                    BombManager.PlaceBomb(HittedClient, HittedClient.Position.x, HittedClient.Position.y, HittedClient.Position.z, HittedClient.angle, true);
                }

                if (killerClient != null)
                {
                    //If the killer killed a team mate
                    if (killerClient.clientParty.partyMode.teamDamage && HittedClient.team == killerClient.team)
                    {
                        //Reduce kill count and money
                        killerClient.killCount--;
                        killerClient.money -= killerClient.clientParty.partyMode.killPenalties;
                        if (killerClient.money < 0)
                            killerClient.money = 0;

                        killerClient.killedFriend++;
                        if (killerClient.killedFriend == 2 && (currentParty.partyMode.roundTime - currentParty.partyTimer).TotalSeconds <= 10) //if the player has killed 2 friends but at the beginning kick the player
                        {
                            ConnectionManager.SendError(killerClient, NetworkDataManager.ErrorType.KickTeamKill);
                            killerClient.NeedRemoveConnection = true;
                        }
                        else if (killerClient.killedFriend >= 3) //if the player has killed 3 friends, kick the player
                        {
                            ConnectionManager.SendError(killerClient, NetworkDataManager.ErrorType.KickTeamKill);
                            killerClient.NeedRemoveConnection = true;
                        }
                        MoneyManager.SendMoney(killerClient);
                    }
                    else
                    {
                        //Add kill count and money
                        killerClient.killCount++;
                        if (ShopManager.allShopElements[killerClient.allGunsInInventory[killerClient.currentGunInInventory]] is Gun)
                            MoneyManager.AddMoneyTo(killerClient, ((Gun)ShopManager.allShopElements[killerClient.allGunsInInventory[killerClient.currentGunInInventory]]).killMoneyBonus[PartyManager.allPartyModesData.IndexOf(killerClient.clientParty.partyMode)]);
                        SendKillCountAndDeathCount(killerClient);
                    }

                    //Send kill text
                    Call.CreateCall($"TEXTPLAYER;{HittedClient.id};{killerClient.id};{0}", currentParty.allConnectedClients);
                }
                else
                {
                    //Send kill text
                    Call.CreateCall($"TEXTPLAYER;{HittedClient.id};{-1};{0}", currentParty.allConnectedClients);
                }

                SendKillCountAndDeathCount(HittedClient);

                if (!CheckScore)
                    return;

                OnPlayerKilled(currentParty, HittedClient);
            }
        }

        public static void OnPlayerKilled(Party currentParty, Client hittedClient)
        {
            if (currentParty.roundState == RoundState.PLAYING)//If the player is killed during the party
            {
                //Check if all a team is dead
                int CounterDeadCount, TerroristDeadCount, CounterTerroristsCount, TerroristsCount;

                TeamManager.CheckTeamDeathCount(currentParty, out TerroristsCount, out CounterTerroristsCount, out TerroristDeadCount, out CounterDeadCount);

                if (CounterDeadCount == CounterTerroristsCount)
                {
                    currentParty.terroristsScore++;

                    if (currentParty.loseCountTerrorists > 0)
                        currentParty.loseCountTerrorists--;

                    if (currentParty.loseCountCounterTerrorists < 4)
                        currentParty.loseCountCounterTerrorists++;

                    //Send score to all clients
                    PartyManager.SendScore(currentParty);

                    PartyManager.SendText(PartyManager.TextEnum.TERRORISTS_WIN, currentParty);

                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.winTheRoundMoney, teamEnum.TERRORISTS);
                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.loseTheRoundMoney + currentParty.partyMode.loseIncrease * currentParty.loseCountCounterTerrorists, teamEnum.COUNTERTERRORISTS);

                    //Set round to finished round state
                    currentParty.partyTimer = currentParty.partyMode.endRoundWaitingTime;
                    currentParty.roundState = RoundState.END_ROUND;
                    PartyManager.CheckAfterRound(currentParty);
                }
                else if (TerroristDeadCount == TerroristsCount && !currentParty.bombSet)
                {
                    currentParty.counterScore++;

                    if (currentParty.loseCountCounterTerrorists > 0)
                        currentParty.loseCountCounterTerrorists--;

                    if (currentParty.loseCountTerrorists < 4)
                        currentParty.loseCountTerrorists++;

                    //Send score to all clients
                    PartyManager.SendScore(currentParty);

                    PartyManager.SendText(PartyManager.TextEnum.COUNTER_TERRORISTS_WIN, currentParty);

                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.winTheRoundMoney, teamEnum.COUNTERTERRORISTS);
                    MoneyManager.AddMoneyTo(currentParty, currentParty.partyMode.loseTheRoundMoney + currentParty.partyMode.loseIncrease * currentParty.loseCountTerrorists, teamEnum.TERRORISTS);

                    //Set round to finished round state
                    currentParty.partyTimer = currentParty.partyMode.endRoundWaitingTime;
                    currentParty.roundState = RoundState.END_ROUND;
                    PartyManager.CheckAfterRound(currentParty);
                }
            }
            else if (!currentParty.partyStarted)//If the player is dead, respawn the player
            {
                if (hittedClient != null)
                {
                    hittedClient.respawnTimer = currentParty.partyMode.trainingRespawnTimer;
                    hittedClient.needRespawn = true;
                }
            }
        }

        /// <summary>
        /// Send the score of a client to all client
        /// </summary>
        /// <param name="CurrentClient"></param>
        public static void SendKillCountAndDeathCount(Client CurrentClient)
        {
            Call.CreateCall($"SCRBOARD;{CurrentClient.id};{CurrentClient.killCount};{CurrentClient.deathCount}", CurrentClient.clientParty.allConnectedClients);
        }


        /// <summary>
        /// Send the score of a client to all client
        /// </summary>
        /// <param name="party"></param>
        public static void SendKillCountAndDeathCount(Party party)
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                SendKillCountAndDeathCount(party.allConnectedClients[i]);
            }
        }

        /// <summary>
        /// Set spawns for players and send positions
        /// </summary>
        /// <param name="party">Party</param>
        public static void SetPlayerSpawns(Party party)
        {
            int TerroristsSpawn = 0, CounterTerroristsSpawn = 0;

            //for each players
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                Client client = party.allConnectedClients[i];
                client.positionErrorCount = 0;

                //Set spawn for player
                if (client.team == teamEnum.TERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)client.clientParty.mapId].allTerroristsSpawns[TerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)client.clientParty.mapId].allTerroristsSpawns[TerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)client.clientParty.mapId].allTerroristsSpawns[TerroristsSpawn].z * 4096);
                    int angle = MapManager.allMaps[(int)client.clientParty.mapId].terroristsSpawnsAngle;
                    client.Position = new Vector3Int(x, y, z);
                    client.angle = angle;

                    TerroristsSpawn++;
                }
                else if (client.team == teamEnum.COUNTERTERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)client.clientParty.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)client.clientParty.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)client.clientParty.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].z * 4096);
                    int angle = MapManager.allMaps[(int)client.clientParty.mapId].counterTerroristsSpawnsAngle;
                    client.Position = new Vector3Int(x, y, z);
                    client.angle = angle;
                    CounterTerroristsSpawn++;
                }
                else
                    continue;
            }

            SendPlayersPositions(party);
            SendSetShopZone(party);
        }

        /// <summary>
        /// Say to all players to set the shop zone
        /// </summary>
        /// <param name="party">Party</param>
        public static void SendSetShopZone(Party party)
        {
            Call.CreateCall("SETSHOPZONE", party.allConnectedClients);
        }

        /// <summary>
        /// Say to all players to set the shop zone
        /// </summary>
        /// <param name="client">Party</param>
        public static void SendSetShopZone(Client client)
        {
            Call.CreateCall("SETSHOPZONE", client);
        }


        /// <summary>
        /// Set spawns for the player and send position to all clients
        /// </summary>
        /// <param name="Player"></param>
        public static void SetPlayerSpawn(Client Player, bool defineShopZone)
        {
            int TerroristsSpawn = 0, CounterTerroristsSpawn = 0;

            Player.positionErrorCount = 0;

            //for each players
            for (int i = 0; i < Player.clientParty.allConnectedClients.Count; i++)
            {
                //Set spawn for player
                if (Player.clientParty.allConnectedClients[i].team == teamEnum.TERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)Player.clientParty.mapId].allTerroristsSpawns[TerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)Player.clientParty.mapId].allTerroristsSpawns[TerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)Player.clientParty.mapId].allTerroristsSpawns[TerroristsSpawn].z * 4096);
                    int angle = MapManager.allMaps[(int)Player.clientParty.mapId].terroristsSpawnsAngle;
                    Player.Position = new Vector3Int(x, y, z);
                    Player.angle = angle;

                    TerroristsSpawn++;
                }
                else if (Player.clientParty.allConnectedClients[i].team == teamEnum.COUNTERTERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)Player.clientParty.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)Player.clientParty.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)Player.clientParty.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].z * 4096);
                    int angle = MapManager.allMaps[(int)Player.clientParty.mapId].counterTerroristsSpawnsAngle;
                    Player.Position = new Vector3Int(x, y, z);
                    Player.angle = angle;

                    CounterTerroristsSpawn++;
                }
                else
                    continue;

                if (Player != Player.clientParty.allConnectedClients[i])
                    continue;

                break;
            }
            SendPlayerPosition(Player, true);
            if(defineShopZone)
            SendSetShopZone(Player);
        }

        /// <summary>
        /// Reset all players
        /// </summary>
        /// <param name="party">Party</param>
        public static void ResetPlayers(Party party)
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
                ResetPlayer(party.allConnectedClients[i]);
        }

        /// <summary>
        /// Send client health to all clients
        /// </summary>
        /// <param name="client"></param>
        public static void SendHealth(Client client)
        {
            Call.CreateCall($"SETHEALTH;{client.id};{client.health};{client.armor};{(client.haveHelmet ? 1 : 0)}", client.clientParty.allConnectedClients);
        }

        /// <summary>
        /// Reset a player
        /// </summary>
        /// <param name="client"></param>
        public static void ResetPlayer(Client client)
        {
            //Set health to the max
            client.health = 100;

            //Reset grenade bought count
            for (int i = 0; i < client.grenadeBought.Length; i++)
            {
                client.grenadeBought[i] = 0;
            }

            //If needed (if the player is dead or when the party start or swap the team)
            if (client.isDead || client.clientParty.counterScore + client.clientParty.terroristsScore == 0 || client.clientParty.partyMode.middlePartyTeamSwap && client.clientParty.terroristsScore + client.clientParty.counterScore == Math.Floor(client.clientParty.partyMode.maxRound / 2f))
            {
                //Remove equipment
                client.haveDefuseKit = false;
                client.armor = 0;
                client.haveHelmet = false;

                //Set default gun
                SetDefaultGun(client);

                //Remove all other gun
                for (int i = 2; i < InventoryManager.INVENTORY_SIZE - 1; i++)
                {
                    client.allGunsInInventory[i] = -1;
                }
            }
            else
            {
                //I don't know what the code does here
                //I think it's to count all bought grenade after the grenade count reset, maybe i can reset the grenade count if the player is dead or when the party start or swap the team
                for (int i = 0; i < ShopManager.SHOP_GRENADE_COUNT; i++)
                {
                    for (int grenadeCheckIndex = InventoryManager.INVENTORY_GRENADES_START_POSITION; grenadeCheckIndex < InventoryManager.INVENTORY_EQUIPMENTS_START_POSITION; grenadeCheckIndex++)
                    {
                        if (client.allGunsInInventory[grenadeCheckIndex] == ShopManager.SHOP_GUN_COUNT + i/* && inventoryIndex == -1 && AllClients[i].grenadeBought[ElementToBuy - GUNCOUNT] < ((Grenade)AllShopElements[ElementToBuy]).MaxQuantity[CurrentParty.PartyType]*/)
                            client.grenadeBought[i]++;
                    }
                }
            }

            //Put armor if needed
            if (client.clientParty.partyMode.spawnWithArmor)
            {
                client.armor = 100;
                client.haveHelmet = true;
            }

            InventoryManager.ResetGunsAmmo(client);

            client.isDead = false;

            SendHealth(client);

            InventoryManager.SendClientInventoryToClients(client);
        }

        public static void SetDefaultGun(Client client)
        {
            if (client.team == teamEnum.TERRORISTS)
                client.allGunsInInventory[1] = InventoryManager.DEFAULT_TERRORIST_GUN;
            else if (client.team == teamEnum.COUNTERTERRORISTS)
                client.allGunsInInventory[1] = InventoryManager.DEFAULT_COUNTERTER_TERRORIST_GUN;
        }
    }
}