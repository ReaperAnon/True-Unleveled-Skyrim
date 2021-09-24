using System;

using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

using TrueUnleveledSkyrim.Config;


namespace TrueUnleveledSkyrim.Patch
{
    class ZonesPatcher
    {
        private static ZoneList? ZonesByKeyword;
        private static ZoneList? ZonesByID;

        private static void UnlevelZone(EncounterZone encZone, ZoneEntry zoneDefinition)
        {
            encZone.Flags.SetFlag(EncounterZone.Flag.MatchPcBelowMinimumLevel, false);
            if(zoneDefinition.EnableCombatBoundary is not null)
                encZone.Flags.SetFlag(EncounterZone.Flag.DisableCombatBoundary, (bool)zoneDefinition.EnableCombatBoundary);

            if (Patcher.ModSettings.Value.Unleveling.Zones.StaticZoneLevels)
            {
                encZone.MinLevel = (sbyte)Patcher.Randomizer.Next(zoneDefinition.MinLevel, zoneDefinition.MaxLevel);
                encZone.MaxLevel = encZone.MinLevel;
            }
            else
            {
                encZone.MinLevel = (sbyte)Patcher.Randomizer.Next(zoneDefinition.MinLevel, zoneDefinition.MaxLevel - zoneDefinition.Range + 1);
                encZone.MaxLevel = (sbyte)(encZone.MinLevel + zoneDefinition.Range);
            }
        }

        private static bool PatchZonesByKeyword(EncounterZone encZone, ILinkCache linkCache)
        {
            if (!encZone.Location.TryResolve<ILocationGetter>(linkCache, out ILocationGetter? resolvedLocation)) return false;
            foreach (var keywordEntry in resolvedLocation.Keywords.EmptyIfNull())
            {
                if (!keywordEntry.TryResolve<IKeywordGetter>(linkCache, out IKeywordGetter? resolvedKeyword)) continue;
                foreach (ZoneEntry? zoneDefinition in ZonesByKeyword!.Zones)
                {
                    foreach (string? keyEntry in zoneDefinition.Keys)
                    {
                        bool willChange = false;
                        if (resolvedKeyword.EditorID is not null && resolvedKeyword.EditorID.Contains(keyEntry))
                        {
                            willChange = true;
                            foreach(string? forbiddenKey in zoneDefinition.ForbiddenKeys)
                            {
                                if (resolvedKeyword.EditorID.Contains(forbiddenKey))
                                    willChange = false;
                            }

                            if(willChange)
                            {
                                UnlevelZone(encZone, zoneDefinition);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool PatchZonesByID(EncounterZone encZone, ILinkCache linkCache)
        {
            foreach (ZoneEntry? zoneDefinition in ZonesByID!.Zones)
            {
                foreach (string? idEntry in zoneDefinition.Keys)
                {
                    if (encZone.EditorID is not null && encZone.EditorID.Contains(idEntry))
                    {
                        bool willChange = true;
                        foreach (var forbiddenKey in zoneDefinition.ForbiddenKeys)
                        {
                            if (encZone.EditorID.Contains(forbiddenKey))
                                willChange = false;
                        }

                        if (willChange)
                        {
                            UnlevelZone(encZone, zoneDefinition);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void PatchZones(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if(Patcher.ModSettings.Value.Unleveling.Zones.UseMorrowlootZoneBalance)
            {
                ZonesByKeyword = JsonHelper.LoadConfig<ZoneList>(TUSConstants.ZoneTyesKeywordMLUPath);
                ZonesByID = JsonHelper.LoadConfig<ZoneList>(TUSConstants.ZoneTyesEDIDMLUPath);
            }
            else
            {
                ZonesByKeyword = JsonHelper.LoadConfig<ZoneList>(TUSConstants.ZoneTyesKeywordPath);
                ZonesByID = JsonHelper.LoadConfig<ZoneList>(TUSConstants.ZoneTyesEDIDPath);
            }

            uint processedRecords = 0;
            foreach (IEncounterZoneGetter? zoneGetter in state.LoadOrder.PriorityOrder.EncounterZone().WinningOverrides())
            {
                bool wasChanged = false;
                EncounterZone zoneCopy = zoneGetter.DeepCopy();

                wasChanged = PatchZonesByID(zoneCopy, Patcher.LinkCache) || PatchZonesByKeyword(zoneCopy, Patcher.LinkCache);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " encounter zones.");

                if (wasChanged)
                {
                    state.PatchMod.EncounterZones.Set(zoneCopy);
                }
            }

            GameSettingFloat? easyEnemyLvlMult = Skyrim.GameSetting.fLeveledActorMultEasy.TryResolve(Patcher.LinkCache)!.DeepCopy() as GameSettingFloat; easyEnemyLvlMult!.Data = new float?(Patcher.ModSettings.Value.Unleveling.Zones.EasySpawnLevelMult); state.PatchMod.GameSettings.Set(easyEnemyLvlMult!);
            GameSettingFloat? mediumEnemyLvlMult = Skyrim.GameSetting.fLeveledActorMultMedium.TryResolve(Patcher.LinkCache)!.DeepCopy() as GameSettingFloat; mediumEnemyLvlMult!.Data = new float?(Patcher.ModSettings.Value.Unleveling.Zones.NormalSpawnLevelMult); state.PatchMod.GameSettings.Set(mediumEnemyLvlMult!);
            GameSettingFloat? hardEnemyLvlMult = Skyrim.GameSetting.fLeveledActorMultHard.TryResolve(Patcher.LinkCache)!.DeepCopy() as GameSettingFloat; hardEnemyLvlMult!.Data = new float?(Patcher.ModSettings.Value.Unleveling.Zones.HardSpawnLevelMult); state.PatchMod.GameSettings.Set(hardEnemyLvlMult!);
            GameSettingFloat? veryHardEnemyLvlMult = Skyrim.GameSetting.fLeveledActorMultVeryHard.TryResolve(Patcher.LinkCache)!.DeepCopy() as GameSettingFloat; veryHardEnemyLvlMult!.Data = new float?(Patcher.ModSettings.Value.Unleveling.Zones.VeryHardSpawnLevelMult); state.PatchMod.GameSettings.Set(veryHardEnemyLvlMult!);

            Console.WriteLine("Processed " + processedRecords + " encounter zones in total.");
        }
    }
}
