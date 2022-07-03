// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System.Collections.Generic;

namespace Counter_Strike_Server
{
    public enum mapEnum
    {
        DUST2 = 0,
        TUTORIAL = 1
    };

    public class MapData
    {
        public mapEnum mapId;
        public int terroristsSpawnsAngle;
        public int counterTerroristsSpawnsAngle;
        public List<Vector3> allTerroristsSpawns = new List<Vector3>();
        public List<Vector3> allCounterTerroristsSpawns = new List<Vector3>();
        public List<BoxCollisions> AllBombsTriggersCollisions = new List<BoxCollisions>();
    }
}
