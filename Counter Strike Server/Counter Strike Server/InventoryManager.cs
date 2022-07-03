// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Reflection;

namespace Counter_Strike_Server
{
    static class InventoryManager
    {
        public const int INVENTORY_GRENADES_START_POSITION = 3;
        public const int INVENTORY_EQUIPMENTS_START_POSITION = 7;
        public const int INVENTORY_C4_POSITION = 8;
        public const int INVENTORY_SIZE = 9;
        public const int DEFAULT_COUNTERTER_TERRORIST_GUN = 6;
        public const int DEFAULT_TERRORIST_GUN = 4;

        public const int KNIFE_ID = 0;
        public const int C4_ID = 28;

        /// <summary>
        /// Set the current used gun of a client (String version)
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="gunId">Gun index</param>
        public static void SetGun(Client client, string gunId)
        {
            SetGun(client, int.Parse(gunId));
        }

        /// <summary>
        /// Set the current used gun of a client<br></br>
        /// !!! CAN KICK PLAYER !!!
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="gunId">Gun index</param>
        /// <exception cref="Exception"></exception>
        public static void SetGun(Client client, int gunId)
        {
            //Check if the gun index is valid
            if (gunId < 0 || gunId >= INVENTORY_SIZE)
            {
                throw new Exception("gunId is out of range.");
            }
            client.currentGunInInventory = gunId;
            SendClientCurrentGunToClients(client);
        }

        /// <summary>
        /// Send client current gun to all clients
        /// </summary>
        /// <param name="client"></param>
        public static void SendClientCurrentGunToClients(Client client)
        {
            Call.CreateCall($"CURGUN;{client.id};{client.currentGunInInventory}", client.clientParty.allConnectedClients, client);
        }

        /// <summary>
        /// Send client gun reloaded
        /// </summary>
        /// <param name="client"></param>
        public static void SendReloaded(Client client)
        {
            Call.CreateCall($"RELOADED;{client.id};{0}", client.clientParty.allConnectedClients, client);
        }

        /// <summary>
        /// Reload gun of a client
        /// </summary>
        /// <param name="client"></param>
        public static void ReloadGun(Client client)
        {
            // Check if the weapon in the player hands is a gun
            if (client.currentGunInInventory == 1 || client.currentGunInInventory == 2)
            {
                int ammoIndex = client.currentGunInInventory - 1;
                int missingAmmoCount = ((Gun)ShopManager.allShopElements[client.allGunsInInventory[client.currentGunInInventory]]).magazineCapacity - client.AllAmmoMagazine[ammoIndex].AmmoCount;
                if (missingAmmoCount < client.AllAmmoMagazine[ammoIndex].TotalAmmoCount)
                {
                    client.AllAmmoMagazine[ammoIndex].AmmoCount += missingAmmoCount;
                    if (!client.clientParty.partyMode.infiniteGunAmmo)
                        client.AllAmmoMagazine[ammoIndex].TotalAmmoCount -= missingAmmoCount;
                }
                else
                {
                    client.AllAmmoMagazine[ammoIndex].AmmoCount += client.AllAmmoMagazine[ammoIndex].TotalAmmoCount;
                    client.AllAmmoMagazine[ammoIndex].TotalAmmoCount = 0;
                }
            }
        }

        /// <summary>
        /// Reset gun ammo of a gun
        /// </summary>
        /// <param name="client"></param>
        /// <param name="inventoryIndex"></param>
        public static void ResetGunAmmo(Client client, int inventoryIndex)
        {
            if (inventoryIndex < 1 || inventoryIndex > 2)
                return;

            int ammoIndex = inventoryIndex - 1;
            client.AllAmmoMagazine[ammoIndex].AmmoCount = ((Gun)ShopManager.allShopElements[client.allGunsInInventory[inventoryIndex]]).magazineCapacity;
            client.AllAmmoMagazine[ammoIndex].TotalAmmoCount = ((Gun)ShopManager.allShopElements[client.allGunsInInventory[inventoryIndex]]).maxAmmoCount;
        }

