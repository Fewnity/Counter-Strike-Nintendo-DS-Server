// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

namespace Counter_Strike_Server
{
    static class GrenadeManager
    {
        /// <summary>
        /// Send greande to all clients except the passed client (String version)
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="xDirection">X direction</param>
        /// <param name="yDirection">Y direction</param>
        /// <param name="zDirection">Z direction</param>
        public static void SendGrenade(Client client, string xDirection, string yDirection, string zDirection)
        {
            SendGrenade(client, float.Parse(xDirection), float.Parse(yDirection), float.Parse(zDirection));
        }

        /// <summary>
        /// Send greande to all clients except the passed client
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="xDirection">X direction</param>
        /// <param name="yDirection">Y direction</param>
        /// <param name="zDirection">Z direction</param>
        public static void SendGrenade(Client client, float xDirection, float yDirection, float zDirection)
        {
            if (client.allGunsInInventory[client.currentGunInInventory] != -1 && ShopManager.allShopElements[client.allGunsInInventory[client.currentGunInInventory]] is Grenade)
            {
                int grenadeId = (ShopManager.allShopElements[client.allGunsInInventory[client.currentGunInInventory]] as Grenade).id;
                Call.CreateCall($"GRENADE;{xDirection.ToString().Replace(',', '.')};{yDirection.ToString().Replace(',', '.')};{zDirection.ToString().Replace(',', '.')};{(int)client.Position.x};{(int)client.Position.y};{(int)client.Position.z};{grenadeId}", client.clientParty.allConnectedClients, client);

                client.allGunsInInventory[client.currentGunInInventory] = -1;
                Physics.Grenade newGrenade = PhysicsManager.physicsEngine.CreateGrenade(client, grenadeId, xDirection, yDirection, zDirection);
                
                //Calculate 4 seconds of grenade physics
                for (int iGrenade = 0; iGrenade < 60 * 4; iGrenade++)
                {
                    PhysicsManager.physicsEngine.NE_PhysicsUpdateOneGrenade(newGrenade);
                }
            }
        }
    }
}
