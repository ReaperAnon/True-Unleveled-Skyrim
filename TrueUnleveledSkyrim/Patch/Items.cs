using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Plugins.Cache;

namespace TrueUnleveledSkyrim.Patch
{
    class ItemsPatcher
    {
        // A struct to hold the original and morrowloot-inspired stats of an armor.
        private readonly struct ArmorValues
        {
            public readonly float ArmorValue { get; }
            public readonly float ArmorWeight { get; }
            public readonly double ArmorPrice { get; }

            public readonly float ArmorValueMod { get; }
            public readonly float ArmorWeightMod { get; }
            public readonly double ArmorPriceMod { get; }

            public ArmorValues(float armorValue, float armorWeight, uint armorPrice, float armorValueMod, float armorWeightMod, uint armorPriceMod)
            {
                ArmorValue = armorValue; ArmorWeight = armorWeight; ArmorPrice = armorPrice;
                ArmorValueMod = armorValueMod; ArmorWeightMod = armorWeightMod; ArmorPriceMod = armorPriceMod;
            }
        };

        // A struct to hold the original and morrowloot-inspired stats of a weapon.
        private readonly struct WeaponValues
        {
            public float WeaponDamage { get; }
            public float WeaponWeight { get; }
            public double WeaponPrice { get; }
            public float WeaponSpeed { get; }
            public float WeaponCritDamage { get; }
            public float WeaponCritMult { get; }

            public float WeaponDamageMod { get; }
            public float WeaponWeightMod { get; }
            public double WeaponPriceMod { get; }
            public float WeaponSpeedMod { get; }
            public float WeaponCritDamageMod { get; }
            public float WeaponCritMultMod { get; }

            public WeaponValues(float weaponDamage, float weaponWeight, float weaponPrice, float weaponSpeed, float weaponCritDamage, float weaponCritMult, float weaponDamageMod, float weaponWeightMod, float weaponPriceMod, float weaponSpeedMod, float weaponCritDamageMod, float weaponCritMultMod)
            {
                WeaponDamage = weaponDamage; WeaponWeight = weaponWeight; WeaponPrice = weaponPrice; WeaponSpeed = weaponSpeed; WeaponCritDamage = weaponCritDamage; WeaponCritMult = weaponCritMult;
                WeaponDamageMod = weaponDamageMod; WeaponWeightMod = weaponWeightMod; WeaponPriceMod = weaponPriceMod; WeaponSpeedMod = weaponSpeedMod; WeaponCritDamageMod = weaponCritDamageMod; WeaponCritMultMod = weaponCritMultMod;
            }
        };

