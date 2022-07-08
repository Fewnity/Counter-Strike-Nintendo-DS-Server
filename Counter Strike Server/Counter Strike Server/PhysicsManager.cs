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
    static class PhysicsManager
    {
        public static Physics physicsEngine = new();

        /// <summary>
        /// Check physics for grenades
        /// </summary>
        public static void CheckPhysics()
        {
            while (true)
            {
                lock (PartyManager.allParties)
                {
                    List<Party> allParties = PartyManager.allParties;

                    //For each parties
                    for (int i = 0; i < allParties.Count; i++)
                    {
                        Party currentParty = allParties[i];
                        //For each grenades
                        for (int grenadeIndex = 0; grenadeIndex < currentParty.allGrenades.Count; grenadeIndex++)
                        {
                            //If the greande explose
                            if (currentParty.allGrenades[grenadeIndex].id == 0 && currentParty.allGrenades[grenadeIndex].timer <= DateTime.Now)
                            {
                                //For each clients in the party
                                for (int playerIndex = 0; playerIndex < currentParty.allConnectedClients.Count; playerIndex++)
                                {
                                    if (currentParty.allConnectedClients[playerIndex].isDead || currentParty.allConnectedClients[playerIndex].team == TeamEnum.SPECTATOR)
                                        continue;

                                    //Calculate distance beetwen the grenade and the client
                                    float Distance = (float)Math.Sqrt(Math.Pow(currentParty.allConnectedClients[playerIndex].position.x - currentParty.allGrenades[grenadeIndex].physics.x / 2, 2f) + Math.Pow(currentParty.allConnectedClients[playerIndex].position.y - currentParty.allGrenades[grenadeIndex].physics.y / 2, 2f) + Math.Pow(currentParty.allConnectedClients[playerIndex].position.z - -(currentParty.allGrenades[grenadeIndex].physics.z / 2), 2f)) / 8096f;
                                    // Set a maximum distance
                                    if (Distance > 4)
                                        Distance = 0;

                                    //If the distance is big enought
                                    if (Distance > 0)
                                    {
                                        //Apply damage
                                        int Damage = (int)Program.Map(Distance, 0.3, 4, 100, 0);
                                        currentParty.allConnectedClients[playerIndex].health -= Damage;
                                        currentParty.allConnectedClients[playerIndex].CheckAfterDamage(currentParty.allGrenades[grenadeIndex].launcher, true, true);
                                    }
                                }
                                currentParty.allGrenades.RemoveAt(grenadeIndex);
                                grenadeIndex--;
                            }
                        }
                    }
                }
                //Wait 0.1 seconds
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Check if the bomb is in the bomb zone
        /// </summary>
        /// <param name="pos">Position of the bomb</param>
        /// <param name="map">Current map played</param>
        /// <returns>Is the bomb in the zone</returns>
        public static bool CheckBombZone(Vector3Int pos, MapData map)
        {
            for (int i = 0; i < map.AllBombsTriggersCollisions.Count; i++)
            {
                BoxCollisions box = map.AllBombsTriggersCollisions[i];
                if (pos.x <= box.corner1 && pos.x >= box.corner2 && pos.z <= box.corner3 && pos.z >= box.corner4)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the bomb is in the defuse zone
        /// </summary>
        /// <param name="pos">Position of the defuser</param>
        /// <param name="party">Party</param>
        /// <returns>Is the defuser in the zone</returns>
        public static bool CheckBombDefuseZone(Vector3Int pos, Party party)
        {
            BoxCollisions box = party.defuseZoneCollisions;
            if (pos.x <= box.corner1 && pos.x >= box.corner2 && pos.z <= box.corner3 && pos.z >= box.corner4)
            {
                return true;
            }
            return false;
        }
    }
}
