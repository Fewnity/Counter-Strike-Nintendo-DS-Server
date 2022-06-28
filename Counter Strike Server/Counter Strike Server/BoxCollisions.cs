// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

namespace Counter_Strike_Server
{
    public class BoxCollisions
    {
        public BoxCollisions(int corner1, int corner2, int corner3, int corner4)
        {
            this.corner1 = corner1;
            this.corner2 = corner2;
            this.corner3 = corner3;
            this.corner4 = corner4;
        }

        public BoxCollisions()
        {
            corner1 = 0;
            corner2 = 0;
            corner3 = 0;
            corner4 = 0;
        }

        public float corner1;
        public float corner2;
        public float corner3;
        public float corner4;
    }
}
