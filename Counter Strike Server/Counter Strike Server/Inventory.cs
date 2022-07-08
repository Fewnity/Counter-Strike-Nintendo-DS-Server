using System;

namespace Counter_Strike_Server
{
    /// <summary>
    /// Inventory of a client
    /// </summary>
    public class Inventory
    {
        public class AmmoMagazine
        {
            public int AmmoCount;
            public int TotalAmmoCount;
        }

        readonly Client client;

        public Inventory(Client client)
        {
            this.client = client;
        }

        public const int INVENTORY_GRENADES_START_POSITION = 3;
        public const int INVENTORY_EQUIPMENTS_START_POSITION = 7;
        public const int INVENTORY_C4_POSITION = 8;
        public const int INVENTORY_SIZE = 9;
        public const int DEFAULT_COUNTERTER_TERRORIST_GUN = 6;
        public const int DEFAULT_TERRORIST_GUN = 4;

        public const int KNIFE_ID = 0;
        public const int C4_ID = 28;

        public int currentSlot;
        public AmmoMagazine[] AllAmmoMagazine = new AmmoMagazine[2];
        public int[] allSlots = new int[INVENTORY_SIZE];
        public int[] grenadeBought = new int[5];
        public bool haveHelmet;
        public bool haveDefuseKit;
        public bool haveBomb;

        /// <summary>
        /// Set the default gun in the client inventory
        /// </summary>
        public void SetDefaultGun()
        {
            if (client.team == TeamEnum.TERRORISTS)
                allSlots[1] = DEFAULT_TERRORIST_GUN;
            else if (client.team == TeamEnum.COUNTERTERRORISTS)
                allSlots[1] = DEFAULT_COUNTERTER_TERRORIST_GUN;
        }

        /// <summary>
        /// Set if the client has the bomb
        /// </summary>
        /// <param name="haveBomb">True : the client will have the bomb, False : will not</param>
        public void SetBomb(bool haveBomb)
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
            {
                Client clientToCheck = client.party.allConnectedClients[i];
                if (clientToCheck == client)
                {
                    this.haveBomb = haveBomb;
                    if (haveBomb)
                    {
                        allSlots[INVENTORY_C4_POSITION] = C4_ID;
                    }
                    else
                    {
                        allSlots[INVENTORY_C4_POSITION] = -1;
                    }
                }
                else
                {
                    //Remove the bomb from all other client
                    clientToCheck.inventory.haveBomb = false;
                    clientToCheck.inventory.allSlots[INVENTORY_C4_POSITION] = -1;
                }
            }

            client.communicator.SendClientBomb(haveBomb);
        }

        /// <summary>
        /// Get the value of the current slot
        /// </summary>
        /// <returns></returns>
        public int GetCurrentSlotValue()
        {
            return allSlots[currentSlot];
        }

        /// <summary>
        /// Set the value of the current slot
        /// </summary>
        /// <param name="value"></param>
        public void SetCurrentSlotValue(int value)
        {
            allSlots[currentSlot] = value;
        }

        #region Ammo management

        /// <summary>
        /// Reset gun ammo of a gun
        /// </summary>
        /// <param name="inventoryIndex"></param>
        public void ResetGunAmmo(int inventoryIndex)
        {
            if (inventoryIndex < 1 || inventoryIndex > 2)
                return;

            int ammoIndex = inventoryIndex - 1;
            AllAmmoMagazine[ammoIndex].AmmoCount = ((Gun)ShopManager.allShopElements[allSlots[inventoryIndex]]).magazineCapacity;
            AllAmmoMagazine[ammoIndex].TotalAmmoCount = ((Gun)ShopManager.allShopElements[allSlots[inventoryIndex]]).maxAmmoCount;
        }

        /// <summary>
        /// Reset all client guns ammo
        /// </summary>
        public void ResetGunsAmmo()
        {
            for (int ammoIndex = 0; ammoIndex < 2; ammoIndex++)
            {
                int inventoryIndex = ammoIndex + 1;
                if (allSlots[inventoryIndex] != -1)
                {
                    AllAmmoMagazine[ammoIndex].AmmoCount = ((Gun)ShopManager.allShopElements[allSlots[inventoryIndex]]).magazineCapacity;
                    AllAmmoMagazine[ammoIndex].TotalAmmoCount = ((Gun)ShopManager.allShopElements[allSlots[inventoryIndex]]).maxAmmoCount;
                }
            }
        }

        /// <summary>
        /// Reload gun of the client
        /// </summary>
        public void ReloadGun()
        {
            // Check if the weapon in the player hands is a gun
            if (currentSlot == 1 || currentSlot == 2)
            {
                int ammoIndex = currentSlot - 1;
                int missingAmmoCount = ((Gun)ShopManager.allShopElements[allSlots[currentSlot]]).magazineCapacity - AllAmmoMagazine[ammoIndex].AmmoCount;
                if (missingAmmoCount < AllAmmoMagazine[ammoIndex].TotalAmmoCount)
                {
                    AllAmmoMagazine[ammoIndex].AmmoCount += missingAmmoCount;
                    if (!client.party.partyMode.infiniteGunAmmo)
                        AllAmmoMagazine[ammoIndex].TotalAmmoCount -= missingAmmoCount;
                }
                else
                {
                    AllAmmoMagazine[ammoIndex].AmmoCount += AllAmmoMagazine[ammoIndex].TotalAmmoCount;
                    AllAmmoMagazine[ammoIndex].TotalAmmoCount = 0;
                }
            }
        }

        #endregion

        /// <summary>
        /// Clear client inventory
        /// </summary>
        public void ClearInventory()
        {
            //Remove equipment
            haveDefuseKit = false;
            client.armor = 0;
            haveHelmet = false;

            //Set default gun
            if (client.team == TeamEnum.TERRORISTS)
                allSlots[1] = DEFAULT_TERRORIST_GUN;
            else if (client.team == TeamEnum.COUNTERTERRORISTS)
                allSlots[1] = DEFAULT_COUNTERTER_TERRORIST_GUN;

            //Remove all other gun
            for (int i = 2; i < INVENTORY_SIZE - 1; i++)
            {
                allSlots[i] = -1;
            }
        }

        /// <summary>
        /// Set the current used gun of the client (String version)
        /// </summary>
        /// <param name="gunId">Gun index</param>
        public void SetCurrentSlot(string gunId)
        {
            SetCurrentSlot(int.Parse(gunId));
        }

        /// <summary>
        /// Set the current used gun of the client<br></br>
        /// !!! CAN KICK PLAYER !!!
        /// </summary>
        /// <param name="slot">Slot index</param>
        /// <exception cref="Exception"></exception>
        public void SetCurrentSlot(int slot)
        {
            //Check if the gun index is valid
            if (slot < 0 || slot >= INVENTORY_SIZE)
            {
                throw new Exception("gunId is out of range.");
            }
            if (allSlots[slot] != -1)
            {
                currentSlot = slot;
                client.communicator.SendClientCurrentGunToClients();
            }
        }
    }
}
