// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Counter_Strike_Server
{
    static class BombManager
    {
        /// <summary>
        /// Send bomb placing or defusing notification
        /// </summary>
        /// <param name="client"></param>
        public static void SendBombPlacing(Client client)
        {
            if (!client.isDead && ((client.haveBomb && client.team== teamEnum.TERRORISTS) || client.team == teamEnum.COUNTERTERRORISTS) && PhysicsManager.CheckBombZone(client.Position, MapManager.allMaps[(int)client.clientParty.mapId]))
                Call.CreateCall($"BOMBPLACING;{client.id}", client.clientParty.allConnectedClients, client);
        }


        /// <summary>
        /// Client place the bomb at the position
        /// </summary>
        /// <param name="client">Client who placed the bomb (text)</param>
        /// <param name="xPos">x position of the bomb (text)</param>
        /// <param name="yPos">y position of the bomb (text)</param>
        /// <param name="zPos">z position of the bomb (text)</param>
        /// <param name="angle">angle of the bomb (text)</param>
        public static void PlaceBomb(Client client, string xPos, string yPos, string zPos, string angle, bool drop)
        {
            PlaceBomb(client, int.Parse(xPos), int.Parse(yPos), int.Parse(zPos), int.Parse(angle), drop);
        }

        /// <summary>
        /// Client place the bomb at the position
        /// </summary>
        /// <param name="client">Client who placed the bomb</param>
        /// <param name="xPos">x position of the bomb</param>
        /// <param name="yPos">y position of the bomb</param>
        /// <param name="zPos">z position of the bomb</param>
        /// <param name="angle">angle of the bomb</param>
        public static void PlaceBomb(Client client, int xPos, int yPos, int zPos, int angle, bool drop)
        {
            Party party = client.clientParty;
            if (client.haveBomb && (drop || !client.isDead))
            {
                bool CanPut  = false;
                if (drop)
                {
                    party.bombDropped = true;
                    //Add some offset to the bomb
                    yPos -= (int)(0.845 * 4096);
                    CanPut = true;
                }
                else if(PhysicsManager.CheckBombZone(client.Position, MapManager.allMaps[(int)client.clientParty.mapId]))
                {
                    CanPut = true;

                    //Set bomb timer
                    party.partyTimer = party.partyMode.bombWaitingTime;
                    party.bombSet = true;
                    party.bombDropped = false;

                    //Send data about the bomb placement
                    PartyManager.SendText(PartyManager.TextEnum.BOMB_PLANTED, party);
                    
                    //Add money to the client
                    MoneyManager.AddMoneyTo(client, party.partyMode.plantBombMoneyBonus);
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

                    SendBombPosition(party);

                    //Remove client's bomb
                    SetBombForAPlayer(client, false);
                }
            }
        }

        /// <summary>
        /// Send the bomb position to all clients
        /// </summary>
        /// <param name="party">Party</param>
        public static void SendBombPosition(Party party)
        {
            int dropInt = 0;
            if (party.bombDropped)
                dropInt = 1;

            Call.CreateCall($"BOMBPLACE;{party.bombPosition.x};{party.bombPosition.y};{party.bombPosition.z};{party.bombPosition.w};{dropInt}", party.allConnectedClients);
        }

        /// <summary>
        /// Send bomb position to the client
        /// </summary>
        /// <param name="client"></param>
        public static void SendBombPosition(Client client)
        {
            Party party = client.clientParty;
            int dropInt = 0;
            if (party.bombDropped)
                dropInt = 1;

            Call.CreateCall($"BOMBPLACE;{party.bombPosition.x};{party.bombPosition.y};{party.bombPosition.z};{party.bombPosition.w};{dropInt}", client);
        }

        /// <summary>
        /// Defuse the bomb
        /// </summary>
        /// <param name="client">Client who defused </param>
        public static void DefuseBomb(Client client)
        {
            Party party = client.clientParty;

            //Security check
            if (!client.isDead && client.team == teamEnum.COUNTERTERRORISTS && party.roundState == RoundState.PLAYING && party.bombSet && PhysicsManager.CheckBombDefuseZone(client.Position, client.clientParty))
            {
                party.counterScore++;

                if (party.loseCountCounterTerrorists > 0)
                    party.loseCountCounterTerrorists--;

                if (party.loseCountTerrorists < 4)
                    party.loseCountTerrorists++;

                PartyManager.SendText(PartyManager.TextEnum.BOMB_DEFUSED, party);

                //Send score to all clients
                PartyManager.SendScore(party);
                Call.CreateCall("BOMBDEFUSE", party.allConnectedClients);

                //Add money to defuser client and to teams
                MoneyManager.AddMoneyTo(client, party.partyMode.defuseBombMoneyBonus);
                MoneyManager.AddMoneyTo(party, party.partyMode.winTheRoundBombMoney, teamEnum.COUNTERTERRORISTS);
                MoneyManager.AddMoneyTo(party, party.partyMode.loseTheRoundMoney + party.partyMode.loseIncrease * party.loseCountTerrorists + party.partyMode.plantedBombLoseMoneyBonus, teamEnum.TERRORISTS);

                //Set round to finished round state
                party.partyTimer = party.partyMode.endRoundWaitingTime;
                party.roundState = RoundState.END_ROUND;
                PartyManager.CheckAfterRound(party);
            }
        }

        /// <summary>
        /// Defuse the bomb
        /// </summary>
        /// <param name="client">Client who defused </param>
        public static void GetBomb(Client client)
        {
            Party party = client.clientParty;

            //Security check
            if (!client.isDead && client.team == teamEnum.TERRORISTS && party.bombDropped && PhysicsManager.CheckBombDefuseZone(client.Position, client.clientParty))
            {
                SetBombForAPlayer(client, true);
                party.bombDropped = false;
            }
        }

        /// <summary>
        /// Set if a client has the bomb
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="haveBomb">True : the client will have the bomb, False : will not</param>
        public static void SetBombForAPlayer(Client client, bool haveBomb)
        {
            for (int i = 0; i < client.clientParty.allConnectedClients.Count; i++)
            {
                if(client.clientParty.allConnectedClients[i] == client)
                {
                    client.haveBomb = haveBomb;
                    if (haveBomb)
                    {
                        client.allGunsInInventory[InventoryManager.INVENTORY_C4_POSITION] = InventoryManager.C4_ID;
                    }
                    else
                    {
                        client.allGunsInInventory[InventoryManager.INVENTORY_C4_POSITION] = -1;
                    }
                }
                else
                {
                    client.clientParty.allConnectedClients[i].haveBomb = false;
                    client.clientParty.allConnectedClients[i].allGunsInInventory[InventoryManager.INVENTORY_C4_POSITION] = -1;
                }
            }

            SendClientBomb(client, haveBomb);
        }

        /// <summary>
        /// Send to the client who has the bomb
        /// </summary>
        /// <param name="clientDestination"></param>
        public static void SendWhoHasTheBomb(Client clientDestination)
        {
            for (int i = 0; i < clientDestination.clientParty.allConnectedClients.Count; i++)
            {
                if (clientDestination.clientParty.allConnectedClients[i].haveBomb)
                {
                    SendClientBomb(clientDestination.clientParty.allConnectedClients[i], clientDestination, true);
                    break;
                }
            }
        }

        /// <summary>
        /// Send to all clients if this client has the bomb or not
        /// </summary>
        /// <param name="client"></param>
        /// <param name="haveBomb"></param>
        public static void SendClientBomb(Client client, bool haveBomb)
        {
            int haveBombInt = 0;
            if (haveBomb)
                haveBombInt = 1;

            //Send if the player has the bomb to all clients
            Call.CreateCall($"SETBOMB;{client.id};{haveBombInt}", client.clientParty.allConnectedClients);
        }

        /// <summary>
        /// Send to a client if this client has the bomb or not
        /// </summary>
        /// <param name="client">Client to get the info</param>
        /// <param name="clientDestination">Receiver</param>
        /// <param name="haveBomb">Have the bomb?</param>
        public static void SendClientBomb(Client client, Client clientDestination, bool haveBomb)
        {
            int haveBombInt = 0;
            if (haveBomb)
                haveBombInt = 1;

            //Send if the player has the bomb to all clients
            Call.CreateCall($"SETBOMB;{client.id};{haveBombInt}", clientDestination);
        }

        /// <summary>
        /// Set the bomb to a random player
        /// </summary>
        /// <param name="party"></param>
        public static void SetBombForARandomPlayer(Party party)
        {
            List<Client> allTerrorists = new List<Client>();

            //Remove the bomb for all terrorists and add them in the allTerrorists list
            foreach (Client client in party.allConnectedClients)
            {
                if (client.team == teamEnum.TERRORISTS)
                {
                    allTerrorists.Add(client);
                }
            }

            //If there is more than 0 terrorist
            if (allTerrorists.Count > 0)
            {
                //Give the bomb to a random one
                int randomTerrorist = new Random().Next(0, allTerrorists.Count);
                SetBombForAPlayer(allTerrorists[randomTerrorist], true);
            }
        }
    }
}
