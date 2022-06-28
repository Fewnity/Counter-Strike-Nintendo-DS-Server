// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;
using System.Collections.Generic;

namespace Counter_Strike_Server
{
    public enum RoundState
    {
        TRAINING = -1,
        WAIT_START = 0,
        PLAYING = 1,
        END_ROUND = 2,
        END = 3,
    };

    public class Party
    {
        public List<Client> allConnectedClients = new List<Client>();
        public int counterScore;
        public int terroristsScore;
        public bool partyStarted;
        public RoundState roundState = RoundState.TRAINING;
        public DateTime partyTimer;
        public bool bombSet;
        public bool bombDropped;
        public PartyModeData partyMode;
        public mapEnum mapId;
        public int round;
        public int loseCountCounterTerrorists;
        public int loseCountTerrorists;
        public Vector4 bombPosition = new Vector4(0,0,0, 0);
        public BoxCollisions defuseZoneCollisions;
        public List<Physics.Grenade> allGrenades = new List<Physics.Grenade>();
        public string password;
        public bool isPrivate;
        public bool needTeamEquilibration;
    }
}
