// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

namespace Counter_Strike_Server
{
    public static class Router
    {
        //Position of the request in the data array
        private const int REQUEST_NAME_INDEX = 0;

        //All request types
        public enum RequestType
        {
            PING = 0,
            STPEP = 1,
            WALLHIT = 2,
            BOMBPLACE = 3,
            BOMBPLACING = 4,
            BOMBDEFUSE = 5,
            CURGUN = 6,
            LEAVE = 7,
            VOTE = 8,
            GRENADE = 9,
            POS = 10,
            SETNAME = 11,
            SHOOT = 12,
            HIT = 13,
            PARTY = 14,
            BUY = 15,
            TEAM = 16,
            KEY = 17,
            SETID = 18,
            TIMER = 19,
            SETMAP = 20,
            SETMODE = 21,
            VOTERESULT = 22,
            CONFIRM = 23,
            SETMONEY = 24,
            SCORE = 25,
            SCRBOARD = 26,
            SETSHOPZONE = 27,
            PARTYROUND = 28,
            SETHEALTH = 29,
            SETBOMB = 30,
            SETCODE = 31,
            HITSOUND = 32,
            ERROR = 33,
            TEXT = 34,
            TEXTPLAYER = 35,
            ADDRANGE = 36,
            ENDGAME = 37,
            ENDUPDATE = 38,
            INVTORY = 39,
            GETBOMB = 40,
            FRAME = 41,
            RELOADED = 42,
            STATUS = 43,
        }

        /// <summary>
        /// Process the request and call the service needed by the request
        /// </summary>
        /// <param name="tempDataSplit">String array of the request data</param>
        /// <param name="client">Client who send the request</param>
        public static void RouteData(string[] tempDataSplit, Client client, int dataLenght)
        {
            Party currentParty = client.party;
            int requestData = int.Parse(tempDataSplit[REQUEST_NAME_INDEX]);
            if (client.checkedKey)
            {
                if (requestData == (int)RequestType.FRAME)//Securised & Verified
                {
                    client.lastFrameCount = int.Parse(tempDataSplit[1]);
                }
                else if (requestData == (int)RequestType.POS)//Securised & Verified
                {
                    client.SetPosition(tempDataSplit[1], tempDataSplit[2], tempDataSplit[3], tempDataSplit[4], tempDataSplit[5]);
                }
                else if (requestData == (int)RequestType.PING)//Securised & Verified
                {
                    client.UpdateClientPing();
                }
                else if (requestData == (int)RequestType.WALLHIT)//Securised & Verified
                {
                    client.communicator.SendWallHit(tempDataSplit[1], tempDataSplit[2], tempDataSplit[3]);
                }
                else if (requestData == (int)RequestType.SHOOT)//Securised & Verified
                {
                    client.communicator.SendShoot();
                }
                else if (requestData == (int)RequestType.HIT)//Securised & Verified
                {
                    int hitCount = (dataLenght - 1) / 4;
                    if (hitCount <= 6)
                    {
                        for (int i = 0; i < hitCount; i++)
                        {
                            client.MakeHit(tempDataSplit[1 + i * 4], tempDataSplit[2 + i * 4], tempDataSplit[3 + i * 4], tempDataSplit[4 + i * 4]);
                        }
                        client.cancelNextHit = true;
                    }
                }
                else if (requestData == (int)RequestType.CURGUN)//Verified & Verified
                {
                    client.inventory.SetCurrentSlot(tempDataSplit[1]);
                }
                else if (requestData == (int)RequestType.RELOADED)//Securised & Verified
                {
                    client.communicator.SendReloaded();
                }
                else if (requestData == (int)RequestType.BOMBPLACE)//Securised & Verified
                {
                    client.PlaceBomb(tempDataSplit[1], tempDataSplit[2], tempDataSplit[3], tempDataSplit[4], false);
                }
                else if (requestData == (int)RequestType.GETBOMB)//Securised & Verified
                {
                    client.GetBomb();
                }
                else if (requestData == (int)RequestType.BOMBPLACING)//Securised & Verified
                {
                    client.communicator.SendBombPlacing();
                }
                else if (requestData == (int)RequestType.BOMBDEFUSE)//Securised & Verified
                {
                    client.DefuseBomb();
                }
                else if (requestData == (int)RequestType.LEAVE)//Securised & Verified
                {
                    client.NeedRemoveConnection = true;
                }
                else if (requestData == (int)RequestType.VOTE)//Securised & Verified
                {
                    if (int.Parse(tempDataSplit[1]) == (int)VoteType.ForceStart)
                    {
                        client.wantStartNow = true;
                        currentParty.communicator.SendVoteResult(VoteType.ForceStart);
                    }
                }
                else if (requestData == (int)RequestType.GRENADE)//Securised & Verified
                {
                    if (!client.isDead)
                    {
                        ShopManager.SendShopConfirm(-1, client.inventory.currentSlot, 1, client);
                        client.communicator.SendGrenade(tempDataSplit[1], tempDataSplit[2], tempDataSplit[3]);
                    }
                }
                else if (requestData == (int)RequestType.SETNAME)//Securised & Verified
                {
                    client.SetName(tempDataSplit[1]);
                }
                else if (requestData == (int)RequestType.PARTY)//Securised & Verified
                {
                    NetworkDataManager.JoinType joinType = (NetworkDataManager.JoinType)int.Parse(tempDataSplit[1]);
                    if (joinType == NetworkDataManager.JoinType.JOIN_RANDOM_PARTY) //JOIN RANDOM PARTY
                    {
                        client.PutClientIntoParty("");
                    }
                    else if (joinType == NetworkDataManager.JoinType.CREATE_PRIVATE_PARTY) //CREATE PRIVATE PARTY
                    {
                        PartyManager.CreateParty(client, true);
                    }
                    else if (joinType == NetworkDataManager.JoinType.JOIN_PRIVATE_PARTY)
                    {
                        client.PutClientIntoParty(tempDataSplit[2]);
                    }

                    if (client.party != null)
                    {
                        currentParty = client.party;
                        client.communicator.SendPartyData();
                        if (!currentParty.partyStarted && currentParty.allConnectedClients.Count == Settings.maxPlayerPerParty)
                        {
                            if (currentParty.partyTimer > PartyManager.startFullPartyWaitingTime)
                            {
                                currentParty.partyTimer = PartyManager.startFullPartyWaitingTime;
                                currentParty.communicator.SendPartyTimer();
                            }
                        }
                    }
                }
                else if (requestData == (int)RequestType.BUY)//Securised & Verified //A client want to buy a gun
                {
                    int elementToBuy = int.Parse(tempDataSplit[1]);
                    ShopManager.Buy(elementToBuy, client);
                }
                else if (requestData == (int)RequestType.TEAM)//Securised //Update team for a client
                {
                    client.SetTeam(tempDataSplit[1]);
                }
            }
            else
            {
                if (requestData == (int)RequestType.KEY)//Securised & Verified
                {
                    //Check the game version first
                    client.CheckPlayerInfo(tempDataSplit[2], tempDataSplit[4]);
                    client.SetName(tempDataSplit[3]);

                    Security.CheckClientKey(client, int.Parse(tempDataSplit[1]));
                }
                else if (requestData == (int)RequestType.STATUS)//Securised & Verified
                {
                    Api.SendServerStatus(client);
                    client.NeedRemoveConnection = true;
                }
            }
        }
    }
}
