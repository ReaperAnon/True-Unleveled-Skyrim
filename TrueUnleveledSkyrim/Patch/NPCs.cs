using System;
using System.Linq;

using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;

using TrueUnleveledSkyrim.Config;


namespace TrueUnleveledSkyrim.Patch
{
    class NPCsPatcher
    {
        private static ExcludedNPCs? excludedNPCs;
        private static NPCEDIDs? customNPCsByID;
        private static NPCFactions? customNPCsByFaction;
        private static RaceModifiers? raceModifiers;

        // Returns the level modifiers for the desired NPC based on their race.
        private static void GetLevelMultiplier(Npc npc, ILinkCache linkCache, out short levelModAdd, out float levelModMult)
        {
            levelModAdd = 0; levelModMult = 1;
            IRaceGetter? raceGetter = npc.Race.TryResolve(linkCache);

            if (raceGetter is null || raceGetter.EditorID is null) return;
            foreach(var dataSet in raceModifiers!.Data)
            {
                foreach (var raceKey in dataSet.Keys)
                {
                    if (raceGetter.EditorID.Contains(raceKey, StringComparison.OrdinalIgnoreCase))
                    {
                        bool willChange = true;
                        foreach (var exclusionKey in dataSet.ForbiddenKeys)
                        {
                            willChange = false;
                            break;
                        }

                        if (willChange)
                        {
                            levelModAdd = dataSet.LevelModifierAdd ?? 0;
                            levelModMult = dataSet.LevelModifierMult ?? 1;
                            break;
                        }
                    }

                    return;
                }
            }
        }

        // Gives the npcs defined in the NPCsByEDID.json file (EDID does not have to be complete) the custom level given.
        private static bool GetNPCLevelByEDID(Npc npc, short levelModAdd, float levelModMult)
        {
            foreach (var dataSet in customNPCsByID!.NPCs)
            {
                foreach (var npcKey in dataSet.Keys)
                {
                    if (npc.EditorID!.Contains(npcKey, StringComparison.OrdinalIgnoreCase))
                    {
                        bool willChange = true;
                        foreach (var exclusionKey in dataSet.ForbiddenKeys)
                        {
                            if (npc.EditorID!.Contains(exclusionKey, StringComparison.OrdinalIgnoreCase))
                            {
                                willChange = false;
                                break;
                            }
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
            Random randomizer = new();
            foreach (RankPlacement? rankEntry in npc.Factions)
            {
                IFactionGetter? faction = rankEntry.Faction.TryResolve(linkCache);

                if (faction is null) continue;
                foreach (var dataSet in customNPCsByFaction!.NPCs)
                {
                    foreach (var factionKey in dataSet.Keys)
                    {
                        if (faction.EditorID!.Contains(factionKey, StringComparison.OrdinalIgnoreCase))
                        {
                            bool willChange = true;
                            foreach (var exclusionKey in dataSet.ForbiddenKeys)
                            {
                                if (faction.EditorID.Contains(exclusionKey, StringComparison.OrdinalIgnoreCase))
                                {
                                    willChange = false;
                                    break;
                                }
                            }

                            if (willChange)
                            {
                                short newLevel = (short)(dataSet.Level ?? randomizer.Next((int)dataSet.MinLevel!, (int)dataSet.MaxLevel!));
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
                float lvlMult = pcLevelMult.LevelMult <= 0 ? 1 : pcLevelMult.LevelMult;
                short lvlMin = npc.Configuration.CalcMinLevel; short lvlMax = npc.Configuration.CalcMaxLevel;

                bool isUnique = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique);
                if (lvlMax == 0 || lvlMax > 81)
                    lvlMax = (short)(isUnique ? 81 : (50 + lvlMin) / 2);

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
        private static bool ChangeInventory(Npc npc, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILinkCache linkCache)
        {
            bool wasChanged = false;

            if(npc.Configuration.Level is NpcLevel npcLevel)
            {
                string usedPostfix = npcLevel.Level < 10 ? TUSConstants.WeakPostfix : npcLevel.Level > 25 ? TUSConstants.StrongPostfix : "";
                foreach (var entry in npc.Items.EmptyIfNull())
                {
                    ILeveledItemGetter? resolvedItem = entry.Item.Item.TryResolve<ILeveledItemGetter>(linkCache);
                    if (resolvedItem is not null)
                    {
                        LeveledItem? newItem = state.PatchMod.LeveledItems.Where(x => x.EditorID == resolvedItem.EditorID + usedPostfix).FirstOrDefault();
                        if (newItem is not null)
                            entry.Item.Item = newItem.AsLink();
                    }
                }
            }

            return wasChanged;
        }

        // Main function to unlevel all NPCs.
        public static void UnlevelNPCs(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            excludedNPCs = JsonHelper.LoadConfig<ExcludedNPCs>(TUSConstants.ExcludedNPCsPath);
            customNPCsByID = JsonHelper.LoadConfig<NPCEDIDs>(TUSConstants.NPCEDIDPath);
            customNPCsByFaction= JsonHelper.LoadConfig<NPCFactions>(TUSConstants.NPCFactionPath);
            raceModifiers = JsonHelper.LoadConfig<RaceModifiers>(TUSConstants.RaceModifiersPath);

            uint processedRecords = 0;
            foreach(INpcGetter? npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                if (npcGetter.EditorID is null) continue;

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
                wasChanged |= ChangeInventory(npcCopy, state, Patcher.LinkCache);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " npcs.");

                if (wasChanged)
                {
                    state.PatchMod.Npcs.Set(npcCopy);
                }
            }

            Console.WriteLine("Processed " + processedRecords + " npcs in total.");
        }
    }
}