        // Read-only dictionary to match weapon materials and types to their corresponding stats.
        private static readonly IReadOnlyDictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>> weaponKeys = new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>>()
        {
            {
                Skyrim.Keyword.WeapMaterialDaedric, new Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>()
                {
                    {Skyrim.Keyword.WeapTypeBattleaxe,      new WeaponValues(weaponDamage: 25, weaponWeight: 27, weaponPrice: 2750, weaponSpeed: 0.7f, weaponCritDamage: 12, weaponCritMult: 1,
                                                                        weaponDamageMod: 32, weaponWeightMod: 26, weaponPriceMod: 4320, weaponSpeedMod: 0.7f, weaponCritDamageMod: 16, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeBow,            new WeaponValues(weaponDamage: 19, weaponWeight: 18, weaponPrice: 2500, weaponSpeed: 0.5f, weaponCritDamage: 9, weaponCritMult: 1,
                                                                        weaponDamageMod: 25, weaponWeightMod: 16, weaponPriceMod: 3150, weaponSpeedMod: 0.5625f, weaponCritDamageMod: 13, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeDagger,         new WeaponValues(weaponDamage: 11, weaponWeight: 6, weaponPrice: 500, weaponSpeed: 1.3f, weaponCritDamage: 5, weaponCritMult: 1,
                                                                        weaponDamageMod: 16, weaponWeightMod: 5, weaponPriceMod: 1650, weaponSpeedMod: 1.43f, weaponCritDamageMod: 8, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeGreatsword,     new WeaponValues(weaponDamage: 24, weaponWeight: 23, weaponPrice: 2500, weaponSpeed: 0.75f, weaponCritDamage: 12, weaponCritMult: 1,
                                                                        weaponDamageMod: 31, weaponWeightMod: 22, weaponPriceMod: 3600, weaponSpeedMod: 0.8f, weaponCritDamageMod: 15, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeMace,           new WeaponValues(weaponDamage: 16, weaponWeight: 20, weaponPrice: 1750, weaponSpeed: 0.8f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 22, weaponWeightMod: 19, weaponPriceMod: 3450, weaponSpeedMod: 0.8f, weaponCritDamageMod: 11, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeSword,          new WeaponValues(weaponDamage: 14, weaponWeight: 16, weaponPrice: 1250, weaponSpeed: 1, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 20, weaponWeightMod: 15, weaponPriceMod: 2460, weaponSpeedMod: 1, weaponCritDamageMod: 10, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarAxe,         new WeaponValues(weaponDamage: 15, weaponWeight: 18, weaponPrice: 1500, weaponSpeed: 0.9f, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 21, weaponWeightMod: 17, weaponPriceMod: 2940, weaponSpeedMod: 0.9f, weaponCritDamageMod: 11, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarhammer,      new WeaponValues(weaponDamage: 27, weaponWeight: 31, weaponPrice: 4000, weaponSpeed: 0.6f, weaponCritDamage: 13, weaponCritMult: 1,
                                                                        weaponDamageMod: 34, weaponWeightMod: 30, weaponPriceMod: 5190, weaponSpeedMod: 0.6f, weaponCritDamageMod: 17, weaponCritMultMod: 1) }
                }
            },
            {
                Skyrim.Keyword.WeapMaterialEbony, new Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>()
                {
                    {Skyrim.Keyword.WeapTypeBattleaxe,      new WeaponValues(weaponDamage: 23, weaponWeight: 26, weaponPrice: 1585, weaponSpeed: 0.7f, weaponCritDamage: 11, weaponCritMult: 1,
                                                                        weaponDamageMod: 30, weaponWeightMod: 27, weaponPriceMod: 2880, weaponSpeedMod: 0.7f, weaponCritDamageMod: 15, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeBow,            new WeaponValues(weaponDamage: 17, weaponWeight: 16, weaponPrice: 1440, weaponSpeed: 0.5625f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 23, weaponWeightMod: 16, weaponPriceMod: 2100, weaponSpeedMod: 0.5625f, weaponCritDamageMod: 12, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeDagger,         new WeaponValues(weaponDamage: 10, weaponWeight: 5, weaponPrice: 290, weaponSpeed: 1.3f, weaponCritDamage: 5, weaponCritMult: 1,
                                                                        weaponDamageMod: 15, weaponWeightMod: 5, weaponPriceMod: 1100, weaponSpeedMod: 1.3f, weaponCritDamageMod: 7, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeGreatsword,     new WeaponValues(weaponDamage: 22, weaponWeight: 22, weaponPrice: 1440, weaponSpeed: 0.75f, weaponCritDamage: 11, weaponCritMult: 1,
                                                                        weaponDamageMod: 29, weaponWeightMod: 22, weaponPriceMod: 2400, weaponSpeedMod: 0.8f, weaponCritDamageMod: 14, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeMace,           new WeaponValues(weaponDamage: 15, weaponWeight: 19, weaponPrice: 1000, weaponSpeed: 0.8f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 20, weaponWeightMod: 19, weaponPriceMod: 2300, weaponSpeedMod: 0.8f, weaponCritDamageMod: 10, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeSword,          new WeaponValues(weaponDamage: 13, weaponWeight: 15, weaponPrice: 720, weaponSpeed: 1, weaponCritDamage: 6, weaponCritMult: 1,
                                                                        weaponDamageMod: 18, weaponWeightMod: 15, weaponPriceMod: 1640, weaponSpeedMod: 1, weaponCritDamageMod: 9, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarAxe,         new WeaponValues(weaponDamage: 14, weaponWeight: 17, weaponPrice: 865, weaponSpeed: 0.9f, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 19, weaponWeightMod: 17, weaponPriceMod: 1960, weaponSpeedMod: 0.9f, weaponCritDamageMod: 10, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarhammer,      new WeaponValues(weaponDamage: 25, weaponWeight: 30, weaponPrice: 1725, weaponSpeed: 0.6f, weaponCritDamage: 12, weaponCritMult: 1,
                                                                        weaponDamageMod: 32, weaponWeightMod: 32, weaponPriceMod: 3460, weaponSpeedMod: 0.6f, weaponCritDamageMod: 16, weaponCritMultMod: 1) }
                }
            },
            {
                Skyrim.Keyword.WeapMaterialGlass, new Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>()
                {
                    {Skyrim.Keyword.WeapTypeBattleaxe,      new WeaponValues(weaponDamage: 22, weaponWeight: 25, weaponPrice: 900, weaponSpeed: 0.7f, weaponCritDamage: 11, weaponCritMult: 1,
                                                                        weaponDamageMod: 27, weaponWeightMod: 18, weaponPriceMod: 1440, weaponSpeedMod: 0.77f, weaponCritDamageMod: 14, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeBow,            new WeaponValues(weaponDamage: 15, weaponWeight: 14, weaponPrice: 820, weaponSpeed: 0.625f, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 19, weaponWeightMod: 8.5f, weaponPriceMod: 1050, weaponSpeedMod: 0.87505f, weaponCritDamageMod: 10, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeDagger,         new WeaponValues(weaponDamage: 9, weaponWeight: 4.5f, weaponPrice: 165, weaponSpeed: 1.3f, weaponCritDamage: 4, weaponCritMult: 1,
                                                                        weaponDamageMod: 13, weaponWeightMod: 1.7f, weaponPriceMod: 550, weaponSpeedMod: 1.43f, weaponCritDamageMod: 7, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeGreatsword,     new WeaponValues(weaponDamage: 21, weaponWeight: 22, weaponPrice: 820, weaponSpeed: 0.75f, weaponCritDamage: 10, weaponCritMult: 1,
                                                                        weaponDamageMod: 26, weaponWeightMod: 14, weaponPriceMod: 1200, weaponSpeedMod: 0.88f, weaponCritDamageMod: 13, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeMace,           new WeaponValues(weaponDamage: 14, weaponWeight: 18, weaponPrice: 575, weaponSpeed: 0.8f, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 18, weaponWeightMod: 12, weaponPriceMod: 1150, weaponSpeedMod: 0.88f, weaponCritDamageMod: 9, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeSword,          new WeaponValues(weaponDamage: 12, weaponWeight: 14, weaponPrice: 410, weaponSpeed: 1, weaponCritDamage: 6, weaponCritMult: 1,
                                                                        weaponDamageMod: 16, weaponWeightMod: 7.5f, weaponPriceMod: 820, weaponSpeedMod: 1.1f, weaponCritDamageMod: 8, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarAxe,         new WeaponValues(weaponDamage: 13, weaponWeight: 16, weaponPrice: 490, weaponSpeed: 0.9f, weaponCritDamage: 6, weaponCritMult: 1,
                                                                        weaponDamageMod: 17, weaponWeightMod: 9.5f, weaponPriceMod: 980, weaponSpeedMod: 0.99f, weaponCritDamageMod: 9, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarhammer,      new WeaponValues(weaponDamage: 24, weaponWeight: 29, weaponPrice: 985, weaponSpeed: 0.6f, weaponCritDamage: 12, weaponCritMult: 1,
                                                                        weaponDamageMod: 29, weaponWeightMod: 22, weaponPriceMod: 1730, weaponSpeedMod: 0.77f, weaponCritDamageMod: 14, weaponCritMultMod: 1) }
                }
            },
            {
                Dawnguard.Keyword.DLC1WeapMaterialDragonbone, new Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>()
                {
                    {Skyrim.Keyword.WeapTypeBattleaxe,      new WeaponValues(weaponDamage: 26, weaponWeight: 30, weaponPrice: 3000, weaponSpeed: 0.7f, weaponCritDamage: 13, weaponCritMult: 1,
                                                                        weaponDamageMod: 32, weaponWeightMod: 30, weaponPriceMod: 3600, weaponSpeedMod: 0.63f, weaponCritDamageMod: 16, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeBow,            new WeaponValues(weaponDamage: 20, weaponWeight: 20, weaponPrice: 2725, weaponSpeed: 0.75f, weaponCritDamage: 10, weaponCritMult: 1,
                                                                        weaponDamageMod: 25, weaponWeightMod: 20, weaponPriceMod: 2625, weaponSpeedMod: 0.48f, weaponCritDamageMod: 13, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeDagger,         new WeaponValues(weaponDamage: 12, weaponWeight: 6.5f, weaponPrice: 600, weaponSpeed: 1.3f, weaponCritDamage: 6, weaponCritMult: 1,
                                                                        weaponDamageMod: 16, weaponWeightMod: 6.5f, weaponPriceMod: 1375, weaponSpeedMod: 1.17f, weaponCritDamageMod: 8, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeGreatsword,     new WeaponValues(weaponDamage: 25, weaponWeight: 27, weaponPrice: 2725, weaponSpeed: 0.75f, weaponCritDamage: 12, weaponCritMult: 1,
                                                                        weaponDamageMod: 31, weaponWeightMod: 27, weaponPriceMod: 3000, weaponSpeedMod: 0.72f, weaponCritDamageMod: 15, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeMace,           new WeaponValues(weaponDamage: 17, weaponWeight: 22, weaponPrice: 2000, weaponSpeed: 0.8f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 22, weaponWeightMod: 22, weaponPriceMod: 2875, weaponSpeedMod: 0.72f, weaponCritDamageMod: 11, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeSword,          new WeaponValues(weaponDamage: 15, weaponWeight: 19, weaponPrice: 1500, weaponSpeed: 1, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 20, weaponWeightMod: 19, weaponPriceMod: 2050, weaponSpeedMod: 0.9f, weaponCritDamageMod: 10, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarAxe,         new WeaponValues(weaponDamage: 16, weaponWeight: 21, weaponPrice: 1700, weaponSpeed: 0.9f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 21, weaponWeightMod: 21, weaponPriceMod: 2450, weaponSpeedMod: 0.81f, weaponCritDamageMod: 11, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarhammer,      new WeaponValues(weaponDamage: 28, weaponWeight: 33, weaponPrice: 4275, weaponSpeed: 0.6f, weaponCritDamage: 14, weaponCritMult: 1,
                                                                        weaponDamageMod: 34, weaponWeightMod: 33, weaponPriceMod: 4325, weaponSpeedMod: 0.54f, weaponCritDamageMod: 17, weaponCritMultMod: 1) }
                }
            },
            {
                Dragonborn.Keyword.DLC2WeaponMaterialStalhrim, new Dictionary<IFormLinkGetter<IKeywordGetter>, WeaponValues>()
                {
                    {Skyrim.Keyword.WeapTypeBattleaxe,      new WeaponValues(weaponDamage: 24, weaponWeight: 25, weaponPrice: 2150, weaponSpeed: 0.7f, weaponCritDamage: 12, weaponCritMult: 1,
                                                                        weaponDamageMod: 29, weaponWeightMod: 26, weaponPriceMod: 2520, weaponSpeedMod: 0.7f, weaponCritDamageMod: 15, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeBow,            new WeaponValues(weaponDamage: 17, weaponWeight: 15, weaponPrice: 1800, weaponSpeed: 0.5625f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 22, weaponWeightMod: 15, weaponPriceMod: 1850, weaponSpeedMod: 0.5625f, weaponCritDamageMod: 11, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeDagger,         new WeaponValues(weaponDamage: 10, weaponWeight: 4.5f, weaponPrice: 395, weaponSpeed: 1.3f, weaponCritDamage: 5, weaponCritMult: 1,
                                                                        weaponDamageMod: 14, weaponWeightMod: 4.5f, weaponPriceMod: 965, weaponSpeedMod: 1.3f, weaponCritDamageMod: 7, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeGreatsword,     new WeaponValues(weaponDamage: 23, weaponWeight: 21, weaponPrice: 1970, weaponSpeed: 0.75f, weaponCritDamage: 11, weaponCritMult: 1,
                                                                        weaponDamageMod: 28, weaponWeightMod: 21, weaponPriceMod: 2100, weaponSpeedMod: 0.8f, weaponCritDamageMod: 14, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeMace,           new WeaponValues(weaponDamage: 16, weaponWeight: 18, weaponPrice: 1375, weaponSpeed: 0.8f, weaponCritDamage: 8, weaponCritMult: 1,
                                                                        weaponDamageMod: 19, weaponWeightMod: 18, weaponPriceMod: 2015, weaponSpeedMod: 0.8f, weaponCritDamageMod: 9, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeSword,          new WeaponValues(weaponDamage: 13, weaponWeight: 14, weaponPrice: 985, weaponSpeed: 1, weaponCritDamage: 6, weaponCritMult: 1,
                                                                        weaponDamageMod: 17, weaponWeightMod: 14, weaponPriceMod: 1435, weaponSpeedMod: 1, weaponCritDamageMod: 8, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarAxe,         new WeaponValues(weaponDamage: 15, weaponWeight: 16, weaponPrice: 1180, weaponSpeed: 0.9f, weaponCritDamage: 7, weaponCritMult: 1,
                                                                        weaponDamageMod: 18, weaponWeightMod: 16, weaponPriceMod: 1715, weaponSpeedMod: 0.9f, weaponCritDamageMod: 9, weaponCritMultMod: 1) },
                    {Skyrim.Keyword.WeapTypeWarhammer,      new WeaponValues(weaponDamage: 26, weaponWeight: 29, weaponPrice: 2850, weaponSpeed: 0.6f, weaponCritDamage: 13, weaponCritMult: 1,
                                                                        weaponDamageMod: 31, weaponWeightMod: 30, weaponPriceMod: 3030, weaponSpeedMod: 0.6f, weaponCritDamageMod: 15, weaponCritMultMod: 1) }
                }
            }
        };

        // Read-only dictionary to match armor materials, types and pieces to thheir corresponding stats.
        private static readonly IReadOnlyDictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>> armorKeys = new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>>()
        {
            {
                Skyrim.Keyword.ArmorMaterialDaedric, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    {
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Original Daedric
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 18, armorWeight: 10, armorPrice: 625, armorValueMod: 23, armorWeightMod: 9, armorPriceMod: 1250) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 49, armorWeight: 50, armorPrice: 3200, armorValueMod: 57, armorWeightMod: 41, armorPriceMod: 4500) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 18, armorWeight: 6, armorPrice: 625, armorValueMod: 23, armorWeightMod: 7, armorPriceMod: 1250) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 23, armorWeight: 15, armorPrice: 1600, armorValueMod: 28, armorWeightMod: 10, armorPriceMod: 1800) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 36, armorWeight: 15, armorPrice: 1600, armorValueMod: 39, armorWeightMod: 13, armorPriceMod: 1875) }
                        }
                    },
                    {
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 13, armorWeight: 3, armorPrice: 450, armorValueMod: 16, armorWeightMod: 5, armorPriceMod: 1850) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 44, armorWeight: 8, armorPrice: 1800, armorValueMod: 50, armorWeightMod: 12, armorPriceMod: 3100) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 13, armorWeight: 2, armorPrice: 450, armorValueMod: 16, armorWeightMod: 4, armorPriceMod: 1550) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 18, armorWeight: 4, armorPrice: 950, armorValueMod: 21, armorWeightMod: 4.5f, armorPriceMod: 1350) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 31, armorWeight: 6, armorPrice: 950, armorValueMod: 32, armorWeightMod: 6.5f, armorPriceMod: 1950) }
                        }
                    }
                }
            },
            {
                Skyrim.Keyword.ArmorMaterialDragonplate, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    {
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Original Dragonplate
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 17, armorWeight: 8, armorPrice: 425, armorValueMod: 22, armorWeightMod: 11, armorPriceMod: 990) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 46, armorWeight: 40, armorPrice: 2125, armorValueMod: 55, armorWeightMod: 45, armorPriceMod: 3600) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 17, armorWeight: 8, armorPrice: 425, armorValueMod: 22, armorWeightMod: 8, armorPriceMod: 990) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 22, armorWeight: 8, armorPrice: 1050, armorValueMod: 27, armorWeightMod: 12, armorPriceMod: 1450) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 34, armorWeight: 15, armorPrice: 1050, armorValueMod: 38, armorWeightMod: 15, armorPriceMod: 1500) }
                        }
                    },
                    {
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Dragonscale
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 12, armorWeight: 3, armorPrice: 300, armorValueMod: 15, armorWeightMod: 6, armorPriceMod: 1350) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 41, armorWeight: 10, armorPrice: 1500, armorValueMod: 48, armorWeightMod: 15, armorPriceMod: 2700) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 12, armorWeight: 3, armorPrice: 300, armorValueMod: 15, armorWeightMod: 5, armorPriceMod: 1050) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 17, armorWeight: 4, armorPrice: 750, armorValueMod: 20, armorWeightMod: 5.5f, armorPriceMod: 1050) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 29, armorWeight: 6, armorPrice: 750, armorValueMod: 31, armorWeightMod: 7.5f, armorPriceMod: 1650) }
                        }
                    }
                }
            },
            {
                Skyrim.Keyword.ArmorMaterialDragonscale, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    {
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Dragonplate
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 17, armorWeight: 8, armorPrice: 425, armorValueMod: 22, armorWeightMod: 11, armorPriceMod: 990) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 46, armorWeight: 40, armorPrice: 2125, armorValueMod: 55, armorWeightMod: 45, armorPriceMod: 3600) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 17, armorWeight: 8, armorPrice: 425, armorValueMod: 22, armorWeightMod: 8, armorPriceMod: 990) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 22, armorWeight: 8, armorPrice: 1050, armorValueMod: 27, armorWeightMod: 12, armorPriceMod: 1450) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 34, armorWeight: 15, armorPrice: 1050, armorValueMod: 38, armorWeightMod: 15, armorPriceMod: 1500) }
                        }
                    },
                    {
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Original Dragonscale
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 12, armorWeight: 3, armorPrice: 300, armorValueMod: 15, armorWeightMod: 6, armorPriceMod: 1350) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 41, armorWeight: 10, armorPrice: 1500, armorValueMod: 48, armorWeightMod: 15, armorPriceMod: 2700) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 12, armorWeight: 3, armorPrice: 300, armorValueMod: 15, armorWeightMod: 5, armorPriceMod: 1050) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 17, armorWeight: 4, armorPrice: 750, armorValueMod: 20, armorWeightMod: 5.5f, armorPriceMod: 1050) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 29, armorWeight: 6, armorPrice: 750, armorValueMod: 31, armorWeightMod: 7.5f, armorPriceMod: 1650) }
                        }
                    }
                }
            },
            {
                Skyrim.Keyword.ArmorMaterialEbony, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    {
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Original Ebony
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 16, armorWeight: 7, armorPrice: 275, armorValueMod: 20, armorWeightMod: 9, armorPriceMod: 825) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 43, armorWeight: 38, armorPrice: 1500, armorValueMod: 51, armorWeightMod: 41, armorPriceMod: 3000) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 16, armorWeight: 7, armorPrice: 275, armorValueMod: 20, armorWeightMod: 7, armorPriceMod: 825) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 21, armorWeight: 10, armorPrice: 750, armorValueMod: 25, armorWeightMod: 10, armorPriceMod: 1200) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 32, armorWeight: 14, armorPrice: 750, armorValueMod: 36, armorWeightMod: 13, armorPriceMod: 1250) }
                        }
                    },
                    {
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Stronger, Heavier Glass
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 12, armorWeight: 3, armorPrice: 190, armorValueMod: 14, armorWeightMod: 5.5f, armorPriceMod: 1000) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 40, armorWeight: 9, armorPrice: 900, armorValueMod: 45, armorWeightMod: 12, armorPriceMod: 2000) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 12, armorWeight: 3, armorPrice: 190, armorValueMod: 14, armorWeightMod: 4.5f, armorPriceMod: 850) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 17, armorWeight: 4, armorPrice: 450, armorValueMod: 19, armorWeightMod: 4.5f, armorPriceMod: 850) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 28, armorWeight: 6, armorPrice: 450, armorValueMod: 28, armorWeightMod: 7, armorPriceMod: 1350) }
                        }
                    }
                }
            },
            {
                Skyrim.Keyword.ArmorMaterialGlass, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    {
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Weaker, Lighter Ebony
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 15, armorWeight: 6, armorPrice: 275, armorValueMod: 19, armorWeightMod: 8, armorPriceMod: 800) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 41, armorWeight: 28, armorPrice: 1500, armorValueMod: 49, armorWeightMod: 31, armorPriceMod: 2750) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 15, armorWeight: 5, armorPrice: 275, armorValueMod: 24, armorWeightMod: 5, armorPriceMod: 800) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 20, armorWeight: 9, armorPrice: 750, armorValueMod: 29, armorWeightMod: 9, armorPriceMod: 1050) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 31, armorWeight: 12, armorPrice: 750, armorValueMod: 40, armorWeightMod: 11, armorPriceMod: 1050) }
                        }
                    },
                    {
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        { // Original Glass
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 11, armorWeight: 2, armorPrice: 190, armorValueMod: 14, armorWeightMod: 4.5f, armorPriceMod: 900) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 38, armorWeight: 7, armorPrice: 900, armorValueMod: 45, armorWeightMod: 9, armorPriceMod: 1800) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 11, armorWeight: 2, armorPrice: 190, armorValueMod: 14, armorWeightMod: 3.5f, armorPriceMod: 700) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 16, armorWeight: 2, armorPrice: 450, armorValueMod: 19, armorWeightMod: 3.5f, armorPriceMod: 700) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 27, armorWeight: 6, armorPrice: 450, armorValueMod: 28, armorWeightMod: 5.5f, armorPriceMod: 1100) }
                        }
                    }
                }
            },
            {
                Skyrim.Keyword.ArmorNightingale, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    {
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 14, armorWeight: 8, armorPrice: 305, armorValueMod: 17.5f, armorWeightMod: 8, armorPriceMod: 835) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 38, armorWeight: 36, armorPrice: 1525, armorValueMod: 44, armorWeightMod: 28, armorPriceMod: 2040) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 14, armorWeight: 7, armorPrice: 305, armorValueMod: 17.5f, armorWeightMod: 7, armorPriceMod: 795) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 19, armorWeight: 9, armorPrice: 750, armorValueMod: 20, armorWeightMod: 9, armorPriceMod: 960) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 29, armorWeight: 13, armorPrice: 750, armorValueMod: 30, armorWeightMod: 11, armorPriceMod: 960) }
                        }
                    },
                    {
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 10, armorWeight: 2, armorPrice: 190, armorValueMod: 13.5f, armorWeightMod: 2, armorPriceMod: 335) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 34, armorWeight: 12, armorPrice: 900, armorValueMod: 41, armorWeightMod: 6, armorPriceMod: 1040) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 10, armorWeight: 2, armorPrice: 190, armorValueMod: 13.5f, armorWeightMod: 1, armorPriceMod: 295) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 15, armorWeight: 2, armorPrice: 450, armorValueMod: 16, armorWeightMod: 2, armorPriceMod: 560) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 25, armorWeight: 4, armorPrice: 450, armorValueMod: 26, armorWeightMod: 4, armorPriceMod: 560) }
                        }
                    }
                }
            },
            {
                Dragonborn.Keyword.DLC2ArmorMaterialStalhrimHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    { // Original Stalhrim Heavy
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 17, armorWeight: 7, armorPrice: 450, armorValueMod: 19.5f, armorWeightMod: 8, armorPriceMod: 860) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 46, armorWeight: 38, armorPrice: 2200, armorValueMod: 50, armorWeightMod: 38, armorPriceMod: 2700) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 17, armorWeight: 7, armorPrice: 450, armorValueMod: 19.5f, armorWeightMod: 6.5f, armorPriceMod: 750) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 22, armorWeight: 7, armorPrice: 1135, armorValueMod: 24, armorWeightMod: 9, armorPriceMod: 1080) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 32, armorWeight: 10, armorPrice: 1135, armorValueMod: 35, armorWeightMod: 12, armorPriceMod: 1080) }
                        }
                    },
                    { // Stalhrim Light
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 11.5f, armorWeight: 2, armorPrice: 215, armorValueMod: 14, armorWeightMod: 5.5f, armorPriceMod: 945) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 39,   armorWeight: 7, armorPrice: 925, armorValueMod: 45, armorWeightMod: 13, armorPriceMod: 1890) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 11.5f, armorWeight: 2, armorPrice: 215, armorValueMod: 14, armorWeightMod: 4.5f, armorPriceMod: 735) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 16.5f, armorWeight: 2, armorPrice: 465, armorValueMod: 19, armorWeightMod: 5, armorPriceMod: 735) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 29.5f, armorWeight: 10, armorPrice: 600, armorValueMod: 28, armorWeightMod: 5.5f, armorPriceMod: 1100) }
                        }
                    }
                }
            },
            {
                Dragonborn.Keyword.DLC2ArmorMaterialStalhrimLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>>()
                {
                    { // Stalhrim Heavy
                        Skyrim.Keyword.ArmorHeavy, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 17, armorWeight: 7, armorPrice: 450, armorValueMod: 19.5f, armorWeightMod: 8, armorPriceMod: 860) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 46, armorWeight: 38, armorPrice: 2200, armorValueMod: 50, armorWeightMod: 38, armorPriceMod: 2700) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 17, armorWeight: 7, armorPrice: 450, armorValueMod: 19.5f, armorWeightMod: 6.5f, armorPriceMod: 750) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 22, armorWeight: 7, armorPrice: 1135, armorValueMod: 24, armorWeightMod: 9, armorPriceMod: 1080) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 32, armorWeight: 10, armorPrice: 1135, armorValueMod: 35, armorWeightMod: 12, armorPriceMod: 1080) }
                        }
                    },
                    { // Original Stalhrim Light
                        Skyrim.Keyword.ArmorLight, new Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>()
                        {
                            {Skyrim.Keyword.ArmorBoots,     new ArmorValues(armorValue: 11.5f, armorWeight: 2, armorPrice: 215, armorValueMod: 14, armorWeightMod: 5.5f, armorPriceMod: 945) },
                            {Skyrim.Keyword.ArmorCuirass,   new ArmorValues(armorValue: 39,   armorWeight: 7, armorPrice: 925, armorValueMod: 45, armorWeightMod: 13, armorPriceMod: 1890) },
                            {Skyrim.Keyword.ArmorGauntlets, new ArmorValues(armorValue: 11.5f, armorWeight: 2, armorPrice: 215, armorValueMod: 14, armorWeightMod: 4.5f, armorPriceMod: 735) },
                            {Skyrim.Keyword.ArmorHelmet,    new ArmorValues(armorValue: 16.5f, armorWeight: 2, armorPrice: 465, armorValueMod: 19, armorWeightMod: 5, armorPriceMod: 735) },
                            {Skyrim.Keyword.ArmorShield,    new ArmorValues(armorValue: 29.5f, armorWeight: 10, armorPrice: 600, armorValueMod: 28, armorWeightMod: 5.5f, armorPriceMod: 1100) }
                        }
                    }
                }
            }
        };

        // Rounds to .5 and then to the next whole number.
        private static double RoundWithHalf(double value)
        {
            return Math.Ceiling(2 * value) / 2d;
        }

        // Patches weapon stats to have a morrowloot-inspired balance while keeping their relative balance intended by the mod authors.
        private static bool PatchWeaponValues(Weapon weaponEntry)
        {
            if (weaponEntry.Keywords is null || weaponEntry.BasicStats is null || weaponEntry.Data is null || weaponEntry.Critical is null) return false;
            if (Patcher.ModSettings.Value.ItemAdjustments.Options.SkipArtifacts && weaponEntry.Keywords.Contains(Skyrim.Keyword.DaedricArtifact)) return false;
            if (Patcher.ModSettings.Value.ItemAdjustments.Options.SkipUniques && weaponEntry.Keywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting)) return false;

            bool wasChanged = false;
            foreach (IFormLinkGetter<IKeywordGetter> weaponKeyword in weaponEntry.Keywords)
            {
                if (wasChanged) break;
                if (!weaponKeys.TryGetValue(weaponKeyword, out var materialDict)) continue;

                foreach (IFormLinkGetter<IKeywordGetter> weaponKeyword2 in weaponEntry.Keywords)
                {
                    if (!materialDict.TryGetValue(weaponKeyword2, out var weaponStats)) continue;
                    
                    weaponEntry.BasicStats.Damage = (ushort)Math.Round(weaponStats.WeaponDamageMod * (weaponEntry.BasicStats.Damage / weaponStats.WeaponDamage));
                    weaponEntry.BasicStats.Weight = (float)RoundWithHalf(weaponStats.WeaponWeightMod * (weaponEntry.BasicStats.Weight / weaponStats.WeaponWeight));
                    weaponEntry.BasicStats.Value = (uint)Math.Round(weaponStats.WeaponPriceMod * (weaponEntry.BasicStats.Value / weaponStats.WeaponPrice));

                    weaponEntry.Data.Speed = weaponStats.WeaponSpeedMod * (weaponEntry.Data.Speed / weaponStats.WeaponSpeed);

                    weaponEntry.Critical.Damage = (ushort)Math.Round(weaponStats.WeaponCritDamageMod * (weaponEntry.Critical.Damage / weaponStats.WeaponCritDamage));
                    weaponEntry.Critical.PercentMult = weaponStats.WeaponCritMultMod * (weaponEntry.Critical.PercentMult / weaponStats.WeaponCritMult);
                    wasChanged = true;
                    break;
                }
            }

            return wasChanged;
        }

        // Patches armor stats to have a morrowloot-inspired balance while keeping their relative balance intended by the mod authors.
        private static bool PatchArmorValues(Armor armorEntry)
        {
            if (armorEntry.Keywords is null) return false;
            if (Patcher.ModSettings.Value.ItemAdjustments.Options.SkipArtifacts && armorEntry.Keywords.Contains(Skyrim.Keyword.DaedricArtifact)) return false;
            if (Patcher.ModSettings.Value.ItemAdjustments.Options.SkipUniques && armorEntry.Keywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting)) return false;

            bool wasChanged = false;
            foreach(IFormLinkGetter<IKeywordGetter> armorKeyword in armorEntry.Keywords)
            {
                if (wasChanged) break;
                if (!armorKeys.TryGetValue(armorKeyword, out var materialDict)) continue;

                Dictionary<IFormLinkGetter<IKeywordGetter>, ArmorValues>? weightDict;
                bool isHeavy = (armorEntry.BodyTemplate?.ArmorType ?? ArmorType.Clothing) == ArmorType.HeavyArmor;
                bool isLight = (armorEntry.BodyTemplate?.ArmorType ?? ArmorType.Clothing) == ArmorType.LightArmor;
                if (isHeavy)
                    materialDict.TryGetValue(Skyrim.Keyword.ArmorHeavy, out weightDict);
                else if (isLight)
                    materialDict.TryGetValue(Skyrim.Keyword.ArmorLight, out weightDict);
                else continue;

                foreach (IFormLinkGetter<IKeywordGetter> armorKeyword2 in armorEntry.Keywords)
                {
                    if (weightDict is null || !weightDict.TryGetValue(armorKeyword2, out var armorStats)) continue;

                    armorEntry.ArmorRating =    (float)RoundWithHalf(armorStats.ArmorValueMod * (armorEntry.ArmorRating / armorStats.ArmorValue));
                    armorEntry.Weight =         (float)RoundWithHalf(armorStats.ArmorWeightMod * (armorEntry.Weight / armorStats.ArmorWeight));
                    armorEntry.Value =          (uint)(armorStats.ArmorPriceMod * (armorEntry.Value / armorStats.ArmorPrice));
                    wasChanged = true;
                    break;
                }

            }

            return wasChanged;
        }

        // Main function to change all item stats to new morrowloot-inspired values.
        public static void PatchItems(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            uint processedRecords = 0;
            foreach(IArmorGetter? armorGetter in state.LoadOrder.PriorityOrder.Armor().WinningOverrides())
            {
                bool wasChanged = false;
                Armor armorCopy = armorGetter.DeepCopy();

                wasChanged |= PatchArmorValues(armorCopy);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " armors.");

                if(wasChanged)
                {
                    state.PatchMod.Armors.Set(armorCopy);
                    // Console.WriteLine("Patched armor: " + armorCopy.EditorID);
                }
            }

            Console.WriteLine("Processed " + processedRecords + " armors in total.");
            processedRecords = 0;

            foreach (IWeaponGetter? weaponGetter in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
            {
                bool wasChanged = false;
                Weapon weaponCopy = weaponGetter.DeepCopy();

                wasChanged |= PatchWeaponValues(weaponCopy);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " weapons.");

                if (wasChanged)
                {
                    state.PatchMod.Weapons.Set(weaponCopy);
                    // Console.WriteLine("Patched weapon: " + weaponCopy.EditorID);
                }
            }

            Console.WriteLine("Processed " + processedRecords + " weapons in total.");
        }
    }
}
