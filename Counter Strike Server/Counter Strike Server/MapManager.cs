// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Counter_Strike_Server
{
    static class MapManager
    {
        public static List<MapData> allMaps = new List<MapData>();

        /// <summary>
        /// Load maps data
        /// </summary>
        public static void LoadMapsData()
        {
            //I don't know how to use a json lib with Mono for Linux
            /*if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\CounterStrikeDsServer"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\CounterStrikeDsServer");

            string SettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\CounterStrikeDsServer\\mapsData.json";
            Console.WriteLine(SettingsPath);
            StreamReader Reader = new StreamReader(SettingsPath);
            allMaps = JsonConvert.DeserializeObject<List<MapData>>(Reader.ReadToEnd());
            Console.WriteLine(allMaps[(int)mapEnum.DUST2].mapId);
            Console.WriteLine(allMaps[(int)mapEnum.TUTORIAL].mapId);*/
            MapData newMap = new MapData();
            newMap.mapId = mapEnum.DUST2;

            newMap.allTerroristsSpawns.Add(new Vector3(-3, 6.43f, 65));
            newMap.allTerroristsSpawns.Add(new Vector3(-6, 6.43f, 65));
            newMap.allTerroristsSpawns.Add(new Vector3(-9, 6.43f, 65));
            newMap.allTerroristsSpawns.Add(new Vector3(-3, 6.43f, 62.5f));
            newMap.allTerroristsSpawns.Add(new Vector3(-6, 6.43f, 62.5f));
            newMap.allTerroristsSpawns.Add(new Vector3(-9, 6.43f, 62.5f));

            newMap.allCounterTerroristsSpawns.Add(new Vector3(16, 0, -22));
            newMap.allCounterTerroristsSpawns.Add(new Vector3(19.5f, 0, -22));
            newMap.allCounterTerroristsSpawns.Add(new Vector3(23, 0, -22));
            newMap.allCounterTerroristsSpawns.Add(new Vector3(16, 0, -19.5f));
            newMap.allCounterTerroristsSpawns.Add(new Vector3(19.5f, 0, -19.5f));
            newMap.allCounterTerroristsSpawns.Add(new Vector3(23, 0, -19.5f));

            newMap.AllBombsTriggersCollisions.Add(SetBombZone(40.8f, -20.8f, 5, 5));
            newMap.AllBombsTriggersCollisions.Add(SetBombZone(-28.03f, -27.07f, 4.46785f, 4.578236f));

            newMap.terroristsSpawnsAngle = 0;
            newMap.counterTerroristsSpawnsAngle = 256;

            allMaps.Add(newMap);
        }

        /// <summary>
        /// Set bomb's zone position and size (2D)
        /// </summary>
        /// <param name="xPos">X position</param>
        /// <param name="zPos">Z position</param>
        /// <param name="xSize">X size</param>
        /// <param name="zSize">Z size</param>
        /// <returns></returns>
        static BoxCollisions SetBombZone(float xPos, float zPos, float xSize, float zSize)
        {
            BoxCollisions newBoxCollisions = new BoxCollisions();
            newBoxCollisions.corner1 = (xPos + xSize / 2.0f) * 4096;
            newBoxCollisions.corner2 = (xPos - xSize / 2.0f) * 4096;
            newBoxCollisions.corner3 = (zPos + zSize / 2.0f) * 4096;
            newBoxCollisions.corner4 = (zPos - zSize / 2.0f) * 4096;

            return newBoxCollisions;
        }

        /// <summary>
        /// Set bomb's defuse zone position and size (2D)
        /// </summary>
        /// <param name="party">Party</param>
        public static void SetBombDefuseZone(Party party)
        {
            BoxCollisions newBoxCollisions = new BoxCollisions();
            newBoxCollisions.corner1 = party.bombPosition.x + 4096;
            newBoxCollisions.corner2 = party.bombPosition.x - 4096;
            newBoxCollisions.corner3 = party.bombPosition.z + 4096;
            newBoxCollisions.corner4 = party.bombPosition.z - 4096;

            party.defuseZoneCollisions = newBoxCollisions;
        }

        /// <summary>
        /// Set bomb's drop zone position and size (2D)
        /// </summary>
        /// <param name="party">Party</param>
        public static void SetBombDropZone(Party party)
        {
            BoxCollisions newBoxCollisions = new BoxCollisions();
            newBoxCollisions.corner1 = party.bombPosition.x + 3277;
            newBoxCollisions.corner2 = party.bombPosition.x - 3277;
            newBoxCollisions.corner3 = party.bombPosition.z + 3277;
            newBoxCollisions.corner4 = party.bombPosition.z - 3277;

            party.defuseZoneCollisions = newBoxCollisions;
        }
    }
}
