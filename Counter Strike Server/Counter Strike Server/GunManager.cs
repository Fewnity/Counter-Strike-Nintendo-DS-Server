// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Diagnostics;
using System.Globalization;

namespace Counter_Strike_Server
{
    static class GunManager
    {
        /// <summary>
        /// Make a hit on a player (String version)
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="hittedPlayerId">Hitted player</param>
        /// <param name="isHeadShotString">Is a head shot</param>
        /// <param name="isLegShotString">Is a leg shot</param>
        public static void MakeHit(Client client, string hittedPlayerId, string isHeadShotString, string isLegShotString, string distanceString)
        {
            int playerId = int.Parse(hittedPlayerId);
            bool isHeadShot = int.Parse(isHeadShotString) == 1;
            bool isLegShot = int.Parse(isLegShotString) == 1;
            float distance = float.Parse(distanceString, CultureInfo.InvariantCulture);
            MakeHit(client, playerId, isHeadShot, isLegShot, distance);
        }

        /// <summary>
        /// Make a hit on a player
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="hittedPlayerId">Hitted player</param>
        /// <param name="isHeadShot">Is a head shot</param>
        /// <param name="isLegShot">Is a leg shot</param>
        /// <exception cref="Exception"></exception>
        public static void MakeHit(Client client, int hittedPlayerId, bool isHeadShot, bool isLegShot, float distance)
        {
            if (client.cancelNextHit || client.isDead)
            {
                return;
            }

            if (isHeadShot && isHeadShot == isLegShot)
            {
                ConnectionManager.SendError(client, NetworkDataManager.ErrorType.Null);
                throw new Exception("isHeadShot equals 1 and equals isLegShot");
            }

            distance = Math.Abs(distance);

            Client HittedClient = NetworkDataManager.FindClientById(client.clientParty, hittedPlayerId);

            if (HittedClient == null)//Do not throw an exeption, the hitted client may be disconnected
                return;

            Gun usedGun = ShopManager.allShopElements[client.allGunsInInventory[client.currentGunInInventory]] as Gun;

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
            if (HittedClient.team == client.team)
                Damage = (int)(Damage / 3f);

            if (client.allGunsInInventory[client.currentGunInInventory] == 0)
                hitType = 3;

            SendHitSound(client, hittedPlayerId, hitType);

            if (client.clientParty.partyMode.teamDamage || HittedClient.team != client.team)
            {
                if (client.clientParty.roundState == RoundState.PLAYING || client.clientParty.roundState == RoundState.TRAINING || client.clientParty.roundState == RoundState.END_ROUND)
                {
                    if ((hitType == 0 && HittedClient.armor != 0) || (hitType == 1 && HittedClient.haveHelmet))
                    {
                        // Reduce damage of the bullet
                        int oldDamage = Damage;
                        Damage = (int)(Damage * ((Gun)ShopManager.allShopElements[client.allGunsInInventory[client.currentGunInInventory]]).penetration / 100f);
                        // Remove headset
                        if (hitType == 1)
                        {
                            HittedClient.haveHelmet = false;
                        }
                        else // Or reduce armor durability
                        {
                            HittedClient.armor -= oldDamage - Damage;
                            if (HittedClient.armor < 0)
                                HittedClient.armor = 0;
                        }
                    }
                    HittedClient.health -= Damage;
                    PlayerManager.CheckAfterDamage(client, HittedClient, true, true);
                }
            }
        }

        /// <summary>
        /// Send player hit sound
        /// </summary>
        /// <param name="client"></param>
        /// <param name="hittedPlayerId"></param>
        /// <param name="hitType"></param>
        public static void SendHitSound(Client client, int hittedPlayerId,int hitType)
        {
            Call.CreateCall($"HITSOUND;{client.id};{hittedPlayerId};{hitType}", client.clientParty.allConnectedClients, client);
        }

        /// <summary>
        /// Send shoot of a client to all client excepted the passed client
        /// </summary>
        /// <param name="client">Client</param>
        public static void SendShoot(Client client)
        {
            client.cancelNextHit = false;

            if (client.isDead)
                return;

            int gunId = client.allGunsInInventory[client.currentGunInInventory];
            if ((client.currentGunInInventory == 1 || client.currentGunInInventory == 2) && client.AllAmmoMagazine[client.currentGunInInventory - 1].AmmoCount > 0)
                client.AllAmmoMagazine[client.currentGunInInventory - 1].AmmoCount--;
            else if (client.currentGunInInventory == 1 || client.currentGunInInventory == 2)
            {
                InventoryManager.ReloadGun(client);
                //Security check
                if (client.AllAmmoMagazine[client.currentGunInInventory - 1].AmmoCount == 0)
                {
                    client.cancelNextHit = true;
                }
                else
                {
                    client.AllAmmoMagazine[client.currentGunInInventory - 1].AmmoCount--;
                }
            }

            if (!client.cancelNextHit)
            {
                Call.CreateCall($"SHOOT;{client.id};{gunId}", client.clientParty.allConnectedClients, client);
                //if (client.currentGunInInventory != 0)
                //NetworkDataManager.PrintMessage($"{client.id} Gun : {client.currentGunInInventory}, Ammo {client.AllAmmoMagazine[client.currentGunInInventory - 1].AmmoCount}; Total Ammo {client.AllAmmoMagazine[client.currentGunInInventory - 1].TotalAmmoCount}\n");
            }
        }

        /// <summary>
        /// Send a wall hit to all client excepted the passed client
        /// </summary>
        /// <param name="xPos">X position</param>
        /// <param name="yPos">Y position</param>
        /// <param name="zPos">Z position</param>
        /// <param name="client">Client</param>
        public static void SendWallHit(string xPos, string yPos, string zPos, Client client)
        {
            if (client.isDead)
                return;

            int x = int.Parse(xPos);
            int y = int.Parse(yPos);
            int z = int.Parse(zPos);
            Call.CreateCall($"WALLHIT;{x};{y};{z}", client.clientParty.allConnectedClients, client);
        }
    }
}
