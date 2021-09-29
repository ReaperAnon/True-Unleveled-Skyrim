using System;
using System.Linq;
using System.Collections.Generic;

using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

using TrueUnleveledSkyrim.Config;


namespace TrueUnleveledSkyrim.Patch
{
    class NPCsPatcher
    {
        private static FollowerList? followerList;
        private static ExcludedPerks? excludedPerks;
        private static ExcludedNPCs? excludedNPCs;
        private static NPCEDIDs? customNPCsByID;
        private static NPCFactions? customNPCsByFaction;
        private static RaceModifiers? raceModifiers;

        // Returns the level modifiers for the desired NPC based on their race.
        private static void GetLevelMultiplier(Npc npc, ILinkCache linkCache, out short levelModAdd, out float levelModMult)
        {
            levelModAdd = 0; levelModMult = 1;
            if (!npc.Race.TryResolve(linkCache, out var raceGetter) || raceGetter.EditorID is null) return;
            foreach (RaceEntries? dataSet in raceModifiers!.Data)
            {
                foreach (string? raceKey in dataSet.Keys)
                {
                    if (raceGetter.EditorID.Contains(raceKey, StringComparison.OrdinalIgnoreCase))
                    {
                        bool willChange = true;
                        foreach (var exclusionKey in dataSet.ForbiddenKeys)
                        {
                            if(raceGetter.EditorID.Contains(exclusionKey, StringComparison.OrdinalIgnoreCase))
                                willChange = false;
                        }

                        if (willChange)
                        {
                            levelModAdd = dataSet.LevelModifierAdd ?? 0;
                            levelModMult = dataSet.LevelModifierMult ?? 1;
                        }

                        return;
                    }
                }
            }
        }

