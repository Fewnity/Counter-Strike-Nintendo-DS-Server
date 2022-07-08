namespace Counter_Strike_Server
{
    /// <summary>
    /// A party communicator is used to send data of the party/clients to clients of the party
    /// </summary>
    public class PartyCommunicator
    {
        readonly Party party;

        public PartyCommunicator(Party party)
        {
            this.party = party;
        }

        /// <summary>
        /// Send clients position to all clients
        /// </summary>
        /// <param name="client"></param>
        public void SendPlayersPositions()
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                Client client = party.allConnectedClients[i];
                Call.Create($"POS;{client.id};{client.position.x};{client.position.y};{client.position.z};{client.angle};{client.cameraAngle}", party.allConnectedClients);
            }
        }

        /// <summary>
        /// Send a notification based on one or players
        /// </summary>
        /// <param name="clientA"></param>
        /// <param name="clientB"></param>
        /// <param name="textId"></param>
        public void SendTextPlayer(Client clientA, Client clientB, int textId)
        {
            int clientAId = clientA != null ? clientA.id : -1;
            int clientBId = clientB != null ? clientB.id : -1;
            //Party party = clientA != null ? clientA.party : clientB.party;
            Call.Create($"TEXTPLAYER;{clientAId};{clientBId};{textId}", party.allConnectedClients);
        }

        /// <summary>
        /// Send the score of a client to all client
        /// </summary>
        /// <param name="party"></param>
        public void SendKillCountAndDeathCount()
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
            {
                //SendKillCountAndDeathCount(party.allConnectedClients[i]);
                party.allConnectedClients[i].communicator.SendKillCountAndDeathCount();
            }
        }

        /// <summary>
        /// Say to all players to set the shop zone
        /// </summary>
        /// <param name="party">Party</param>
        public void SendSetShopZone()
        {
            Call.Create("SETSHOPZONE", party.allConnectedClients);
        }

        /// <summary>
        /// Send text to show to all client
        /// </summary>
        /// <param name="text">Text enum to show</param>
        /// <param name="party">Party</param>
        public void SendText(PartyManager.TextEnum text)
        {
            Call.Create($"TEXT;{(int)text}", party.allConnectedClients);
        }

        /// <summary>
        /// Show party score
        /// </summary>
        /// <param name="party">Party</param>
        public void SendScore()
        {
            Call.Create($"SCORE;{party.counterScore};{party.terroristsScore}", party.allConnectedClients);
        }

        /// <summary>
        /// Send party current round to all clients
        /// </summary>
        /// <param name="party">Party</param>
        public void SendPartyRound()
        {
            Call.Create($"PartyRound;{(int)party.roundState}", party.allConnectedClients);
        }

        /// <summary>
        /// Send party timer
        /// </summary>
        /// <param name="party">Party</param>
        public void SendPartyTimer()
        {
            SendPartyTimer(party.partyTimer.Minute, party.partyTimer.Second);
        }

        /// <summary>
        /// Send party timer
        /// </summary>
        /// <param name="party">Party</param>
        /// <param name="minutes">Minutes</param>
        /// <param name="seconds">Seconds</param>
        public void SendPartyTimer(int minutes, int seconds)
        {
            Call.Create($"TimerA;{minutes};{seconds}", party.allConnectedClients);
        }


        /// <summary>
        /// Send current vote result to all clients
        /// </summary>
        /// <param name="currentParty">Party</param>
        /// <param name="type">Vote type</param>
        public void SendVoteResult(VoteType type)
        {
            if (type == VoteType.ForceStart)
            {
                //Count every vote
                int forceCount = 0;
                foreach (Client currentClient in party.allConnectedClients)
                {
                    if (currentClient.wantStartNow)
                    {
                        forceCount++;
                    }
                }
                int count = party.allConnectedClients.Count - 2;
                if (party.allConnectedClients.Count <= 3)
                {
                    count = 2;
                }

                //Send data
                Call.Create($"VOTERESULT;{0};{forceCount};{count}", party.allConnectedClients);
            }
        }

        /// <summary>
        /// Send players team to all client
        /// </summary>
        /// <param name="party"></param>
        public void SendTeam()
        {
            for (int i = 0; i < party.allConnectedClients.Count; i++)
                Call.Create($"TEAM;{party.allConnectedClients[i].id};{(int)party.allConnectedClients[i].team}", party.allConnectedClients);
        }

        /// <summary>
        /// Send money of each players to all players
        /// </summary>
        /// <param name="party"></param>
        public void SendMoney()
        {
            //Send teams info to all players
            for (int i = 0; i < party.allConnectedClients.Count; i++)
                Call.Create($"SETMONEY;{party.allConnectedClients[i].money}", party.allConnectedClients[i]);
        }

        /// <summary>
        /// Send the bomb position to all clients
        /// </summary>
        /// <param name="party">Party</param>
        public void SendBombPosition()
        {
            int dropInt = 0;
            if (party.bombDropped)
                dropInt = 1;

            Call.Create($"BOMBPLACE;{party.bombPosition.x};{party.bombPosition.y};{party.bombPosition.z};{party.bombPosition.w};{dropInt}", party.allConnectedClients);
        }
    }
}