        /// <summary>
        /// Reset all client guns ammo
        /// </summary>
        /// <param name="client"></param>
        public static void ResetGunsAmmo(Client client)
        {
            for (int ammoIndex = 0; ammoIndex < 2; ammoIndex++)
            {
                int inventoryIndex = ammoIndex + 1;
                if (client.allGunsInInventory[inventoryIndex] != -1)
                {
                    client.AllAmmoMagazine[ammoIndex].AmmoCount = ((Gun)ShopManager.allShopElements[client.allGunsInInventory[inventoryIndex]]).magazineCapacity;
                    client.AllAmmoMagazine[ammoIndex].TotalAmmoCount = ((Gun)ShopManager.allShopElements[client.allGunsInInventory[inventoryIndex]]).maxAmmoCount;
                }
            }
        }

        /// <summary>
        /// Send clients current gun
        /// </summary>
        /// <param name="client"></param>
        public static void SendClientsCurrentGunToClient(Client client)
        {
            for (int i = 0; i < client.clientParty.allConnectedClients.Count; i++)
            {
                if (client.clientParty.allConnectedClients[i] != client)
                    Call.CreateCall($"CURGUN;{client.clientParty.allConnectedClients[i].id};{client.clientParty.allConnectedClients[i].currentGunInInventory}", client);
            }
        }

        /// <summary>
        /// Send client ammo to clients
        /// </summary>
        /// <param name="client"></param>
        public static void SendClientsAmmoToClient(Client client)
        {
            for (int i = 0; i < client.clientParty.allConnectedClients.Count; i++)
            {
                Client clientToCheck = client.clientParty.allConnectedClients[i];
                if (client.clientParty.allConnectedClients[i] != client)
                    Call.CreateCall($"AMMO;{clientToCheck.id};{clientToCheck.AllAmmoMagazine[0].AmmoCount};{clientToCheck.AllAmmoMagazine[0].TotalAmmoCount};{clientToCheck.AllAmmoMagazine[1].AmmoCount};{clientToCheck.AllAmmoMagazine[1].TotalAmmoCount}", client);
            }
        }

        /// <summary>
        /// Clear client inventory
        /// </summary>
        /// <param name="client"></param>
        public static void ClearInventory(Client client)
        {
            //Remove equipment
            client.haveDefuseKit = false;
            client.armor = 0;
            client.haveHelmet = false;

            //Set default gun
            if (client.team == teamEnum.TERRORISTS)
                client.allGunsInInventory[1] = DEFAULT_TERRORIST_GUN;
            else if (client.team == teamEnum.COUNTERTERRORISTS)
                client.allGunsInInventory[1] = DEFAULT_COUNTERTER_TERRORIST_GUN;

            //Remove all other gun
            for (int i = 2; i < INVENTORY_SIZE - 1; i++)
            {
                client.allGunsInInventory[i] = -1;
            }
        }

        /// <summary>
        /// Send client inventory to all clients
        /// </summary>
        /// <param name="client">Client</param>
        public static void SendClientInventoryToClients(Client client)
        {
            string NewCallData = $"INVTORY;{client.id};";
            for (int gunIndex = 0; gunIndex < client.allGunsInInventory.Length; gunIndex++)
                NewCallData += $"{client.allGunsInInventory[gunIndex]};";

            NewCallData = NewCallData.Remove(NewCallData.Length - 1);
            Call.CreateCall(NewCallData, client.clientParty.allConnectedClients);
        }

        /// <summary>
        /// Send client inventory to the clients list
        /// </summary>
        /// <param name="clientDestination"></param>
        public static void SendClientsInventoryToClient(Client clientDestination)
        {
            for (int i = 0; i < clientDestination.clientParty.allConnectedClients.Count; i++)
            {
                Client client = clientDestination.clientParty.allConnectedClients[i];
                if (client != clientDestination)
                {
                    string NewCallData = $"INVTORY;{client.id};";
                    for (int gunIndex = 0; gunIndex < client.allGunsInInventory.Length; gunIndex++)
                        NewCallData += $"{client.allGunsInInventory[gunIndex]};";

                    NewCallData = NewCallData.Remove(NewCallData.Length - 1);
                    Call.CreateCall(NewCallData, clientDestination);
                }
            }
        }
    }
}
