// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System;

namespace Counter_Strike_Server
{
    public class PartyModeData
    {
        public PartyModeData(bool middlePartyTeamSwap, int maxRound, int startMoney, int maxMoney, int winTheRoundMoney, int winTheRoundBombMoney, int loseTheRoundMoney, int loseIncrease, int defuseBombMoneyBonus, int plantBombMoneyBonus, int plantedBombLoseMoneyBonus, int killPenalties, bool noMoneyOnTimeEnd, bool teamDamage)
        {
            this.middlePartyTeamSwap = middlePartyTeamSwap;
            this.maxRound = maxRound;
            this.startMoney = startMoney;
            this.maxMoney = maxMoney;
            this.winTheRoundMoney = winTheRoundMoney;
            this.winTheRoundBombMoney = winTheRoundBombMoney;
            this.loseTheRoundMoney = loseTheRoundMoney;
            this.loseIncrease = loseIncrease;
            this.defuseBombMoneyBonus = defuseBombMoneyBonus;
            this.plantBombMoneyBonus = plantBombMoneyBonus;
            this.plantedBombLoseMoneyBonus = plantedBombLoseMoneyBonus;
            this.killPenalties = killPenalties;
            this.noMoneyOnTimeEnd = noMoneyOnTimeEnd;
            this.teamDamage = teamDamage;
        }
        public bool middlePartyTeamSwap;
        public int maxRound;
        public int startMoney;
        public int maxMoney;

        public int winTheRoundMoney;
        public int winTheRoundBombMoney;
        public int loseTheRoundMoney;
        public int loseIncrease;
        public int defuseBombMoneyBonus;
        public int plantBombMoneyBonus;

        public int plantedBombLoseMoneyBonus;
        public int killPenalties;

        public bool teamDamage;
        public bool noMoneyOnTimeEnd;

        public DateTime trainingTime = new(2000, 1, 1, 0, 0, 20);
        public DateTime startRoundWaitingTime = new(2000, 1, 1, 0, 0, 15);
        public DateTime roundTime = new(2000, 1, 1, 0, 2, 15);
        public DateTime endRoundWaitingTime = new(2000, 1, 1, 0, 0, 5);
        public DateTime bombWaitingTime = new(2000, 1, 1, 0, 0, 45);
        public DateTime trainingRespawnTimer = new(2000, 1, 1, 0, 0, 5);

        //TO DO
        public bool spawnWithArmor;
        //TO DO
        public bool infiniteMoney;
        //TO DO
        public bool infiniteTimer;
        //TO DO
        public bool noScore;
        //TO DO
        public bool limitedShopByZoneAndTimer;
        //TO DO
        public bool infiniteGunAmmo;
    }
}
