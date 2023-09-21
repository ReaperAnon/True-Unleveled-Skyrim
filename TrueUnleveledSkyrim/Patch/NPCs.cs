using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

using TrueUnleveledSkyrim.Config;
using Mutagen.Bethesda.Plugins.Order;

namespace TrueUnleveledSkyrim.Patch
{
    class NPCsPatcher
    {
        private static readonly List<string> ExcludedClasses = new() { "smith", "alchem", "enchant", "vendor", "apothec" };

        private static FollowerList? followerList;
        private static ExcludedPerks? excludedPerks;
        private static ExcludedNPCs? excludedNPCs;
        private static NPCEDIDs? customNPCsByID;
        private static NPCFactions? customNPCsByFaction;
        private static RaceModifiers? raceModifiers;

        private static bool ShouldModifyPerks(Npc npc, ILinkCache linkCache)
        {
            foreach (var keyword in Patcher.ModSettings.Value.NPCs.PerkDistributionFilter)
            {
                if (npc.HasKeyword(keyword) || (npc.Race.TryResolve(linkCache, out var raceGetter) && raceGetter.HasKeyword(Skyrim.Keyword.ActorTypeUndead)))
                    return false;
            }

            return true;
        }

        // Returns the level modifiers for the desired NPC based on their race.
        private static void GetLevelMultiplier(Npc npc, ILinkCache linkCache, out short levelModAdd, out float levelModMult)
        {
            levelModAdd = 0;
            levelModMult = 1;
            if (!npc.Race.TryResolve(linkCache, out var raceGetter) || raceGetter.EditorID is null)
                return;

            foreach (RaceEntries? raceEntry in raceModifiers!.Data)
            {
                if (raceEntry.Keys.Any(key => raceGetter.EditorID.Contains(key)) && !raceEntry.ForbiddenKeys.Any(key => raceGetter.EditorID.Contains(key)))
                {
                    levelModAdd = raceEntry.LevelModifierAdd ?? 0;
                    levelModMult = raceEntry.LevelModifierMult ?? 1;
                    return;
                }
            }
        }

        // Gives the npcs defined in the NPCsByEDID.json file (EDID does not have to be complete) the custom level given.
        private static bool GetNPCLevelByEDID(Npc npc, short levelModAdd, float levelModMult)
        {
            foreach (NPCEDIDEntry? edidEntry in customNPCsByID!.NPCs)
            {
                if (edidEntry.Keys.Any(key => npc.EditorID!.Contains(key, StringComparison.OrdinalIgnoreCase)) && !edidEntry.ForbiddenKeys.Any(key => npc.EditorID!.Contains(key, StringComparison.OrdinalIgnoreCase)))
                {
                    npc.Configuration.Level = new NpcLevel() { Level = (short)Math.Max(edidEntry.Level * levelModMult + levelModAdd, 1) };
                    return true;
                }
            }

            return false;
        }

        // Gives the npcs who have the appropriate factions defined in the NPCsByFaction.json file (faction EDID does not have to be complete) the custom level or level range given.
        private static bool GetNPCLevelByFaction(Npc npc, ILinkCache linkCache, short levelModAdd, float levelModMult)
        {
            foreach (RankPlacement? rankEntry in npc.Factions)
            {
                if (!rankEntry.Faction.TryResolve(linkCache, out var factionGetter) || factionGetter.EditorID is null)
                    continue;

                foreach (NPCFactionEntry? factionEntry in customNPCsByFaction!.NPCs)
                {
                    if (factionEntry.Keys.Any(key => factionGetter.EditorID.Contains(key, StringComparison.OrdinalIgnoreCase)) && !factionEntry.ForbiddenKeys.Any(key => factionGetter.EditorID.Contains(key, StringComparison.OrdinalIgnoreCase)))
                    {
                        short newLevel = (short)(factionEntry.Level ?? Patcher.Randomizer.Next((int)factionEntry.MinLevel!, (int)factionEntry.MaxLevel!));
                        npc.Configuration.Level = new NpcLevel() { Level = (short)Math.Max(newLevel * levelModMult + levelModAdd, 1) };
                        return true;
                    }
                }
            }

            return false;
        }

