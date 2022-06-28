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
        public static Physics physicsEngine = new Physics();

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
                        for (int i2 = 0; i2 < currentParty.allGrenades.Count; i2++)
                        {
                            //If the greande explose
                            if (currentParty.allGrenades[i2].id == 0 && currentParty.allGrenades[i2].timer <= DateTime.Now)
                            {
                                //For each clients in the party
                                for (int i3 = 0; i3 < currentParty.allConnectedClients.Count; i3++)
                                {
                                    //Calculate distance beetwen the grenade and the client
                                    float Distance = (float)Math.Sqrt(Math.Pow(currentParty.allConnectedClients[i3].Position.x - currentParty.allGrenades[i2].physics.x / 2, 2f) + Math.Pow(currentParty.allConnectedClients[i3].Position.y - currentParty.allGrenades[i2].physics.y / 2, 2f) + Math.Pow(currentParty.allConnectedClients[i3].Position.z - -(currentParty.allGrenades[i2].physics.z / 2), 2f)) / 8096f;
                                    if (Distance > 4)
                                        Distance = 0;

                                    //If the distance is big enought
                                    if (Distance > 0)
                                    {
                                        //Apply damage
                                        int Damage = (int)Program.map(Distance, 0.3, 4, 100, 0);
                                        currentParty.allConnectedClients[i3].health -= Damage;
                                        PlayerManager.CheckAfterDamage(currentParty.allGrenades[i2].launcher, currentParty.allConnectedClients[i3], true, true);
                                    }
                                }
                                currentParty.allGrenades.RemoveAt(i2);
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
