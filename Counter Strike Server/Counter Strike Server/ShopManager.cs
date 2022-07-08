// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    static class ShopManager
    {
        public static List<ShopElement> allShopElements = new();

        public const int SHOP_GUN_COUNT = 25;
        public const int SHOP_GRENADE_COUNT = 3;
        public const int SHOP_EQUIPMENT_COUNT = 1; //TODO LATER 3

        enum ShopConfirmType
        {
            ShopOk = 0,
            ShopError = 1,
        }

        enum ConfirmType
        {
            Shop = 0,
        }

        /// <summary>
        /// Load shop data
        /// </summary>
        public static void AddAllShopElements()
        {
            allShopElements.Add(new Gun(new List<int>() { 1500, 750, 1500 }, false, 0, 40, 85, 0, 0, 1)); // Knife 0

            //Small guns
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, false, 650, 53, 75, 7, 35, 0.81f)); // Desert Eagle 1
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, false, 800, 43, 52, 30, 120, 0.75f)); // Dual Berettas / Elit 2
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, false, 750, 24, 75, 20, 100, 0.885f)); // Five-SeveN 3
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, false, 400, 24, 52, 20, 120, 0.75f)); // Glock-18 4
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, false, 600, 31, 62.5f, 13, 52, 0.8f)); // P228 5
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, false, 500, 29, 50, 12, 100, 0.79f)); // USP 6

            //Big guns
            allShopElements.Add(new Gun(new List<int>() { 900, 450, 900 }, true, 1700, 171 / 6, 50, 8, 48, 0.7f)); // M3 Check kill money 7
            allShopElements.Add(new Gun(new List<int>() { 900, 450, 900 }, true, 3000, 114 / 6, 50, 7, 32, 0.7f)); // XM1014 8 
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 5750, 35, 75, 100, 200, 0.97f)); // M249 9 

            allShopElements.Add(new Gun(new List<int>() { 600, 300, 600 }, true, 1400, 28, 47.5f, 30, 100, 0.82f)); // MAC-10 10
            allShopElements.Add(new Gun(new List<int>() { 600, 300, 600 }, true, 1500, 25, 50, 30, 120, 0.84f)); // MP5 Check kill money 11
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 2350, 25, 75, 50, 100, 0.885f)); // P90 12
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 1250, 19, 50, 30, 120, 0.85f)); // TMP Check kill money 13
            allShopElements.Add(new Gun(new List<int>() { 600, 300, 600 }, true, 1700, 29, 50, 25, 100, 0.75f)); // UMP-45 14
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 2500, 35, 77.5f, 30, 90, 0.98f)); // AK-47 15
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 3500, 31, 70, 30, 90, 0.96f)); // AUG 16
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 2250, 29, 70, 25, 75, 0.96f)); // FAMAS 17
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 2000, 29, 77, 35, 90, 0.98f)); // Galil Check kill money 18
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 3100, 31, 70, 30, 90, 0.97f)); // M4A1 Check kill money 19
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 3500, 32, 70, 30, 90, 0.955f)); // SG 552 Check kill money 20
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 4200, 69, 72.5f, 30, 90, 0.98f)); // SG 550 Check kill money 21
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 2750, 74, 85, 10, 90, 0.98f)); // SSG 08 / Scout Check kill money 22
            allShopElements.Add(new Gun(new List<int>() { 300, 150, 300 }, true, 5000, 79, 82.5f, 20, 90, 0.98f)); // G3SG1 23
            allShopElements.Add(new Gun(new List<int>() { 100, 50, 100 }, true, 4750, 115, 97.5f, 10, 30, 0.99f));  // AWP 24

            allShopElements.Add(new Grenade(new List<int>() { 300, 300, 300 }, new List<int>() { 1, 1, 1 }, 300, 0)); // HE Grenade 25
            allShopElements.Add(new Grenade(new List<int>() { 300, 300, 300 }, new List<int>() { 1, 1, 1 }, 300, 1)); // Smoke 26
            allShopElements.Add(new Grenade(new List<int>() { 300, 300, 300 }, new List<int>() { 2, 1, 1 }, 200, 2)); // Flashbang 27

            allShopElements.Add(new Equipment(0, 0)); //C4 28
            allShopElements.Add(new Equipment(1, 400)); //Defuse Kit 29
            allShopElements.Add(new Equipment(2, 650)); //Kevlar vest 30
            allShopElements.Add(new Equipment(3, 1000)); //Kevlar and vest 31
        }

        /// <summary>
        /// Buy a gun for a client
        /// !!! CAN KICK PLAYER !!!
        /// </summary>
        /// <param name="elementToBuy"></param>
        /// <param name="client"></param>
        /// <exception cref="Exception"></exception>
        public static void Buy(int elementToBuy, Client client)
        {
            //Check if the shop element index is valid
            if (elementToBuy < 0 || elementToBuy >= allShopElements.Count)
            {
                client.communicator.SendError(NetworkDataManager.ErrorType.Null);
                throw new Exception("elementToBuy is out of range.");
            }

            //Check if the player is not dead and have enough money
            if (!client.isDead && client.money >= allShopElements[elementToBuy].price)
            {
                bool haveError = false;
                int inventoryIndex = -1;

                //If the element to buy is a gun
                if (allShopElements[elementToBuy] is Gun)
                {
                    //Chose the inventory index
                    if (((Gun)allShopElements[elementToBuy]).isBigGun)
                        inventoryIndex = 2;
                    else
                        inventoryIndex = 1;

                    //If the gun is alreay bought, stop the request
                    if (client.inventory.allSlots[inventoryIndex] == elementToBuy)
                    {
                        haveError = true;
                    }
                    else
                    {
                        client.inventory.allSlots[inventoryIndex] = elementToBuy;
                    }
                }
                else if (allShopElements[elementToBuy] is Grenade)  //If the element to buy is a grenade
                {
                    bool foundPlace = false;

                    //Try to put the grenade in a free slot for grenade in the inventory
                    for (int grenadeCheckIndex = Inventory.INVENTORY_GRENADES_START_POSITION; grenadeCheckIndex < Inventory.INVENTORY_EQUIPMENTS_START_POSITION; grenadeCheckIndex++)
                    {
                        //Check if the inventory has a free slot and if the grenade bought count is valid
                        if (client.inventory.allSlots[grenadeCheckIndex] == -1 && client.inventory.grenadeBought[elementToBuy - SHOP_GUN_COUNT] < ((Grenade)allShopElements[elementToBuy]).maxQuantity[PartyManager.allPartyModesData.IndexOf(client.party.partyMode)])
                        {
                            inventoryIndex = grenadeCheckIndex;
                            client.inventory.grenadeBought[elementToBuy - SHOP_GUN_COUNT]++;
                            foundPlace = true;
                            break;
                        }
                    }

                    //If the inventory is not full
                    if (foundPlace)
                    {
                        client.inventory.allSlots[inventoryIndex] = elementToBuy;
                    }
                    else
                    {
                        haveError = true;
                    }
                }
                else if (allShopElements[elementToBuy] is Equipment) //If the element to buy is an equipment
                {
                    //If the equipment to buy is a defuse kit
                    if (((Equipment)allShopElements[elementToBuy]).equipmentId == 1)
                    {
                        if (client.inventory.haveDefuseKit)
                        {
                            haveError = true;
                        }
                        else
                        {
                            client.inventory.haveDefuseKit = true;
                        }
                    }
                    else if (((Equipment)allShopElements[elementToBuy]).equipmentId == 2) //If the equipment to buy is a kevlar
                    {
                        if (client.armor == 100)
                        {
                            haveError = true;
                        }
                        else
                        {
                            client.armor = 100;
                        }

                        client.communicator.SendHealth();
                    }
                    else if (((Equipment)allShopElements[elementToBuy]).equipmentId == 3) //If the equipment to buy is a kevlar and helmet
                    {
                        if (client.inventory.haveHelmet)
                        {
                            haveError = true;
                        }
                        else
                        {
                            client.armor = 100;
                            client.inventory.haveHelmet = true;
                        }

                        client.communicator.SendHealth();
                    }
                    else //If the element index is invalid, kick the player
                    {
                        client.communicator.SendError(NetworkDataManager.ErrorType.Null);
                        throw new Exception("elementToBuy is incorrect.");
                    }
                }

                if (!haveError)
                {
                    //Send response

                    if (inventoryIndex != -1)//Place found for the gun or grenade
                    {
                        client.inventory.allSlots[inventoryIndex] = elementToBuy;
                        if (inventoryIndex == 1 || inventoryIndex == 2)
                        {
                            client.inventory.ResetGunAmmo(inventoryIndex);
                        }
                        SendShopConfirm(elementToBuy, inventoryIndex, 1, client);
                    }
                    else //For equipment
                    {
                        SendShopConfirm(elementToBuy, inventoryIndex, 2, client);
                    }

                    client.money -= allShopElements[elementToBuy].price;
                    client.communicator.SendClientInventoryToClients();
                }
                else //Send bad result
                    SendShopConfirm(elementToBuy, 0, 0, client);
            }
            else//Send bad result
            {
                SendShopConfirm(elementToBuy, 0, 0, client);
            }
            //Actualise money to client
            client.communicator.SendMoney();
        }

        /// <summary>
        /// Send the shop response to the client
        /// </summary>
        /// <param name="elementToBuy">Shop Element index to buy</param>
        /// <param name="inventoryIndex">Inventory index of the gun</param>
        /// <param name="result">Request result</param>
        /// <param name="client">Client</param>
        public static void SendShopConfirm(int elementToBuy, int inventoryIndex, int result, Client client)
        {
            Call.Create($"CONFIRM;{0};{elementToBuy};{inventoryIndex};{result}", client);
        }
    }
}