        // Gives all NPCs that revolve around the player a static level and applies level modifiers.
        private static bool SetStaticLevel(Npc npc, ILinkCache linkCache)
        {
            GetLevelMultiplier(npc, linkCache, out short levelModAdd, out float levelModMult);

            bool wasChanged = GetNPCLevelByEDID(npc, levelModAdd, levelModMult) || GetNPCLevelByFaction(npc, linkCache, levelModAdd, levelModMult);
            if (wasChanged)
                return true;

            if (npc.Configuration.Level is PcLevelMult pcLevelMult)
            {
                wasChanged = true;
                bool isUnique = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique);
                float lvlMult = (pcLevelMult.LevelMult <= 0) ? 1 : pcLevelMult.LevelMult;

                int lvlMin = Math.Max((int)npc.Configuration.CalcMinLevel, 1);
                int lvlMax = npc.Configuration.CalcMaxLevel <= 0 ? (isUnique ? 100 : 80) : npc.Configuration.CalcMaxLevel;

                short finalLevel = (short)((Math.Round((lvlMin + lvlMax) / 2.0f) + lvlMin * lvlMult) * levelModMult + levelModAdd);

                npc.Configuration.Level = new NpcLevel() { Level = finalLevel };
            }
            else if (npc.Configuration.Level is NpcLevel npcLevel)
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
                if(Patcher.ModSettings.Value.Items.AllowMidTier)
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
                                entry.Item.Item = newItem.ToLink();
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
                            npc.DefaultOutfit = newOutfit.ToNullableLink();
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
            byte maxSkill = Patcher.ModSettings.Value.NPCs.NPCMaxSkillLevel;
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
            // NPCs that inherit stats from a template don't need their skills changed.
            if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Stats) && !npc.Template.IsNull)
                return false;

            float skillsPerLevel = Patcher.ModSettings.Value.NPCs.NPCSkillsPerLevel;
            if (skillsPerLevel > 0 && npc.PlayerSkills is not null && npc.Configuration.Level is NpcLevel npcLevel)
            {
                if (npc.Class.TryResolve(linkCache, out var classGetter))
                    DistributeSkills(classGetter.SkillWeights, npc.PlayerSkills.SkillValues, (int)Math.Round(skillsPerLevel * npcLevel.Level));

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
            if (perkCondition is null)
                return false;

            return perkCondition.CompareOperator switch
            {
                CompareOperator.EqualTo => lValue.CompareTo(rValue) == 0,
                CompareOperator.GreaterThan => lValue.CompareTo(rValue) > 0,
                CompareOperator.GreaterThanOrEqualTo => lValue.CompareTo(rValue) >= 0,
                CompareOperator.LessThan => lValue.CompareTo(rValue) < 0,
                CompareOperator.LessThanOrEqualTo => lValue.CompareTo(rValue) <= 0,
                CompareOperator.NotEqualTo => lValue.CompareTo(rValue) != 0,
                _ => false,
            };
        }

        private static void RemoveOldPerks(Npc npc, ILinkCache vanillaCache)
        {
            if (npc.Perks is null || npc.Perks.Count <= 0)
                return;

            for(int i=npc.Perks.Count - 1; i>=0; --i)
            {
                // Always keep the base skill boosts.
                if (npc.Perks[i].Perk.Equals(Skyrim.Perk.AlchemySkillBoosts) || npc.Perks[i].Perk.Equals(Skyrim.Perk.PerkSkillBoosts))
                    continue;

                // Remove if present in the vanilla cache.
                if (vanillaCache.TryResolve(npc.Perks[i].Perk, out _))
                    npc.Perks.RemoveAt(i);
            }
        }

        private static bool FulfillsPerkConditions(Npc npc, IPerkGetter perkEntry, Skill currSkill, ILinkCache linkCache)
        {
            // Check if NPC already has the perk or not.
            if (npc.Perks!.Where(x => x.Perk.Equals(perkEntry.ToLink())).Any()) return false;
            
            bool fulfillsConditions = true;
            foreach(IConditionGetter? perkCondition in perkEntry.Conditions)
            {
                if (perkCondition is null)
                    continue;

                if (perkCondition is not ConditionFloat condFloat)
                    continue;

                if (condFloat.Data is GetBaseActorValueConditionData avData && (int)avData.ActorValue == (int)currSkill)
                {
                    fulfillsConditions = PerformCompare(condFloat, npc.PlayerSkills!.SkillValues[currSkill], condFloat.ComparisonValue);
                }
                else if (condFloat.Data is HasPerkConditionData perkData && perkData.Perk.Link.TryResolve(linkCache, out var requiredPerk))
                {
                    if (condFloat.CompareOperator == CompareOperator.EqualTo && condFloat.ComparisonValue == 1 || condFloat.CompareOperator == CompareOperator.NotEqualTo && condFloat.ComparisonValue == 0)
                        fulfillsConditions = npc.Perks!.Where(x => x.Perk.Equals(requiredPerk.ToLink())).Any();
                    else if (condFloat.CompareOperator == CompareOperator.EqualTo && condFloat.ComparisonValue == 0 || condFloat.CompareOperator == CompareOperator.NotEqualTo && condFloat.ComparisonValue == 1)
                        fulfillsConditions = !npc.Perks!.Where(x => x.Perk.Equals(requiredPerk.ToLink())).Any();
                }
                else return false;
            }
            
            return fulfillsConditions;
        }

        private static bool DistributeNPCPerks(Npc npc, ILinkCache linkCache, ILinkCache vanillaCache)
        {
            // NPCs with the inherit spell list flag actually inherit perks, so no need to change them.
            if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList) && !npc.Template.IsNull)
                return false;

            // Check the keyword filters to see if any should be excluded.
            if (!ShouldModifyPerks(npc, linkCache))
                return false;

            float perksPerLevel = Patcher.ModSettings.Value.NPCs.NPCPerksPerLevel;
            if(perksPerLevel > 0)
            {
                if (npc.PlayerSkills is null)
                    return false;

                if (!npc.Class.TryResolve<IClassGetter>(linkCache, out IClassGetter? npcClass))
                    return false;

                if (!npc.Race.TryResolve<IRaceGetter>(linkCache, out IRaceGetter? npcRace))
                    return false;

                if (!npcRace.HasKeyword(Skyrim.Keyword.ActorTypeNPC) && !npcRace.HasKeyword(Skyrim.Keyword.ActorTypeUndead))
                    return false;

                if (npc.Configuration.Level is NpcLevel npcLevel)
                    perksPerLevel *= npcLevel.Level;

                npc.Perks ??= new();
                if (Patcher.ModSettings.Value.NPCs.RemoveVanillaPerks)
                    RemoveOldPerks(npc, vanillaCache);

                int perkOverflow = 0;
                List<KeyValuePair<Skill, byte>> perkDistribution = npcClass.SkillWeights.ToList();
                float weightSum = perkDistribution.Any() ? perkDistribution.Sum(x => x.Value) : 0;
                foreach(KeyValuePair<Skill, byte> perkWeight in perkDistribution)
                {
                    if (perkWeight.Value <= 0)
                        continue;

                    byte perksToSpend = (byte)Math.Round(perkOverflow + perksPerLevel * (perkWeight.Value / weightSum));
                    if (perksToSpend <= 0)
                        continue;

                    if (!GetTreeFromSkill(perkWeight.Key, linkCache, out var perkTree) || perkTree!.PerkTree is null)
                        continue;

                    while(perksToSpend > 0)
                    {
                        bool wasPerkAdded = false;
                        foreach(IActorValuePerkNodeGetter? perkNode in perkTree.PerkTree)
                        {
                            if (perksToSpend <= 0)
                                break;

                            if (!perkNode.Perk.TryResolve(linkCache, out var perkEntry) || perkEntry.EditorID is null)
                                continue;

                            if (excludedPerks!.Keys.Any(key => perkEntry.EditorID.Contains(key, StringComparison.OrdinalIgnoreCase)) && !excludedPerks.ForbiddenKeys.Any(key => perkEntry.EditorID.Contains(key, StringComparison.OrdinalIgnoreCase)))
                                continue;

                            if (FulfillsPerkConditions(npc, perkEntry, perkWeight.Key, linkCache))
                            {
                                --perksToSpend;
                                wasPerkAdded = true;
                                npc.Perks!.Add(new PerkPlacement() { Perk = perkEntry.ToLink(), Rank = 1 });
                            }

                            while (perksToSpend > 0 && perkEntry.NextPerk.TryResolve<IPerkGetter>(linkCache, out perkEntry))
                            {
                                if (FulfillsPerkConditions(npc, perkEntry, perkWeight.Key, linkCache))
                                {
                                    --perksToSpend;
                                    wasPerkAdded = true;
                                    npc.Perks!.Add(new PerkPlacement() { Perk = perkEntry.ToLink(), Rank = 1 });
                                } else break;
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
            if (!Patcher.ModSettings.Value.NPCs.DisableExtraDamagePerks)
                return;

            foreach (IPerkGetter? perkGetter in state.LoadOrder.PriorityOrder.Perk().WinningOverrides().Where(x => x.EditorID is not null && x.EditorID.Contains("crExtraDamage", StringComparison.OrdinalIgnoreCase)))
            {
                Perk perkCopy = state.PatchMod.Perks.GetOrAddAsOverride(perkGetter);
                perkCopy.Effects.Clear();
            }
        }

        private static bool IsFollower(Npc npc)
        {
            if (!Patcher.ModSettings.Value.NPCs.ScalingFollowers || npc.EditorID is null)
                return false;

            bool isFollower = npc.Factions.Any(rankPlacement => rankPlacement.Faction.Equals(Skyrim.Faction.PotentialFollowerFaction) || rankPlacement.Faction.Equals(Skyrim.Faction.PotentialHireling));
            foreach (FollowerEntry? followerEntry in followerList!.Followers)
            {
                if (npc.EditorID.Contains(followerEntry.Key, StringComparison.OrdinalIgnoreCase))
                    isFollower = true;

                foreach (string? forbiddenKey in followerEntry.ForbiddenKeys)
                {
                    if (npc.EditorID.Contains(forbiddenKey))
                    {
                        isFollower = false;
                        break;
                    }
                }

                break;
            }

            return isFollower;
        }

        private static bool SetFollowerScaling(Npc npc)
        {
            if (!IsFollower(npc))
                return false;

            short currLevel = (npc.Configuration.Level as NpcLevel)?.Level ?? 40;
            npc.Configuration.Level = new PcLevelMult { LevelMult = 1 };
            npc.Configuration.CalcMinLevel = Math.Max(npc.Configuration.CalcMinLevel, (short)1);
            npc.Configuration.CalcMaxLevel = Math.Max(npc.Configuration.CalcMaxLevel, currLevel);

            return true;
        }

        /// <summary>
        /// Processes the leaves of the leveled item lists and manages the skill weights dictionary.
        /// </summary>
        /// <param name="itemGetter"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        /// <param name="divisor"></param>
        private static void GetItemSkillWeights(IItemGetter itemGetter, IDictionary<Skill, float> skillWeights, float divisor = 1)
        {
            // Add to weights when desired items are found.
            if (itemGetter is IWeaponGetter weaponGetter)
            {
                if (weaponGetter.Data is null || weaponGetter.Data.Skill is null)
                    return;

                skillWeights[(Skill)weaponGetter.Data.Skill] += 1 / divisor;
            }
            else if (itemGetter is IArmorGetter armorGetter)
            {
                if (armorGetter.HasKeyword(Skyrim.Keyword.ArmorShield))
                    skillWeights[Skill.Block] += 1 / divisor;

                if (armorGetter.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    skillWeights[Skill.HeavyArmor] += 1 / divisor;
                else if (armorGetter.HasKeyword(Skyrim.Keyword.ArmorLight))
                    skillWeights[Skill.LightArmor] += 1 / divisor;
            }
        }

        /// <summary>
        /// Traverses the leveled item lists and calls the function managing the skill weights when a leaf is found.
        /// </summary>
        /// <param name="lvliGetter"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        private static void GetItemSkillWeights(ILeveledItemGetter lvliGetter, IDictionary<Skill, float> skillWeights, ILinkCache linkCache, float divisor = 1)
        {
            List<ILeveledItemEntryGetter> nodes = lvliGetter.Entries?.ToList() ?? new();
            while (nodes.Any())
            {
                var node = nodes.Last();
                nodes.RemoveAt(nodes.Count - 1);
                if (node.Data is null || !node.Data.Reference.TryResolve(linkCache, out var entryGetter))
                    continue;

                if (entryGetter is ILeveledItemGetter lvliNode)
                {
                    foreach (var child in lvliNode.Entries.EmptyIfNull())
                        nodes.Add(child);
                }
                else GetItemSkillWeights(entryGetter, skillWeights, divisor);
            }
        }

        /// <summary>
        /// Processes outfits, manages the skill weights if an outfit contains an armor, otherwise calls the leveled list traversal function.
        /// </summary>
        /// <param name="outfitTargetGetter"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        private static void GetItemSkillWeights(IOutfitTargetGetter outfitTargetGetter, IDictionary<Skill, float> skillWeights, ILinkCache linkCache)
        {
            // Go through the leveled list tree.
            if (outfitTargetGetter is ILeveledItemGetter leveledItemGetter)
            {
                GetItemSkillWeights(leveledItemGetter, skillWeights, linkCache);
            }
            // Add to weights when desired items are found.
            else if (outfitTargetGetter is IArmorGetter armorGetter)
            {
                if (armorGetter.HasKeyword(Skyrim.Keyword.ArmorShield))
                    skillWeights[Skill.Block] += 1;

                if (armorGetter.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    skillWeights[Skill.HeavyArmor] += 1;
                else if (armorGetter.HasKeyword(Skyrim.Keyword.ArmorLight))
                    skillWeights[Skill.LightArmor] += 1;
            }
        }

        /// <summary>
        /// Processes the leaves of the leveled spell lists and managed the skill weights dictionary.
        /// </summary>
        /// <param name="spellRecordGetter"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        /// <param name="divisor"></param>
        private static void GetSpellSkillWeights(ISpellRecordGetter spellRecordGetter, IDictionary<Skill, float> skillWeights, ILinkCache linkCache, float divisor = 1)
        {
            if (spellRecordGetter is not ISpellGetter finalSpell)
                return;

            if (finalSpell.Type != SpellType.Spell)
                return;

            // Get magic skill from magic effect.
            foreach (var effectGetter in finalSpell.Effects)
            {
                if(effectGetter.BaseEffect.IsNull || !effectGetter.BaseEffect.TryResolve(linkCache, out var mgefGetter))
                    continue;

                // Return once incremented, don't need to go through all mgefs, just until we find one with one of the magic schools.
                switch (mgefGetter.MagicSkill)
                {
                    case ActorValue.Destruction:
                        skillWeights[Skill.Destruction] += 1 / divisor;
                        return;
                    case ActorValue.Alteration:
                        skillWeights[Skill.Alteration] += 1 / divisor;
                        return;
                    case ActorValue.Conjuration:
                        skillWeights[Skill.Conjuration] += 1 / divisor;
                        return;
                    case ActorValue.Illusion:
                        skillWeights[Skill.Illusion] += 1 / divisor;
                        return;
                    case ActorValue.Restoration:
                        skillWeights[Skill.Restoration] += 1 / divisor;
                        return;
                }
            }
        }

        /// <summary>
        /// Traverses the leveled spell lists and calls the function managing the skill weights when a leaf is found.
        /// </summary>
        /// <param name="leveledSpellGetter"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        /// <param name="divisor"></param>
        private static void GetSpellSkillWeights(ILeveledSpellGetter leveledSpellGetter, IDictionary<Skill, float> skillWeights, ILinkCache linkCache, float divisor = 1)
        {
            List<ILeveledSpellEntryGetter> nodes = leveledSpellGetter.Entries?.ToList() ?? new();
            while (nodes.Any())
            {
                var node = nodes.Last();
                nodes.RemoveAt(nodes.Count - 1);
                if (node.Data is null || !node.Data.Reference.TryResolve(linkCache, out var entryGetter))
                    continue;

                if (entryGetter is ILeveledSpellGetter lvlSpellNode)
                {
                    foreach (var child in lvlSpellNode.Entries.EmptyIfNull())
                        nodes.Add(child);
                }
                else GetSpellSkillWeights(entryGetter, skillWeights, linkCache, divisor);
            }
        }

        /// <summary>
        /// Resolves npc templates and traverses leveled npc lists, then calls GetItemSkillWeights on the leaves.
        /// </summary>
        /// <param name="npcSpawn"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        private static void PopulateByInventory(INpcSpawnGetter npcSpawn, IDictionary<Skill, float> skillWeights, ILinkCache linkCache)
        {
            List<ValueTuple<INpcSpawnGetter, int>> nodes = new();
            if (npcSpawn is INpcGetter npcGetter)
            {
                if (npcGetter.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory) && npcGetter.Template.TryResolve(linkCache, out var newNpcSingleSpawn))
                {
                    nodes.Add(new(newNpcSingleSpawn, 1));
                }
                else // Populate skill weights by inventory.
                {
                    foreach (var itemEntry in npcGetter.Items.EmptyIfNull())
                    {
                        if (!itemEntry.Item.Item.TryResolve(linkCache, out var itemGetter))
                            continue;

                        if (itemGetter is ILeveledItemGetter leveledItemGetter)
                            GetItemSkillWeights(leveledItemGetter, skillWeights, linkCache);
                        else
                            GetItemSkillWeights(itemGetter, skillWeights);
                    }
                }
            }
            else if (npcSpawn is ILeveledNpcGetter listGetter)
            {
                foreach (var listEntry in listGetter.Entries.EmptyIfNull())
                {
                    if (listEntry.Data is null || !listEntry.Data.Reference.TryResolve(linkCache, out var newNpcListSpawn))
                        continue;

                    nodes.Add(new(newNpcListSpawn, listGetter.Entries!.Count));
                }
            }

            while (nodes.Any())
            {
                var node = nodes.Last();
                nodes.RemoveAt(nodes.Count - 1);

                if (node.Item1 is ILeveledNpcGetter listGetter)
                {
                    foreach (var child in listGetter.Entries.EmptyIfNull())
                    {
                        if (child.Data is null || !child.Data.Reference.TryResolve(linkCache, out var listSpawn))
                            continue;

                        nodes.Add(new(listSpawn, listGetter.Entries!.Count));
                    }
                }
                else if (node.Item1 is INpcGetter singleGetter)
                {
                    if (singleGetter.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory) && singleGetter.Template.TryResolve(linkCache, out var singleSpawn))
                    {
                        nodes.Add(new(singleSpawn, 1));
                    }
                    else // Populate skill weights by inventory.
                    {
                        foreach (var itemEntry in singleGetter.Items.EmptyIfNull())
                        {
                            if (!itemEntry.Item.Item.TryResolve(linkCache, out var itemGetter))
                                continue;

                            if (itemGetter is ILeveledItemGetter leveledItemGetter)
                                GetItemSkillWeights(leveledItemGetter, skillWeights, linkCache, node.Item2);
                            else
                                GetItemSkillWeights(itemGetter, skillWeights, node.Item2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves npc templates and traverses leveled npc lists, then calls GetSpellSkillWeights on the leaves.
        /// </summary>
        /// <param name="npcSpawn"></param>
        /// <param name="skillWeights"></param>
        /// <param name="linkCache"></param>
        private static void PopulateBySpells(INpcSpawnGetter npcSpawn, IDictionary<Skill, float> skillWeights, ILinkCache linkCache)
        {
            List<ValueTuple<INpcSpawnGetter, int>> nodes = new();
            if (npcSpawn is INpcGetter npcGetter)
            {
                if (npcGetter.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory) && npcGetter.Template.TryResolve(linkCache, out var newNpcSingleSpawn))
                {
                    nodes.Add(new(newNpcSingleSpawn, 1));
                }
                else // Populate skill weights by inventory.
                {
                    foreach (var spellEntry in npcGetter.ActorEffect.EmptyIfNull())
                    {
                        if (!spellEntry.TryResolve(linkCache, out var spellRecordGetter))
                            continue;

                        if (spellRecordGetter is ILeveledSpellGetter leveledSpellGetter)
                            GetSpellSkillWeights(leveledSpellGetter, skillWeights, linkCache);
                        else
                            GetSpellSkillWeights(spellRecordGetter, skillWeights, linkCache);
                    }
                }
            }
            else if (npcSpawn is ILeveledNpcGetter listGetter)
            {
                foreach (var listEntry in listGetter.Entries.EmptyIfNull())
                {
                    if (listEntry.Data is null || !listEntry.Data.Reference.TryResolve(linkCache, out var newNpcListSpawn))
                        continue;

                    nodes.Add(new(newNpcListSpawn, listGetter.Entries!.Count));
                }
            }

            while (nodes.Any())
            {
                var node = nodes.Last();
                nodes.RemoveAt(nodes.Count - 1);

                if (node.Item1 is ILeveledNpcGetter listGetter)
                {
                    foreach (var child in listGetter.Entries.EmptyIfNull())
                    {
                        if (child.Data is null || !child.Data.Reference.TryResolve(linkCache, out var listSpawn))
                            continue;

                        nodes.Add(new(listSpawn, listGetter.Entries!.Count));
                    }
                }
                else if (node.Item1 is INpcGetter singleGetter)
                {
                    if (singleGetter.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory) && singleGetter.Template.TryResolve(linkCache, out var singleSpawn))
                    {
                        nodes.Add(new(singleSpawn, 1));
                    }
                    else // Populate skill weights by inventory.
                    {
                        foreach (var spellEntry in singleGetter.ActorEffect.EmptyIfNull())
                        {
                            if (!spellEntry.TryResolve(linkCache, out var spellRecordGetter))
                                continue;

                            if (spellRecordGetter is ILeveledSpellGetter leveledSpellGetter)
                                GetSpellSkillWeights(leveledSpellGetter, skillWeights, linkCache, node.Item2);
                            else
                                GetSpellSkillWeights(spellRecordGetter, skillWeights, linkCache, node.Item2);
                        }
                    }
                }
            }
        }

        private static void PopulateByOutfit(Npc npc, IDictionary<Skill, float> skillWeights, ILinkCache linkCache)
        {
            if (npc.DefaultOutfit.IsNull || !npc.DefaultOutfit.TryResolve(linkCache, out var outfitGetter))
                return;

            foreach (var outfitEntry in outfitGetter!.Items.EmptyIfNull())
            {
                if (!outfitEntry.TryResolve(linkCache, out var entryGetter))
                    continue;

                GetItemSkillWeights(entryGetter, skillWeights, linkCache);
            }
        }

        private static void PopulateSkillWeights(Npc npc, IDictionary<Skill, float> skillWeights, ILinkCache linkCache)
        {
            // Populate weights.
            PopulateByInventory(npc, skillWeights, linkCache);
            PopulateBySpells(npc, skillWeights, linkCache);
            PopulateByOutfit(npc, skillWeights, linkCache);

            // Ceil them all to the nearest whole value.
            skillWeights.ForEach(x => skillWeights[x.Key] = (float)Math.Ceiling(x.Value));
        }

        private static void CalculateClassWeights(Class npcClass, IDictionary<Skill, float> newSkillWeights)
        {
            List<KeyValuePair<Skill, float>> weightList = newSkillWeights.Where(entry => entry.Value > 0).ToList();

            // Sort the weights in ascending value.
            weightList.Sort((x, y) => x.Value >= y.Value ? 1 : -1);

            // Assign the real weights.
            float lastValue = weightList[0].Value;
            for (int weight = 1, i = 0; i < weightList.Count; i++)
            {
                if (weightList[i].Value > lastValue)
                {
                    weight = i + 1;
                    lastValue = weightList[i].Value;
                }

                weightList[i] = new (weightList[i].Key, weight);
            }

            // Apply stat distribution ratios to skill weights to correct overspec in the wrong skill.
            bool isHybridClass = weightList.Where(x => x.Key == Skill.Block || x.Key == Skill.OneHanded || x.Key == Skill.TwoHanded || x.Key == Skill.LightArmor || x.Key == Skill.HeavyArmor).Sum(x => x.Value) > 0 &&
                weightList.Where(x => x.Key == Skill.Illusion || x.Key == Skill.Alteration || x.Key == Skill.Conjuration || x.Key == Skill.Destruction || x.Key == Skill.Restoration).Sum(x => x.Value) > 0;

            float combatRatio = -1;
            float magicRatio = -1;
            if (isHybridClass)
            {
                float weightSum = npcClass.StatWeights.Sum(x => x.Value);
                float combatSum = npcClass.StatWeights[BasicStat.Health] > npcClass.StatWeights[BasicStat.Magicka] ? npcClass.StatWeights[BasicStat.Health] + npcClass.StatWeights[BasicStat.Stamina] : npcClass.StatWeights[BasicStat.Health];
                float magicSum = npcClass.StatWeights[BasicStat.Magicka] > npcClass.StatWeights[BasicStat.Health] ? npcClass.StatWeights[BasicStat.Magicka] + npcClass.StatWeights[BasicStat.Stamina] : npcClass.StatWeights[BasicStat.Magicka];
                if (npcClass.StatWeights[BasicStat.Health] == npcClass.StatWeights[BasicStat.Magicka])
                    weightSum -= npcClass.StatWeights[BasicStat.Stamina];

                magicRatio = magicSum / weightSum;
                combatRatio = combatSum / weightSum;
                for (int i = 0; i < weightList.Count; i++)
                {
                    switch (weightList[i].Key)
                    {
                        case Skill.Block:
                        case Skill.OneHanded:
                        case Skill.TwoHanded:
                        case Skill.LightArmor:
                        case Skill.HeavyArmor:
                            weightList[i] = new(weightList[i].Key, (float)Math.Round(weightList[i].Value * combatRatio));
                            break;

                        case Skill.Illusion:
                        case Skill.Alteration:
                        case Skill.Conjuration:
                        case Skill.Destruction:
                        case Skill.Restoration:
                            weightList[i] = new(weightList[i].Key, (float)Math.Round(weightList[i].Value * magicRatio));
                            break;
                    }
                }
            }

            weightList.ForEach(x => newSkillWeights[x.Key] = x.Value);

            // If enemy is a combat class and has no shield then give them some block skill.
            if (!isHybridClass && newSkillWeights[Skill.Block] == 0 && (newSkillWeights[Skill.OneHanded] > 0 || newSkillWeights[Skill.TwoHanded] > 0))
                newSkillWeights[Skill.Block] = 1;
        }

        private static bool RebalanceClassValues(Npc npc, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILinkCache linkCache)
        {
            if (!Patcher.ModSettings.Value.NPCs.RebuildNPCClasses)
                return false;

            // NPCs that inherit stats inherit class too, so they can be skipped from generation.
            if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Stats) && !npc.Template.IsNull)
                return false;

            if (!npc.Class.TryResolve(linkCache, out var classGetter))
                return false;

            // Skip unique NPCs like smiths, alchemists, shopkeeps, etc.
            if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique))
            {
                if (ExcludedClasses.Any(entry => classGetter.EditorID?.Contains(entry, StringComparison.OrdinalIgnoreCase) ?? false) || ExcludedClasses.Any(entry => classGetter.Name?.String?.Contains(entry, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
            }

            IDictionary<Skill, float> skillWeights = new Dictionary<Skill, float>();
            classGetter.SkillWeights.ForEach(x => skillWeights[x.Key] = 0); // Populate the dictionary.

            PopulateSkillWeights(npc, skillWeights, linkCache);
            if (skillWeights.All(x => x.Value == 0)) // No data for generating new class.
                return false;

            // Make a new class unique to the NPC.
            var newClass = state.PatchMod.Classes.AddNew();
            newClass.DeepCopyIn(classGetter);
            newClass.EditorID = "TUSClass" + npc.EditorID;
            npc.Class = newClass.ToLink();

            CalculateClassWeights(newClass, skillWeights);
            newClass.SkillWeights.ForEach(x => newClass.SkillWeights[x.Key] = 0);
            skillWeights.ForEach(x => newClass.SkillWeights[x.Key] = (byte)x.Value);

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
            var vanillaCache = LoadOrder.Import<ISkyrimModGetter>(state.DataFolderPath, new List<ModKey>() { Skyrim.ModKey, Dawnguard.ModKey, Dragonborn.ModKey }, GameRelease.SkyrimSE).PriorityOrder.ToImmutableLinkCache();
            foreach (INpcGetter? npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                if (npcGetter.EditorID is null)
                    continue;

                if (npcGetter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsCharGenFacePreset) || npcGetter.HasKeyword(Skyrim.Keyword.PlayerKeyword))
                    continue;

                if (excludedNPCs.Keys.Any(key => npcGetter.EditorID.Contains(key, StringComparison.OrdinalIgnoreCase)) && !excludedNPCs.ForbiddenKeys.Any(key => npcGetter.EditorID.Contains(key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                bool wasChanged = false;
                Npc npcCopy = npcGetter.DeepCopy();

                wasChanged |= SetStaticLevel(npcCopy, Patcher.LinkCache);
                wasChanged |= RebalanceClassValues(npcCopy, state, Patcher.LinkCache); // since it uses a static link cache it has to go before equipment changes, otherwise it will try to use missing data
                wasChanged |= ChangeEquipment(npcCopy, state, Patcher.LinkCache);
                wasChanged |= RelevelNPCSkills(npcCopy, state.LinkCache); // dynamic link cache to account for local class changes
                wasChanged |= DistributeNPCPerks(npcCopy, state.LinkCache, vanillaCache);
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

            Console.WriteLine("Processed " + processedRecords + " npcs in total.\n");
        }
    }
}
