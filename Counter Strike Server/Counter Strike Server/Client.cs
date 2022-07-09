// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace Counter_Strike_Server
{
    /// <summary>
    /// A client is a player or a web browser
    /// </summary>
    public class Client
    {
        //Client connections
        public TcpClient currentClientTcp;
        public NetworkStream currentClientStream;
        public string data;
        public string ip;
        public bool NeedRemoveConnection;
        public DateTime lastPing = DateTime.Now;
        public int ping = 0;
        public int sentKey = 0;
        public bool checkedKey = false;
        public ClientCommunicator communicator;


        //player informations
        public string macAddress = "null";
        public string name = "player";
        public int id;

        public TeamEnum team = TeamEnum.SPECTATOR; //-1 no team, 0 terrorist, 1 counter terrorist
        public bool isDead;
        public int health;
        public int armor;
        public int money;
        public int killCount;
        public int deathCount;
        public Inventory inventory;

        public bool needRespawn;
        public DateTime respawnTimer = new(2000, 1, 1, 0, 0, 0);
        public int positionErrorCount = 0;
        public Party party;
        public Vector3Int position = new(0, -100 * 4096, 0);
        public int angle = 0;
        public float cameraAngle = 128;
        public bool wantStartNow = false;
        public int killedFriend = 0;
        public bool cancelNextHit = false;
        public int lastFrameCount = 0;
        public bool removed = false;

        public Client()
        {
            inventory = new(this);
            communicator = new(this);
        }

        #region Position management

        /// <summary>
        /// Set spawn for the client and send position to all clients
        /// </summary>
        /// <param name="defineShopZone">Need to update shop zone of the client?</param>
        public void SetPlayerSpawn(bool defineShopZone)
        {
            int TerroristsSpawn = 0, CounterTerroristsSpawn = 0;

            positionErrorCount = 0;

            //for each players
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                //Set spawn for player
                if (party.allConnectedClients[i].team == TeamEnum.TERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)party.mapId].allTerroristsSpawns[TerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)party.mapId].allTerroristsSpawns[TerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)party.mapId].allTerroristsSpawns[TerroristsSpawn].z * 4096);

                    position = new Vector3Int(x, y, z);
                    angle = MapManager.allMaps[(int)party.mapId].terroristsSpawnsAngle;

                    TerroristsSpawn++;
                }
                else if (party.allConnectedClients[i].team == TeamEnum.COUNTERTERRORISTS)
                {
                    int x = (int)(MapManager.allMaps[(int)party.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].x * 4096);
                    int y = (int)(MapManager.allMaps[(int)party.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].y * 4096);
                    int z = (int)(MapManager.allMaps[(int)party.mapId].allCounterTerroristsSpawns[CounterTerroristsSpawn].z * 4096);

                    position = new Vector3Int(x, y, z);
                    angle = MapManager.allMaps[(int)party.mapId].counterTerroristsSpawnsAngle;

                    CounterTerroristsSpawn++;
                }
                else
                    continue;

                if (this != party.allConnectedClients[i])
                    continue;

                break;
            }

            communicator.SendPlayerPosition(true);
            if (defineShopZone)
                communicator.SendSetShopZone();
        }

        /// <summary>
        /// Set the position of the client (string input)
        /// </summary>
        /// <param name="xPos">X position</param>
        /// <param name="yPos">Y position</param>
        /// <param name="zPos">Z position</param>
        /// <param name="angle">Player angle</param>
        /// <param name="cameraAngle">Camera angle</param>
        /// <exception cref="Exception"></exception>
        public void SetPosition(string xPos, string yPos, string zPos, string angle, string cameraAngle)
        {
            //If the player is playing
            if (!isDead && team != TeamEnum.SPECTATOR && party.roundState != RoundState.WAIT_START)
            {
                //Parse data
                Vector3Int newPos = new(int.Parse(xPos), int.Parse(yPos), int.Parse(zPos));
                this.angle = int.Parse(angle);

                //Get camera angle
                this.cameraAngle = int.Parse(cameraAngle);
                //Limit the camera angle
                if (this.cameraAngle < 9)
                {
                    this.cameraAngle = 9;
                }
                else if (this.cameraAngle > 245)
                {
                    this.cameraAngle = 245;
                }

                //Security check : check if the player is not moving too fast
                if (Math.Abs(newPos.x - position.x) < 3000 && Math.Abs(newPos.z - position.z) < 3000)
                {
                    position = newPos;
                }
                else
                {
                    //Sometime the player can move too fast (connection bug or server teleportation), so kick the player if the player has a high error rate
                    positionErrorCount++;
                    Debug.WriteLine("Wrong player positon warning");
                    if (positionErrorCount >= 40)
                    {
                        throw new Exception("Wrong player positon");
                    }
                    else if (positionErrorCount >= 6) //Do a rollback
                    {
                        communicator.SendPlayerPosition(true);
                    }
                }

                // If the player pass through the map, teleport the player
                if (newPos.y <= -5)
                {
                    SetPlayerSpawn(false);
                }

                communicator.SendPlayerPosition(false);
            }
        }

        #endregion

        #region Party management

        /// <summary>
        /// Send party data to the client
        /// </summary>
        public void UpdatePartyToClient()
        {
            communicator.SendTeam();
            party.communicator.SendKillCountAndDeathCount();
            party.communicator.SendScore();
            communicator.SendPlayersPositions();
            communicator.SendClientsCurrentGunToClient();
            communicator.SendClientsAmmoToClient();
        }

        /// <summary>
        /// Put the client in a party
        /// </summary>
        /// <param name="code">Code of the private party</param>
        /// <exception cref="Exception"></exception>
        public void PutClientIntoParty(string code)
        {
            //Security check : Check the code
            if (!string.IsNullOrEmpty(code) && code.Length != 5)
            {
                communicator.SendError(NetworkDataManager.ErrorType.IncorrectCode);
                throw new Exception("Code too long or too short.");
            }

            //check for a available party
            lock (PartyManager.allParties)
            {
                //Scan for a available party
                for (int i = 0; i < PartyManager.allParties.Count; i++)
                {
                    Party currentParty = PartyManager.allParties[i];

                    //If the party is not full and not started and if the party is private check the password
                    if (currentParty.allConnectedClients.Count < Settings.maxPlayerPerParty && ((!currentParty.partyStarted && !currentParty.isPrivate) || (currentParty.isPrivate && currentParty.password == code.ToUpper())))
                    {
                        currentParty.allConnectedClients.Add(this);
                        party = currentParty;
                        return;
                    }
                }
            }

            //If there is no party found, create new one
            if (string.IsNullOrEmpty(code))
                PartyManager.CreateParty(this, false);
            else //Or the password is just wrong
            {
                communicator.SendError(NetworkDataManager.ErrorType.IncorrectCode);
                RemoveClient(true);
            }
        }

        /// <summary>
        /// Remove the client from the party
        /// </summary>
        /// <param name="notifyClients">Notify the client of the disconnection</param>
        /// <returns>Return if the client party is deleted now</returns>
        public bool RemoveClient(bool notifyClients)
        {
            //Check if the client was already removed from the server
            if (!removed)
            {
                removed = true;
                //Notify the client of the disconnection
                if (party != null && notifyClients)
                {
                    Call.Create($"LEAVE;{id}", party.allConnectedClients);
                }

                //Remove the client connection count of the total connection count of his ip
                try
                {
                    if (!string.IsNullOrWhiteSpace(ip) || ip != "null")
                        ConnectionManager.connectionCount[ConnectionManager.connectedIps.IndexOf(ip)]--;
                }
                catch (Exception e)
                {
                    Logger.LogErrorInFile($"Not able to reduce client's connection count (client IP : {ip}) {e.Message} {e.StackTrace}");
                }

                //Close stream
                currentClientStream.Close();
                currentClientTcp.Close();
                ConnectionManager.allClients.Remove(this);

                //Remove the client from his party
                if (party != null)
                {
                    party.allConnectedClients.Remove(this);
                    OnPlayerKilled();

                    //Update vote
                    if (!party.partyStarted)
                    {
                        party.communicator.SendVoteResult(VoteType.ForceStart);
                    }
                    else
                    {
                        party.CheckIfTeamsAreEquilibrated();
                        party.CheckIfThereIsEmptyTeam();
                    }

                    lock (PartyManager.allParties)
                    {
                        return party.CheckEmptyParty();
                    }
                }
            }

            return false;
        }

        #endregion

        #region Team management

        /// <summary>
        /// Set team for the client
        /// </summary>
        /// <param name="team"></param>
        public void SetTeam(string team)
        {
            TeamEnum TempIsCounter = (TeamEnum)int.Parse(team);
            SetTeam(TempIsCounter);
        }

        /// <summary>
        /// Set team for the client
        /// </summary>
        /// <param name="team"></param>
        public void SetTeam(TeamEnum team)
        {
            if (this.team != team)
            {
                party.CheckTeamCount(out int TerroristsCount, out int CounterTerroristsCount);

                if (team == TeamEnum.COUNTERTERRORISTS && TerroristsCount >= CounterTerroristsCount)
                {
                    this.team = TeamEnum.COUNTERTERRORISTS;
                }
                else if (team == TeamEnum.TERRORISTS && CounterTerroristsCount >= TerroristsCount)
                {
                    this.team = TeamEnum.TERRORISTS;
                }
                else//The client is trying to be a spectator
                {

                }

                if (party.roundState == RoundState.TRAINING) //////////////////////////////TODO RE ENABLE THIS
                {
                    ResetPlayer();
                    SetPlayerSpawn(true);
                }
                else
                {
                    health = 0;
                    isDead = true;
                    communicator.SendHealth();
                }
                party.communicator.SendTeam();
            }
        }

        #endregion

        #region Bomb management

        /// <summary>
        /// The client places the bomb at the position
        /// </summary>
        /// <param name="xPos">X position of the bomb (text)</param>
        /// <param name="yPos">Y position of the bomb (text)</param>
        /// <param name="zPos">Z position of the bomb (text)</param>
        /// <param name="angle">Angle of the bomb (text)</param>
        /// <param name="drop">Is the bomb dropped?</param>
        public void PlaceBomb(string xPos, string yPos, string zPos, string angle, bool drop)
        {
            PlaceBomb(int.Parse(xPos), int.Parse(yPos), int.Parse(zPos), int.Parse(angle), drop);
        }

        /// <summary>
        ///  The client places the bomb at the position
        /// </summary>
        /// <param name="xPos">X position of the bomb</param>
        /// <param name="yPos">Y position of the bomb</param>
        /// <param name="zPos">Z position of the bomb</param>
        /// <param name="angle">Angle of the bomb</param>
        /// <param name="drop">Is the bomb dropped?</param>
        public void PlaceBomb(int xPos, int yPos, int zPos, int angle, bool drop)
        {
            if (inventory.haveBomb && (drop || !isDead))
            {
                bool CanPut = false;
                if (drop)
                {
                    party.bombDropped = true;
                    //Add some offset to the bomb
                    yPos -= (int)(0.845 * 4096);
                    CanPut = true;
                }
                else if (PhysicsManager.CheckBombZone(position, MapManager.allMaps[(int)party.mapId]))
                {
                    CanPut = true;

                    //Set bomb timer
                    party.partyTimer = party.partyMode.bombWaitingTime;
                    party.bombSet = true;
                    party.bombDropped = false;

                    //Send data about the bomb placement
                    party.communicator.SendText(PartyManager.TextEnum.BOMB_PLANTED);

                    //Add money to the client
                    AddMoneyTo(party.partyMode.plantBombMoneyBonus);
                }

                if (CanPut)
                {
                    //Set bomb position
                    party.bombPosition.x = xPos;
                    party.bombPosition.y = yPos;
                    party.bombPosition.z = zPos;
                    party.bombPosition.w = angle;

                    if (!drop)
                        MapManager.SetBombDefuseZone(party);
                    else
                        MapManager.SetBombDropZone(party);

                    party.communicator.SendBombPosition();

                    //Remove client's bomb
                    inventory.SetBomb(false);
                }
            }
        }

        /// <summary>
        /// Defuse the bomb
        /// </summary>
        public void DefuseBomb()
        {
            //Security check
            if (!isDead && team == TeamEnum.COUNTERTERRORISTS && party.roundState == RoundState.PLAYING && party.bombSet && PhysicsManager.CheckBombDefuseZone(position, party))
            {
                party.counterScore++;

                if (party.loseCountCounterTerrorists > 0)
                    party.loseCountCounterTerrorists--;

                if (party.loseCountTerrorists < 4)
                    party.loseCountTerrorists++;

                party.communicator.SendText(PartyManager.TextEnum.BOMB_DEFUSED);

                //Send score to all clients
                party.communicator.SendScore();
                Call.Create("BOMBDEFUSE", party.allConnectedClients);

                //Add money to defuser client and to teams
                AddMoneyTo(party.partyMode.defuseBombMoneyBonus);
                party.AddMoneyTo(party.partyMode.winTheRoundBombMoney, TeamEnum.COUNTERTERRORISTS);
                party.AddMoneyTo(party.partyMode.loseTheRoundMoney + party.partyMode.loseIncrease * party.loseCountTerrorists + party.partyMode.plantedBombLoseMoneyBonus, TeamEnum.TERRORISTS);

                //Set round to finished round state
                party.partyTimer = party.partyMode.endRoundWaitingTime;
                party.roundState = RoundState.END_ROUND;
                party.CheckAfterRound();
            }
        }

        /// <summary>
        /// Get the bomb from ground
        /// </summary>
        public void GetBomb()
        {
            //Security check
            if (!isDead && team == TeamEnum.TERRORISTS && party.bombDropped && PhysicsManager.CheckBombDefuseZone(position, party))
            {
                inventory.SetBomb(true);
                party.bombDropped = false;
            }
        }

        #endregion

        #region Security

        /// <summary>
        /// Check if client informations are valid
        /// </summary>
        /// <param name="macAddress">Client mac adress</param>
        /// <param name="version">Client version</param>
        /// <exception cref="Exception"></exception>
        public void CheckPlayerInfo(string macAddress, string version)
        {
            if (string.IsNullOrEmpty(version))//If the mac address of the nintendo ds is banned
            {
                communicator.SendError(NetworkDataManager.ErrorType.WrongVersion);
                throw new Exception("The game version is missing");
            }
            else if (!Settings.GAME_VERSIONS.Contains(version))            //Check is the game version match with the server version
            {
                communicator.SendError(NetworkDataManager.ErrorType.WrongVersion);
                throw new Exception("Wrong game version");
            }
            else if (string.IsNullOrEmpty(macAddress))//If the mac address of the nintendo ds is banned
            {
                communicator.SendError(NetworkDataManager.ErrorType.MacAddressMissing);
                throw new Exception("The MAC address is missing");
            }
            else if (macAddress.Length > 14)//If the mac address of the nintendo ds is banned
            {
                communicator.SendError(NetworkDataManager.ErrorType.MacAddressMissing);
                throw new Exception("The MAC address is wrong");
            }
            else if (ConnectionManager.bannedMac.Contains(macAddress))//If the mac address of the nintendo ds is banned
            {
                communicator.SendError(NetworkDataManager.ErrorType.Ban);
                throw new Exception("A banned client tried to connect");
            }

            //Get Mac address from data
            this.macAddress = macAddress;
        }

        #endregion

        #region Damage/Health management

        /// <summary>
        /// Called after killing the player
        /// </summary>
        public void OnPlayerKilled()
        {
            if (party.roundState == RoundState.PLAYING)//If the player is killed during the party
            {
                //Check if all a team is dead

                party.CheckTeamDeathCount(out int TerroristsCount, out int CounterTerroristsCount, out int TerroristDeadCount, out int CounterDeadCount);

                if (CounterDeadCount == CounterTerroristsCount)
                {
                    party.terroristsScore++;

                    if (party.loseCountTerrorists > 0)
                        party.loseCountTerrorists--;

                    if (party.loseCountCounterTerrorists < 4)
                        party.loseCountCounterTerrorists++;

                    //Send score to all clients
                    party.communicator.SendScore();

                    party.communicator.SendText(PartyManager.TextEnum.TERRORISTS_WIN);

                    party.AddMoneyTo(party.partyMode.winTheRoundMoney, TeamEnum.TERRORISTS);
                    party.AddMoneyTo(party.partyMode.loseTheRoundMoney + party.partyMode.loseIncrease * party.loseCountCounterTerrorists, TeamEnum.COUNTERTERRORISTS);

                    //Set round to finished round state
                    party.partyTimer = party.partyMode.endRoundWaitingTime;
                    party.roundState = RoundState.END_ROUND;
                    party.CheckAfterRound();
                }
                else if (TerroristDeadCount == TerroristsCount && !party.bombSet)
                {
                    party.counterScore++;

                    if (party.loseCountCounterTerrorists > 0)
                        party.loseCountCounterTerrorists--;

                    if (party.loseCountTerrorists < 4)
                        party.loseCountTerrorists++;

                    //Send score to all clients
                    party.communicator.SendScore();

                    party.communicator.SendText(PartyManager.TextEnum.COUNTER_TERRORISTS_WIN);

                    party.AddMoneyTo(party.partyMode.winTheRoundMoney, TeamEnum.COUNTERTERRORISTS);
                    party.AddMoneyTo(party.partyMode.loseTheRoundMoney + party.partyMode.loseIncrease * party.loseCountTerrorists, TeamEnum.TERRORISTS);

                    //Set round to finished round state
                    party.partyTimer = party.partyMode.endRoundWaitingTime;
                    party.roundState = RoundState.END_ROUND;
                    party.CheckAfterRound();
                }
            }
            else if (!party.partyStarted)//If the player is dead, respawn the player
            {
                respawnTimer = party.partyMode.trainingRespawnTimer;
                needRespawn = true;
            }
        }

        /// <summary>
        /// Check client health after damage
        /// </summary>
        /// <param name="killerClient">Client who shoot the client</param>
        /// <param name="CheckScore">Check if the server can add a point to a team</param>
        /// <param name="sendHealth">Send health to all clients?</param>
        public void CheckAfterDamage(Client killerClient, bool CheckScore, bool sendHealth)
        {
            Party currentParty = party;
            if (health <= 0)
                health = 0;

            if (sendHealth)
                communicator.SendHealth();

            if (health == 0 && !isDead)
            {
                //Set client has dead
                isDead = true;
                deathCount++;

                //Drop the bomb
                if (inventory.haveBomb)
                {
                    PlaceBomb(position.x, position.y, position.z, angle, true);
                }

                if (killerClient != null)
                {
                    //If the killer killed a team mate
                    if (killerClient.party.partyMode.teamDamage && team == killerClient.team)
                    {
                        //Reduce kill count and money
                        killerClient.killCount--;
                        killerClient.money -= killerClient.party.partyMode.killPenalties;
                        if (killerClient.money < 0)
                            killerClient.money = 0;

                        killerClient.killedFriend++;
                        if (killerClient.killedFriend == 2 && (currentParty.partyMode.roundTime - currentParty.partyTimer).TotalSeconds <= 10) //if the player has killed 2 friends but at the beginning kick the player
                        {
                            killerClient.communicator.SendError(NetworkDataManager.ErrorType.KickTeamKill);
                            killerClient.NeedRemoveConnection = true;
                        }
                        else if (killerClient.killedFriend >= 3) //if the player has killed 3 friends, kick the player
                        {
                            killerClient.communicator.SendError(NetworkDataManager.ErrorType.KickTeamKill);
                            killerClient.NeedRemoveConnection = true;
                        }
                        killerClient.communicator.SendMoney();
                    }
                    else
                    {
                        //Add kill count and money
                        killerClient.killCount++;
                        if (ShopManager.allShopElements[killerClient.inventory.GetCurrentSlotValue()] is Gun)
                            killerClient.AddMoneyTo(((Gun)ShopManager.allShopElements[killerClient.inventory.GetCurrentSlotValue()]).killMoneyBonus[PartyManager.allPartyModesData.IndexOf(killerClient.party.partyMode)]);
                        killerClient.communicator.SendKillCountAndDeathCount();
                    }

                    //Send kill text
                    currentParty.communicator.SendTextPlayer(this, killerClient, 0);
                }
                else
                {
                    //Send kill text
                    currentParty.communicator.SendTextPlayer(this, null, 0);
                }

                communicator.SendKillCountAndDeathCount();

                if (!CheckScore)
                    return;

                OnPlayerKilled();
            }
        }

        /// <summary>
        /// Make a hit on a client (String version)
        /// </summary>
        /// <param name="hittedPlayerId">Hitted client</param>
        /// <param name="isHeadShotString">Is a head shot</param>
        /// <param name="isLegShotString">Is a leg shot</param>
        public void MakeHit(string hittedPlayerId, string isHeadShotString, string isLegShotString, string distanceString)
        {
            int playerId = int.Parse(hittedPlayerId);
            bool isHeadShot = int.Parse(isHeadShotString) == 1;
            bool isLegShot = int.Parse(isLegShotString) == 1;
            float distance = float.Parse(distanceString, CultureInfo.InvariantCulture);
            MakeHit(playerId, isHeadShot, isLegShot, distance);
        }

        /// <summary>
        /// Make a hit on the client
        /// </summary>
        /// <param name="hittedPlayerId">Hitted client</param>
        /// <param name="isHeadShot">Is a head shot</param>
        /// <param name="isLegShot">Is a leg shot</param>
        /// <exception cref="Exception"></exception>
        public void MakeHit(int hittedPlayerId, bool isHeadShot, bool isLegShot, float distance)
        {
            if (cancelNextHit || isDead)
            {
                return;
            }

            if (isHeadShot && isHeadShot == isLegShot)
            {
                communicator.SendError(NetworkDataManager.ErrorType.Null);
                throw new Exception("isHeadShot equals 1 and equals isLegShot");
            }

            distance = Math.Abs(distance);

            Client HittedClient = party.FindClientById(hittedPlayerId);

            if (HittedClient == null)//Do not throw an exeption, the hitted client may be disconnected
                return;

            Gun usedGun = ShopManager.allShopElements[inventory.GetCurrentSlotValue()] as Gun;

            //Get gun damage
            int Damage = (int)(usedGun.damage * Math.Pow(usedGun.damageFalloff, distance / 500.0));

            int hitType = 0;
            if (isHeadShot)
            {
                hitType = 1;
                Damage *= 4;
            }
            else if (isLegShot)
            {
                hitType = 2;
                Damage /= 2;
            }

            //Reduce damage when clients are in the same team
            if (HittedClient.team == team)
                Damage = (int)(Damage / 3f);

            if (inventory.GetCurrentSlotValue() == 0)
                hitType = 3;

            communicator.SendHitSound(hittedPlayerId, hitType);

            if (party.partyMode.teamDamage || HittedClient.team != team)
            {
                if (party.roundState == RoundState.PLAYING || party.roundState == RoundState.TRAINING || party.roundState == RoundState.END_ROUND)
                {
                    if ((hitType == 0 && HittedClient.armor != 0) || (hitType == 1 && HittedClient.inventory.haveHelmet))
                    {
                        // Reduce damage of the bullet
                        int oldDamage = Damage;
                        Damage = (int)(Damage * ((Gun)ShopManager.allShopElements[inventory.GetCurrentSlotValue()]).penetration / 100f);
                        // Remove headset
                        if (hitType == 1)
                        {
                            HittedClient.inventory.haveHelmet = false;
                        }
                        else // Or reduce armor durability
                        {
                            HittedClient.armor -= oldDamage - Damage;
                            if (HittedClient.armor < 0)
                                HittedClient.armor = 0;
                        }
                    }
                    HittedClient.health -= Damage;
                    HittedClient.CheckAfterDamage(this, true, true);
                }
            }
        }

        #endregion

        #region Other

        /// <summary>
        /// Reset a player
        /// </summary>
        /// <param name="client"></param>
        public void ResetPlayer()
        {
            //Set health to the max
            health = 100;

            //Reset grenade bought count
            for (int i = 0; i < inventory.grenadeBought.Length; i++)
            {
                inventory.grenadeBought[i] = 0;
            }

            //If needed (if the player is dead or when the party start or swap the team)
            if (isDead || party.counterScore + party.terroristsScore == 0 || party.partyMode.middlePartyTeamSwap && party.terroristsScore + party.counterScore == Math.Floor(party.partyMode.maxRound / 2f))
            {
                //Remove equipment
                inventory.haveDefuseKit = false;
                armor = 0;
                inventory.haveHelmet = false;

                //Set default gun
                inventory.SetDefaultGun();

                //Remove all other gun
                for (int i = 2; i < Inventory.INVENTORY_SIZE - 1; i++)
                {
                    inventory.allSlots[i] = -1;
                }
            }
            else
            {
                //I don't know what the code does here
                //I think it's to count all bought grenade after the grenade count reset, maybe i can reset the grenade count if the player is dead or when the party start or swap the team
                for (int i = 0; i < ShopManager.SHOP_GRENADE_COUNT; i++)
                {
                    for (int grenadeCheckIndex = Inventory.INVENTORY_GRENADES_START_POSITION; grenadeCheckIndex < Inventory.INVENTORY_EQUIPMENTS_START_POSITION; grenadeCheckIndex++)
                    {
                        if (inventory.allSlots[grenadeCheckIndex] == ShopManager.SHOP_GUN_COUNT + i/* && inventoryIndex == -1 && AllClients[i].grenadeBought[ElementToBuy - GUNCOUNT] < ((Grenade)AllShopElements[ElementToBuy]).MaxQuantity[CurrentParty.PartyType]*/)
                            inventory.grenadeBought[i]++;
                    }
                }
            }

            //Put armor if needed
            if (party.partyMode.spawnWithArmor)
            {
                armor = 100;
                inventory.haveHelmet = true;
            }

            inventory.ResetGunsAmmo();

            isDead = false;

            communicator.SendHealth();

            communicator.SendClientInventoryToClients();
        }

        /// <summary>
        /// Set player name
        /// !!! CAN KICK PLAYER !!!
        /// </summary>
        /// <param name="name">Client name</param>
        /// <exception cref="Exception"></exception>
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 14)
            {
                communicator.SendError(NetworkDataManager.ErrorType.SaveCorrupted);
                throw new Exception("Name is too long or empty.");
            }
            this.name = name;

            //Send name to all clients
            if (party != null)
                Call.Create($"SETNAME;{id};{name}", party.allConnectedClients, this);
        }

        /// <summary>
        /// Update the last time the client sent a response after the server's ping request
        /// </summary>
        public void UpdateClientPing()
        {
            ping = (int)(DateTime.Now - lastPing).TotalMilliseconds;
        }

        /// <summary>
        /// Add money to the client and send money to all clients except him
        /// </summary>
        /// <param name="Money">Money to add</param>
        public void AddMoneyTo(int Money)
        {
            money += Money;

            //Check if the money is too high, set player money to the max capacity
            if (money > party.partyMode.maxMoney)
                money = party.partyMode.maxMoney;

            communicator.SendMoney();
        }

        #endregion
    }
}