        // Gives the npcs defined in the NPCsByEDID.json file (EDID does not have to be complete) the custom level given.
        private static bool GetNPCLevelByEDID(Npc npc, short levelModAdd, float levelModMult)
        {
            foreach (NPCEDIDEntry? dataSet in customNPCsByID!.NPCs)
            {
                foreach (string? npcKey in dataSet.Keys)
                {
                    if (npc.EditorID!.Contains(npcKey, StringComparison.OrdinalIgnoreCase))
                    {
                        bool willChange = true;
                        foreach (string? exclusionKey in dataSet.ForbiddenKeys)
                        {
                            if (npc.EditorID!.Contains(exclusionKey, StringComparison.OrdinalIgnoreCase))
                                willChange = false;
                        }

                        if (willChange)
                        {
                            npc.Configuration.Level = new NpcLevel() { Level = (short)Math.Max(dataSet.Level * levelModMult + levelModAdd, 1) };
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        // Gives the npcs who have the appropriate factions defined in the NPCsByFaction.json file (faction EDID does not have to be complete) the custom level or level range given.
        private static bool GetNPCLevelByFaction(Npc npc, ILinkCache linkCache, short levelModAdd, float levelModMult)
        {
            foreach (RankPlacement? rankEntry in npc.Factions)
            {
                IFactionGetter? faction = rankEntry.Faction.TryResolve(linkCache);

                if (faction is null) continue;
                foreach (NPCFactionEntry? dataSet in customNPCsByFaction!.NPCs)
                {
                    foreach (string? factionKey in dataSet.Keys)
                    {
                        if (faction.EditorID!.Contains(factionKey, StringComparison.OrdinalIgnoreCase))
                        {
                            bool willChange = true;
                            foreach (string? exclusionKey in dataSet.ForbiddenKeys)
                            {
                                if (faction.EditorID.Contains(exclusionKey, StringComparison.OrdinalIgnoreCase))
                                    willChange = false;
                            }

                            if (willChange)
                            {
                                short newLevel = (short)(dataSet.Level ?? Patcher.Randomizer.Next((int)dataSet.MinLevel!, (int)dataSet.MaxLevel!));
                                npc.Configuration.Level = new NpcLevel() { Level = (short)Math.Max(newLevel * levelModMult + levelModAdd, 1) };
                                return true;
                            }

                            return false;
                        }
                    }
                }
            }

            return false;
        }

        // Gives all NPCs that revolve around the player a static level and applies level modifiers.
        private static bool SetStaticLevel(Npc npc, ILinkCache linkCache)
        {
            short levelModAdd; float levelModMult;
            GetLevelMultiplier(npc, linkCache, out levelModAdd, out levelModMult);
            bool wasChanged = GetNPCLevelByEDID(npc, levelModAdd, levelModMult) || GetNPCLevelByFaction(npc, linkCache, levelModAdd, levelModMult);

            if (wasChanged) return true;
            if (npc.Configuration.Level is PcLevelMult pcLevelMult)
            {
                float lvlMult = (pcLevelMult.LevelMult <= 0) ? 1 : pcLevelMult.LevelMult;
                short lvlMin = npc.Configuration.CalcMinLevel; short lvlMax = npc.Configuration.CalcMaxLevel;

                bool isUnique = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique);
                if(isUnique && (lvlMax == 0 || lvlMax >= 100))
                        lvlMax = 100;
                else if(lvlMax == 0 || lvlMax > 80)
                    lvlMax = 80;

                wasChanged = true;
                npc.Configuration.Level = new NpcLevel()
                {
                    Level = (short)(Math.Round((lvlMin + lvlMax) * lvlMult * levelModMult / 2) + levelModAdd)
                };
            }
            else if(npc.Configuration.Level is NpcLevel npcLevel)
            {
                short prevLevel = npcLevel.Level;
                npcLevel.Level = (short)Math.Max(npcLevel.Level * levelModMult + levelModAdd, 1); 
                wasChanged = npcLevel.Level != prevLevel;
            }

            return wasChanged;
        }

        // Changes the inventory of NPCs to have weaker or stronger versions of their equipment lists based on their level.
        private static bool ChangeEquipment(Npc npc, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILinkCache linkCache)
        {
            bool wasChanged = false;

            if(npc.Configuration.Level is NpcLevel npcLevel)
            {
                string usedPostfix;
                if(Patcher.ModSettings.Value.Unleveling.Items.AllowMidTier)
                    usedPostfix = npcLevel.Level < 13 ? TUSConstants.WeakPostfix : npcLevel.Level > 27 ? TUSConstants.StrongPostfix : "";
                else
                    usedPostfix = npcLevel.Level <= 27 ? TUSConstants.WeakPostfix : npcLevel.Level > 27 ? TUSConstants.StrongPostfix : "";
                if (!usedPostfix.IsNullOrEmpty())
                {
                    foreach (ContainerEntry? entry in npc.Items.EmptyIfNull())
                    {
                        ILeveledItemGetter? resolvedItem = entry.Item.Item.TryResolve<ILeveledItemGetter>(linkCache);
                        if (resolvedItem is not null)
                        {
                            LeveledItem? newItem = state.PatchMod.LeveledItems.Where(x => x.EditorID == resolvedItem.EditorID + usedPostfix).FirstOrDefault();
                            if (newItem is not null)
                            {
                                entry.Item.Item = newItem.AsLink();
                                wasChanged = true;
                            }
                        }
                    }

                    IOutfitGetter? npcOutfit = npc.DefaultOutfit.TryResolve(linkCache);
                    if(npcOutfit is not null)
                    {
                        Outfit? newOutfit = state.PatchMod.Outfits.Where(x => x.EditorID == npcOutfit.EditorID + usedPostfix).FirstOrDefault();
                        if (newOutfit is not null)
                        {
                            npc.DefaultOutfit = newOutfit.AsNullableLink();
                            wasChanged = true;
                        }
                    }
                }
            }

            return wasChanged;
        }

        private static void DistributeSkills(IReadOnlyDictionary<Skill, byte> skillWeights, IDictionary<Skill, byte> skillValues, int skillPoints)
        {
            float weightSum = 0;
            bool firstPass = true;
            byte maxSkill = Patcher.ModSettings.Value.Unleveling.NPCs.NPCMaxSkillLevel;
            List<KeyValuePair<Skill, byte>> tempWeights = skillWeights.ToList();
            
            do
            {
                int pointOverflow = 0;
                weightSum = tempWeights.Any() ? tempWeights.Sum(x => x.Value) : 0;
                for (int i=tempWeights.Count - 1; i>=0; --i)
                {
                    if (firstPass)
                        skillValues[tempWeights[i].Key] = 15;

                    skillValues[tempWeights[i].Key] += (byte)(skillPoints * (tempWeights[i].Value / weightSum));
                    if (skillValues[tempWeights[i].Key] > maxSkill)
                    {
                        pointOverflow += skillValues[tempWeights[i].Key] - maxSkill;
                        skillValues[tempWeights[i].Key] = maxSkill;
                        tempWeights.RemoveAt(i);
                    }
                }

                firstPass = false;
                skillPoints = pointOverflow;
            } while (skillPoints > 0 && weightSum > 0);
        }

        private static bool RelevelNPCSkills(Npc npc, ILinkCache linkCache)
        {
            float skillsPerLevel = Patcher.ModSettings.Value.Unleveling.NPCs.NPCSkillsPerLevel;
            if (skillsPerLevel > 0 && npc.PlayerSkills is not null && npc.Configuration.Level is NpcLevel npcLevel)
            {
                IClassGetter? npcClass = npc.Class.TryResolve(linkCache);
                if (npcClass is not null)
                    DistributeSkills(npcClass.SkillWeights, npc.PlayerSkills.SkillValues, (int)Math.Round(skillsPerLevel * npcLevel.Level));

                return true;
            }

            return false;
        }

        private static bool GetTreeFromSkill(Skill activeSkill, ILinkCache linkCache, out IActorValueInformationGetter? actorValue)
        {
            switch(activeSkill)
            {
                case Skill.Alchemy: actorValue = Skyrim.ActorValueInformation.AVAlchemy.TryResolve(linkCache); return true;
                case Skill.Alteration: actorValue = Skyrim.ActorValueInformation.AVAlteration.TryResolve(linkCache); return true;
                case Skill.Archery: actorValue = Skyrim.ActorValueInformation.AVMarksman.TryResolve(linkCache); return true;
                case Skill.Block: actorValue = Skyrim.ActorValueInformation.AVBlock.TryResolve(linkCache); return true;
                case Skill.Conjuration: actorValue = Skyrim.ActorValueInformation.AVConjuration.TryResolve(linkCache); return true;
                case Skill.Destruction: actorValue = Skyrim.ActorValueInformation.AVDestruction.TryResolve(linkCache); return true;
                case Skill.Enchanting: actorValue = Skyrim.ActorValueInformation.AVEnchanting.TryResolve(linkCache); return true;
                case Skill.HeavyArmor: actorValue = Skyrim.ActorValueInformation.AVHeavyArmor.TryResolve(linkCache); return true;
                case Skill.Illusion: actorValue = Skyrim.ActorValueInformation.AVMysticism.TryResolve(linkCache); return true;
                case Skill.LightArmor: actorValue = Skyrim.ActorValueInformation.AVLightArmor.TryResolve(linkCache); return true;
                case Skill.Lockpicking: actorValue = Skyrim.ActorValueInformation.AVLockpicking.TryResolve(linkCache); return true;
                case Skill.OneHanded: actorValue = Skyrim.ActorValueInformation.AVOneHanded.TryResolve(linkCache); return true;
                case Skill.Pickpocket: actorValue = Skyrim.ActorValueInformation.AVPickpocket.TryResolve(linkCache); return true;
                case Skill.Restoration: actorValue = Skyrim.ActorValueInformation.AVRestoration.TryResolve(linkCache); return true;
                case Skill.Smithing: actorValue = Skyrim.ActorValueInformation.AVSmithing.TryResolve(linkCache); return true;
                case Skill.Sneak: actorValue = Skyrim.ActorValueInformation.AVSneak.TryResolve(linkCache); return true;
                case Skill.Speech: actorValue = Skyrim.ActorValueInformation.AVSpeechcraft.TryResolve(linkCache); return true;
                case Skill.TwoHanded: actorValue = Skyrim.ActorValueInformation.AVTwoHanded.TryResolve(linkCache); return true;
                default: actorValue = null;  return false;
            }
        }

        private static bool PerformCompare<T>(IConditionGetter? perkCondition, T lValue, T rValue ) where T : IComparable<T>
        {
            if (perkCondition is null) return false;
            switch(perkCondition.CompareOperator)
            {
                case CompareOperator.EqualTo:               return lValue.CompareTo(rValue) ==  0;
                case CompareOperator.GreaterThan:           return lValue.CompareTo(rValue) >   0;
                case CompareOperator.GreaterThanOrEqualTo:  return lValue.CompareTo(rValue) >=  0;
                case CompareOperator.LessThan:              return lValue.CompareTo(rValue) <   0;
                case CompareOperator.LessThanOrEqualTo:     return lValue.CompareTo(rValue) <=  0;
                case CompareOperator.NotEqualTo:            return lValue.CompareTo(rValue) !=  0;
                    default: return false;
            }
        }

        private static void RemoveOldPerks(Npc npc)
        {
            if (npc.Perks!.Count == 0) return;

            for(int i=npc.Perks.Count - 1; i>=0; --i)
            {
                ModKey perkModKey = npc.Perks[i].Perk.FormKey.ModKey;
                if (perkModKey == Skyrim.ModKey || perkModKey == Dawnguard.ModKey || perkModKey == Dragonborn.ModKey)
                    npc.Perks.RemoveAt(i);
            }
        }

        private static bool FulfillsPerkConditions(Npc npc, IPerkGetter perkEntry, Skill currSkill, ILinkCache linkCache)
        {
            // Check if NPC already has the perk or not.
            if (npc.Perks!.Where(x => x.Perk.Equals(perkEntry.AsLink())).Any()) return false;
            
            bool fulfillsConditions = true;
            foreach(IConditionGetter? perkCondition in perkEntry.Conditions)
            {
                if (perkCondition is null) continue;
                if (perkCondition.DeepCopy() is ConditionFloat perkCondFloat && perkCondFloat.Data is FunctionConditionData funcCond)
                {
                    if (funcCond.Function == Condition.Function.GetBaseActorValue && funcCond.ParameterOneNumber == (int)currSkill)
                        fulfillsConditions = fulfillsConditions && PerformCompare(perkCondFloat, npc.PlayerSkills!.SkillValues[currSkill], perkCondFloat.ComparisonValue);
                    else if (funcCond.Function == Condition.Function.HasPerk && funcCond.ParameterOneRecord.TryResolve<IPerkGetter>(linkCache, out var requiredPerk))
                    {
                        if(perkCondFloat.CompareOperator == CompareOperator.EqualTo && perkCondFloat.ComparisonValue == 1 || perkCondFloat.CompareOperator == CompareOperator.NotEqualTo && perkCondFloat.ComparisonValue == 0)
                            fulfillsConditions = fulfillsConditions && npc.Perks!.Where(x => x.Perk.Equals(requiredPerk.AsLink())).Any();
                        else if(perkCondFloat.CompareOperator == CompareOperator.EqualTo && perkCondFloat.ComparisonValue == 0 || perkCondFloat.CompareOperator == CompareOperator.NotEqualTo && perkCondFloat.ComparisonValue == 1)
                            fulfillsConditions = fulfillsConditions && !npc.Perks!.Where(x => x.Perk.Equals(requiredPerk.AsLink())).Any();
                    }
                }
                else return false;
            }
            
            return fulfillsConditions;
        }

        private static bool DistributeNPCPerks(Npc npc, ILinkCache linkCache)
        {
            bool removeOldPerks = Patcher.ModSettings.Value.Unleveling.NPCs.RemoveOldPerks;
            float perksPerLevel = Patcher.ModSettings.Value.Unleveling.NPCs.NPCPerksPerLevel;
            if(perksPerLevel > 0)
            {
                if (npc.PlayerSkills is null) return false;
                if (!npc.Class.TryResolve<IClassGetter>(linkCache, out IClassGetter? npcClass)) return false;
                if (!npc.Race.TryResolve<IRaceGetter>(linkCache, out IRaceGetter? npcRace)) return false;
                if (!npcRace.HasKeyword(Skyrim.Keyword.ActorTypeNPC) && !npcRace.HasKeyword(Skyrim.Keyword.ActorTypeUndead)) return false;

                if (npc.Configuration.Level is NpcLevel npcLevel)
                    perksPerLevel *= npcLevel.Level;

                if (npc.Perks is null) npc.Perks = new();
                if (removeOldPerks)
                    RemoveOldPerks(npc);

                List<KeyValuePair<Skill, byte>> perkDistribution = npcClass.SkillWeights.ToList();
                float weightSum = perkDistribution.Any() ? perkDistribution.Sum(x => x.Value) : 0;
                int perkOverflow = 0;
                foreach(KeyValuePair<Skill, byte> perkWeight in perkDistribution)
                {
                    if (perkWeight.Value <= 0) continue;

                    byte perksToSpend = (byte)Math.Round(perkOverflow + perksPerLevel * (perkWeight.Value / weightSum));
                    if (perksToSpend <= 0) continue;
                    if (!GetTreeFromSkill(perkWeight.Key, linkCache, out var perkTree) || perkTree!.PerkTree is null) continue;

                    while(perksToSpend > 0)
                    {
                        bool wasPerkAdded = false;
                        foreach(IActorValuePerkNodeGetter? perkNode in perkTree.PerkTree)
                        {
                            if (perksToSpend <= 0) break;
                            if (!perkNode.Perk.TryResolve(linkCache, out var perkEntry) || perkEntry.EditorID is null) continue;

                            bool willSkip = false;
                            foreach(string? perkKey in excludedPerks!.Keys)
                            {
                                if(perkEntry.EditorID.Contains(perkKey, StringComparison.OrdinalIgnoreCase))
                                {
                                    foreach (string? forbiddenKey in excludedPerks.ForbiddenKeys)
                                    if (perkEntry.EditorID.Contains(forbiddenKey, StringComparison.OrdinalIgnoreCase))
                                        break;

                                    willSkip = true;
                                    break;
                                }
                            }

                            if (willSkip) continue;
                            if (FulfillsPerkConditions(npc, perkEntry, perkWeight.Key, linkCache))
                            {
                                --perksToSpend;
                                wasPerkAdded = true;
                                npc.Perks!.Add(new PerkPlacement() { Perk = perkEntry.AsLink(), Rank = 1 });
                            }

                            while (perksToSpend > 0 && perkEntry.NextPerk.TryResolve<IPerkGetter>(linkCache, out perkEntry))
                            {
                                if (FulfillsPerkConditions(npc, perkEntry, perkWeight.Key, linkCache))
                                {
                                    --perksToSpend;
                                    wasPerkAdded = true;
                                    npc.Perks!.Add(new PerkPlacement() { Perk = perkEntry.AsLink(), Rank = 1 });
                                }
                                else break;
                            }
                        }

                        if (!wasPerkAdded)
                        {
                            perkOverflow = perksToSpend;
                            break;
                        } else perkOverflow = 0;
                    }
                }

                if (npc.Perks.Count == 0)
                    npc.Perks = null;

                return true;
            }

            return false;
        }

        private static void DisableExtraDamagePerks(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!Patcher.ModSettings.Value.Unleveling.NPCs.DisableExtraDamagePerks) return;

            foreach (IPerkGetter? perkGetter in state.LoadOrder.PriorityOrder.Perk().WinningOverrides().Where(x => x.EditorID is not null && x.EditorID.Contains("crExtraDamage", StringComparison.OrdinalIgnoreCase)))
            {
                Perk perkCopy = perkGetter.DeepCopy();
                perkCopy.Effects.Clear();
                state.PatchMod.Perks.Set(perkCopy);
            }
        }

        private static bool IsFollower(Npc npc)
        {
            if (!Patcher.ModSettings.Value.Unleveling.NPCs.ScalingFollowers || npc.EditorID is null) return false;

            bool isFollower = false;
            foreach (RankPlacement rankPlacement in npc.Factions)
            {
                if (rankPlacement.Faction.Equals(Skyrim.Faction.PotentialFollowerFaction) || rankPlacement.Faction.Equals(Skyrim.Faction.PotentialHireling))
                {
                    isFollower = true;
                    break;
                }
            }

            foreach (FollowerEntry? followerEntry in followerList!.Followers)
            {
                if (npc.EditorID.Contains(followerEntry.Key, StringComparison.OrdinalIgnoreCase))
                    isFollower = true;
                foreach (string? forbiddenKey in followerEntry.ForbiddenKeys)
                if (npc.EditorID.Contains(forbiddenKey))
                {
                    isFollower = false;
                    break;
                }

                break;
            }

            return isFollower;
        }

        private static bool SetFollowerScaling(Npc npc)
        {
            if (!IsFollower(npc)) return false;

            short currLevel = (npc.Configuration.Level as NpcLevel)?.Level ?? 40;
            npc.Configuration.Level = new PcLevelMult { LevelMult = 1 };
            npc.Configuration.CalcMinLevel = Math.Max(npc.Configuration.CalcMinLevel, (short)1);
            npc.Configuration.CalcMaxLevel = Math.Max(npc.Configuration.CalcMaxLevel, currLevel);

            return true;
        }

        // Main function to unlevel all NPCs.
        public static void PatchNPCs(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            followerList = JsonHelper.LoadConfig<FollowerList>(TUSConstants.FollowersPath);
            excludedPerks = JsonHelper.LoadConfig<ExcludedPerks>(TUSConstants.ExcludedPerksPath);
            excludedNPCs = JsonHelper.LoadConfig<ExcludedNPCs>(TUSConstants.ExcludedNPCsPath);
            customNPCsByID = JsonHelper.LoadConfig<NPCEDIDs>(TUSConstants.NPCEDIDPath);
            customNPCsByFaction= JsonHelper.LoadConfig<NPCFactions>(TUSConstants.NPCFactionPath);
            raceModifiers = JsonHelper.LoadConfig<RaceModifiers>(TUSConstants.RaceModifiersPath);
            
            uint processedRecords = 0;
            foreach(INpcGetter? npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                if (npcGetter.EditorID is null) continue;
                if (npcGetter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsCharGenFacePreset) || npcGetter.Keywords.EmptyIfNull().Contains(Skyrim.Keyword.PlayerKeyword)) continue;

                bool willSkip = false;
                foreach (var exclusionKey in excludedNPCs.Keys)
                {
                    if (npcGetter.EditorID.Contains(exclusionKey, StringComparison.OrdinalIgnoreCase))
                    {
                        willSkip = true;
                        foreach (var forbiddenKey in excludedNPCs.ForbiddenKeys)
                        {
                            if (npcGetter.EditorID.Contains(forbiddenKey, StringComparison.OrdinalIgnoreCase))
                            {
                                willSkip = false;
                                break;
                            }
                        }
                        break;
                    }
                }

                if (willSkip) continue;

                bool wasChanged = false;
                Npc npcCopy = npcGetter.DeepCopy();

                wasChanged |= SetStaticLevel(npcCopy, Patcher.LinkCache);
                wasChanged |= ChangeEquipment(npcCopy, state, Patcher.LinkCache);
                wasChanged |= RelevelNPCSkills(npcCopy, Patcher.LinkCache);
                wasChanged |= DistributeNPCPerks(npcCopy, Patcher.LinkCache);
                wasChanged |= SetFollowerScaling(npcCopy);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " npcs.");

                if (wasChanged)
                {
                    state.PatchMod.Npcs.Set(npcCopy);
                }
            }

            DisableExtraDamagePerks(state);

            Console.WriteLine("Processed " + processedRecords + " npcs in total.");
        }
    }
}
