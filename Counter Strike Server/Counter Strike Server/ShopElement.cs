// SPDX-License-Identifier: MIT
//
// Copyright (c) 2021-2022, Fewnity - Grégory Machefer
//
// This file is part of the server of Counter Strike Nintendo DS Multiplayer Edition (CS:DS)

using System.Collections.Generic;

namespace Counter_Strike_Server
{
    public class ShopElement
    {
        public int price;

        public ShopElement(int price)
        {
            this.price = price;
        }
    }

    public class Gun : ShopElement
    {
        public List<int> killMoneyBonus = new List<int>();
        public bool isBigGun;
        public int damage;
        public float penetration;
        public int magazineCapacity;
        public int maxAmmoCount;
        public float damageFalloff;

        public Gun(List<int> killMoneyBonus, bool isBigGun, int price, int damage, float penetration, int magazineCapacity, int maxAmmoCount, float damageFalloff) : base(price)
        {
            this.killMoneyBonus = new List<int>(killMoneyBonus);
            this.isBigGun = isBigGun;
            this.damage = damage;
            this.penetration = penetration;
            this.magazineCapacity = magazineCapacity;
            this.maxAmmoCount = maxAmmoCount;
            this.damageFalloff = damageFalloff;
        }
    }

    public class Grenade : ShopElement
    {
        public List<int> killMoneyBonus = new List<int>();
        public List<int> maxQuantity = new List<int>();
        public int id;

        public Grenade(List<int> killMoneyBonus, List<int> maxQuantity, int price, int id) : base(price)
        {
            this.killMoneyBonus = new List<int>(killMoneyBonus);
            this.maxQuantity = new List<int>(maxQuantity);
            this.id = id;
        }
    }

    public class Equipment : ShopElement
    {
        public int equipmentId;

        public Equipment(int equipmentId, int price) : base(price)
        {
            this.equipmentId = equipmentId;
        }
    }
}
