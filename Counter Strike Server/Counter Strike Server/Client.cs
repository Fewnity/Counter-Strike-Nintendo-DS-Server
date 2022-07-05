// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Net.Sockets;

namespace Counter_Strike_Server
{

    public class AmmoMagazine
    {
        public int AmmoCount;
        public int TotalAmmoCount;
    }

    public class Client
    {
        //Client connections
        public TcpClient currentClientTcp;
        public NetworkStream currentClientStream;
        public string data;

        //player informations
        public string macAddress = "null";
        public string name = "player";

        public int id;
        public teamEnum team = teamEnum.SPECTATOR; //-1 no team, 0 terrorist, 1 counter terrorist
        public bool isDead;
        public int health;
        public int armor;
        public int money;
        public int killCount;
        public int deathCount;
        public bool NeedRemoveConnection;

        public int currentGunInInventory;

        public AmmoMagazine[] AllAmmoMagazine = new AmmoMagazine[2];
        public int[] allGunsInInventory = new int[InventoryManager.INVENTORY_SIZE];
        public int[] grenadeBought = new int[5];
        public bool haveHelmet;
        public bool haveDefuseKit;
        public bool haveBomb;

        public DateTime lastPing = DateTime.Now;
        public bool needRespawn;
        public DateTime respawnTimer = new DateTime(2000, 1, 1, 0, 0, 0);
        public int positionErrorCount = 0;
        public int ping = 0;
        public Party clientParty;
        public Vector3Int Position = new Vector3Int(0,-100 * 4096, 0);
        public int angle = 0;
        public float cameraAngle = 128;
        public bool wantStartNow = false;
        public int sentKey = 0;
        public bool checkedKey = false;
        public int killedFriend = 0;
        public bool cancelNextHit = false;
        public int lastFrameCount = 0;
        public bool removed = false;
    }
}
