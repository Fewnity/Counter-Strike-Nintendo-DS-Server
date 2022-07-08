using System.Text;
using System.Globalization;

namespace Counter_Strike_Server
{
    /// <summary>
    /// A client communicator is used to send data of the client to clients of the party
    /// </summary>
    public class ClientCommunicator
    {
        readonly Client client;

        public ClientCommunicator(Client client)
        {
            this.client = client;
        }

        /// <summary>
        /// Send client health to all clients
        /// </summary>
        public void SendHealth()
        {
            Call.Create($"SETHEALTH;{client.id};{client.health};{client.armor};{(client.inventory.haveHelmet ? 1 : 0)}", client.party.allConnectedClients);
        }

        /// <summary>
        /// Say to all players to set the shop zone
        /// </summary>
        public void SendSetShopZone()
        {
            Call.Create("SETSHOPZONE", client);
        }

        /// <summary>
        /// Send the score of the client to all client
        /// </summary>
        public void SendKillCountAndDeathCount()
        {
            Call.Create($"SCRBOARD;{client.id};{client.killCount};{client.deathCount}", client.party.allConnectedClients);
        }

        /// <summary>
        /// Send step sound to all clients except client who send the step
        /// </summary>
        public void SendStep()
        {
            Call.Create($"STEP;{client.id}", client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send clients position to the client
        /// </summary>
        public void SendPlayersPositions()
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
            {
                Client tempClient = client.party.allConnectedClients[i];
                if (client != tempClient)
                    Call.Create($"POS;{tempClient.id};{tempClient.position.x};{tempClient.position.y};{tempClient.position.z};{tempClient.angle};{tempClient.cameraAngle}", client);
            }
        }

        /// <summary>
        /// Send client position to clients
        /// </summary>
        /// <param name="includeHim">Include the client in the clients list of the call?</param>
        public void SendPlayerPosition(bool includeHim)
        {
            string request = $"POS;{client.id};{client.position.x};{client.position.y};{client.position.z};{client.angle};{client.cameraAngle}";
            if (includeHim)
                Call.Create(request, client.party.allConnectedClients);
            else
                Call.Create(request, client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send money of a client to this client
        /// </summary>
        /// <param name="client">Client</param>
        public void SendMoney()
        {
            //Send teams info to all players
            Call.Create($"SETMONEY;{client.money}", client);
        }

        /// <summary>
        /// Send bomb placing or defusing notification
        /// </summary>
        /// <param name="client"></param>
        public void SendBombPlacing()
        {
            if (!client.isDead && ((client.inventory.haveBomb && client.team == TeamEnum.TERRORISTS) || client.team == TeamEnum.COUNTERTERRORISTS) && PhysicsManager.CheckBombZone(client.position, MapManager.allMaps[(int)client.party.mapId]))
                Call.Create($"BOMBPLACING;{client.id}", client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send party current round to a client
        /// </summary>
        /// <param name="client">Client</param>
        public void SendPartyRound()
        {
            Call.Create($"PartyRound;{(int)client.party.roundState}", client);
        }

        /// <summary>
        /// Send party data to the new client
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="client">Client</param>
        public void SendPartyData()
        {
            Call.Create($"ADDRANGE;{client.id}", client.party.allConnectedClients, client);

            //Send new client ID to new client
            Call.Create($"SETID;{client.id}", client);

            Call.Create($"SETMAP;{(int)client.party.mapId}", client);
            Call.Create($"SETMODE;{PartyManager.allPartyModesData.IndexOf(client.party.partyMode)}", client);

            client.communicator.SendName(client, false);
            client.party.communicator.SendVoteResult(VoteType.ForceStart);
            SendPartyRound();

            if (client.party.isPrivate)
                Call.Create($"SETCODE;{client.party.password}", client);

            //Send all party's clients ID to new client
            StringBuilder NewCallData = new();

            NewCallData.Append("ADDRANGE;");
            for (int ClientI = 0; ClientI < client.party.allConnectedClients.Count; ClientI++)
                if (client.party.allConnectedClients[ClientI] != client)
                {
                    NewCallData.Append(client.party.allConnectedClients[ClientI].id);
                    NewCallData.Append(';');
                }

            NewCallData.Remove(NewCallData.Length - 1, 1);
            Call.Create(NewCallData.ToString(), client);

            //Send all clients names to the client
            for (int ClientI = 0; ClientI < client.party.allConnectedClients.Count; ClientI++)
            {
                Client client1 = client.party.allConnectedClients[ClientI];
                if (client1 != client)
                {
                    client1.communicator.SendName(client, true);
                }
            }

            client.money = client.party.partyMode.maxMoney;
            SendMoney();

            client.ResetPlayer();

            client.communicator.SendClientsInventoryToClient();

            client.UpdatePartyToClient();
            if (client.party.bombDropped || client.party.bombSet)
                client.communicator.SendBombPosition();
            else
            {
                client.communicator.SendWhoHasTheBomb();
            }
            client.party.communicator.SendPartyTimer();
            Call.Create("ENDUPDATE", client);
        }

        /// <summary>
        /// Send an error to the client
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="errorType">Error type</param>
        public void SendError(NetworkDataManager.ErrorType errorType)
        {
            Call.Create($"ERROR;{(int)errorType}", client);
        }

        /// <summary>
        /// Send name of a client
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="toHim">Send to name </param>
        public void SendName(Client destinator, bool toHim)
        {
            string txt = $"SETNAME;{client.id};{client.name}";
            if (!toHim)
                Call.Create(txt, destinator.party.allConnectedClients, destinator);
            else
                Call.Create(txt, destinator);
        }

        /// <summary>
        /// Send greande to all clients except the passed client (String version)
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="xDirection">X direction</param>
        /// <param name="yDirection">Y direction</param>
        /// <param name="zDirection">Z direction</param>
        public void SendGrenade(string xDirection, string yDirection, string zDirection)
        {
            SendGrenade(float.Parse(xDirection, CultureInfo.InvariantCulture), float.Parse(yDirection, CultureInfo.InvariantCulture), float.Parse(zDirection, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Send greande to all clients except the passed client
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="xDirection">X direction</param>
        /// <param name="yDirection">Y direction</param>
        /// <param name="zDirection">Z direction</param>
        public void SendGrenade(float xDirection, float yDirection, float zDirection)
        {
            if (client.inventory.GetCurrentSlotValue() != -1 && ShopManager.allShopElements[client.inventory.GetCurrentSlotValue()] is Grenade)
            {
                int grenadeId = (ShopManager.allShopElements[client.inventory.GetCurrentSlotValue()] as Grenade).id;
                Call.Create($"GRENADE;{xDirection.ToString().Replace(',', '.')};{yDirection.ToString().Replace(',', '.')};{zDirection.ToString().Replace(',', '.')};{client.position.x};{client.position.y};{client.position.z};{grenadeId}", client.party.allConnectedClients, client);

                client.inventory.SetCurrentSlotValue(-1);
                Physics.Grenade newGrenade = Physics.CreateGrenade(client, grenadeId, xDirection, yDirection, zDirection);

                //Calculate 4 seconds of grenade physics
                for (int iGrenade = 0; iGrenade < 60 * 4; iGrenade++)
                {
                    PhysicsManager.physicsEngine.NE_PhysicsUpdateOneGrenade(newGrenade);
                }
            }
        }

        /// <summary>
        /// Send client ping
        /// </summary>
        /// <param name="client"></param>
        public void SendPing()
        {
            Call.Create($"PING;{client.ping}", client);
        }

        /// <summary>
        /// Send client current gun to all clients
        /// </summary>
        /// <param name="client"></param>
        public void SendClientCurrentGunToClients()
        {
            Call.Create($"CURGUN;{client.id};{client.inventory.currentSlot}", client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send client gun reloaded
        /// </summary>
        /// <param name="client"></param>
        public void SendReloaded()
        {
            Call.Create($"RELOADED;{client.id};{0}", client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send clients current gun
        /// </summary>
        /// <param name="client"></param>
        public void SendClientsCurrentGunToClient()
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
            {
                if (client.party.allConnectedClients[i] != client)
                    Call.Create($"CURGUN;{client.party.allConnectedClients[i].id};{client.party.allConnectedClients[i].inventory.currentSlot}", client);
            }
        }

        /// <summary>
        /// Send client ammo to clients
        /// </summary>
        /// <param name="client"></param>
        public void SendClientsAmmoToClient()
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
            {
                Client clientToCheck = client.party.allConnectedClients[i];
                if (client.party.allConnectedClients[i] != client)
                    Call.Create($"AMMO;{clientToCheck.id};{clientToCheck.inventory.AllAmmoMagazine[0].AmmoCount};{clientToCheck.inventory.AllAmmoMagazine[0].TotalAmmoCount};{clientToCheck.inventory.AllAmmoMagazine[1].AmmoCount};{clientToCheck.inventory.AllAmmoMagazine[1].TotalAmmoCount}", client);
            }
        }

        /// <summary>
        /// Send client inventory to all clients
        /// </summary>
        /// <param name="client">Client</param>
        public void SendClientInventoryToClients()
        {
            string NewCallData = $"INVTORY;{client.id};";
            for (int gunIndex = 0; gunIndex < client.inventory.allSlots.Length; gunIndex++)
                NewCallData += $"{client.inventory.allSlots[gunIndex]};";

            NewCallData = NewCallData.Remove(NewCallData.Length - 1);
            Call.Create(NewCallData, client.party.allConnectedClients);
        }

        /// <summary>
        /// Send client inventory to the clients list
        /// </summary>
        /// <param name="clientDestination"></param>
        public void SendClientsInventoryToClient()
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
            {
                Client client = this.client.party.allConnectedClients[i];
                if (client != this.client)
                {
                    string NewCallData = $"INVTORY;{client.id};";
                    for (int gunIndex = 0; gunIndex < client.inventory.allSlots.Length; gunIndex++)
                        NewCallData += $"{client.inventory.allSlots[gunIndex]};";

                    NewCallData = NewCallData.Remove(NewCallData.Length - 1);
                    Call.Create(NewCallData, this.client);
                }
            }
        }

        /// <summary>
        /// Send player hit sound
        /// </summary>
        /// <param name="client"></param>
        /// <param name="hittedPlayerId"></param>
        /// <param name="hitType"></param>
        public void SendHitSound(int hittedPlayerId, int hitType)
        {
            Call.Create($"HITSOUND;{client.id};{hittedPlayerId};{hitType}", client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send shoot of a client to all client excepted the passed client
        /// </summary>
        /// <param name="client">Client</param>
        public void SendShoot()
        {
            client.cancelNextHit = false;

            if (client.isDead)
                return;

            if (client.inventory.currentSlot == 1 || client.inventory.currentSlot == 2)
            {
                Inventory.AmmoMagazine ammoMagazine = client.inventory.AllAmmoMagazine[client.inventory.currentSlot - 1];
                if (ammoMagazine.AmmoCount > 0)
                    ammoMagazine.AmmoCount--;
                else
                {
                    client.inventory.ReloadGun();
                    //Security check
                    if (ammoMagazine.AmmoCount == 0)
                    {
                        client.cancelNextHit = true;
                    }
                    else
                    {
                        ammoMagazine.AmmoCount--;
                    }
                }

            }

            if (!client.cancelNextHit)
            {
                Call.Create($"SHOOT;{client.id};{client.inventory.GetCurrentSlotValue()}", client.party.allConnectedClients, client);
            }
        }

        /// <summary>
        /// Send a wall hit to all client excepted the passed client
        /// </summary>
        /// <param name="xPos">X position</param>
        /// <param name="yPos">Y position</param>
        /// <param name="zPos">Z position</param>
        /// <param name="client">Client</param>
        public void SendWallHit(string xPos, string yPos, string zPos)
        {
            if (client.isDead)
                return;

            int x = int.Parse(xPos);
            int y = int.Parse(yPos);
            int z = int.Parse(zPos);
            Call.Create($"WALLHIT;{x};{y};{z}", client.party.allConnectedClients, client);
        }

        /// <summary>
        /// Send to the client who has the bomb
        /// </summary>
        /// <param name="clientDestination"></param>
        public void SendWhoHasTheBomb()
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
            {
                if (client.party.allConnectedClients[i].inventory.haveBomb)
                {
                    client.party.allConnectedClients[i].communicator.SendClientBomb(client, true);
                    break;
                }
            }
        }

        /// <summary>
        /// Send to all clients if this client has the bomb or not
        /// </summary>
        /// <param name="client"></param>
        /// <param name="haveBomb"></param>
        public void SendClientBomb(bool haveBomb)
        {
            int haveBombInt = 0;
            if (haveBomb)
                haveBombInt = 1;

            //Send if the player has the bomb to all clients
            Call.Create($"SETBOMB;{client.id};{haveBombInt}", client.party.allConnectedClients);
        }

        /// <summary>
        /// Send to a client if this client has the bomb or not
        /// </summary>
        /// <param name="client">Client to get the info</param>
        /// <param name="clientDestination">Receiver</param>
        /// <param name="haveBomb">Have the bomb?</param>
        public void SendClientBomb(Client clientDestination, bool haveBomb)
        {
            int haveBombInt = 0;
            if (haveBomb)
                haveBombInt = 1;

            //Send if the player has the bomb to all clients
            Call.Create($"SETBOMB;{client.id};{haveBombInt}", clientDestination);
        }


        /// <summary>
        /// Send players team to the client
        /// </summary>
        /// <param name="client"></param>
        public void SendTeam()
        {
            for (int i = 0; i < client.party.allConnectedClients.Count; i++)
                Call.Create($"TEAM;{client.party.allConnectedClients[i].id};{(int)client.party.allConnectedClients[i].team}", client);
        }

        /// <summary>
        /// Send bomb position to the client
        /// </summary>
        /// <param name="client"></param>
        public void SendBombPosition()
        {
            int dropInt = 0;
            if (client.party.bombDropped)
                dropInt = 1;

            Call.Create($"BOMBPLACE;{client.party.bombPosition.x};{client.party.bombPosition.y};{client.party.bombPosition.z};{client.party.bombPosition.w};{dropInt}", client);
        }
    }
}
