using System;
using System.Linq;

using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Records;

using TrueUnleveledSkyrim.Config;


namespace TrueUnleveledSkyrim.Patch
{
    public class LeveledItemsPatcher
    {
        private static ArtifactKeys? artifactKeys;

        // Determines if a given leveled list holds artifacts or not based on the predefined EDID snippets in artifactKeys.json.
        private static bool IsArtifactList(LeveledItem itemList)
        {
            if (itemList.EditorID is null) return false;
            foreach (string? artifactKey in artifactKeys!.Keys)
            {
                if (itemList.EditorID.Contains(artifactKey, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // Checks if the given leveled list is an artifact list by checking if all the item names in the list are the same.
        private static bool IsArtifactList(LeveledItem itemList, ILinkCache linkCache)
        {
            if (!Patcher.ModSettings.Value.LeveledLists.ItemListOptions.SlowArtifactUnleveling)
                return IsArtifactList(itemList);

            int entryCount = 0;
            string? itemName = null;

            foreach(LeveledItemEntry? itemEntry in itemList.Entries.EmptyIfNull())
            {
                if (itemEntry.Data is null) return false;
                if (!itemEntry.Data.Reference.TryResolve(linkCache, out var resolvedItem)) return false;
                
                if(resolvedItem is INamedGetter namedItem)
                {
                    if (itemName == namedItem.Name)
                    {
                        ++entryCount;
                    }
                    else if (itemName.IsNullOrEmpty())
                    {
                        itemName = namedItem.Name;
                        ++entryCount;
                    }
                }
            }

            return entryCount == (itemList.Entries?.Count ?? 0);
        }

        // Removes every item from a list other than the highest level one.
        private static bool CullArtifactList(LeveledItem itemList)
        {
            if (itemList.Entries is null || !Patcher.ModSettings.Value.LeveledLists.ItemListOptions.UnlevelArtifacts)
                return false;

            bool wasChanged = false;
            int levelMax = itemList.Entries.Select(x => x.Data!.Level).Max();
            for(int i=itemList.Entries.Count - 1; i>=0; --i)
            {
                if (itemList.Entries[i].Data!.Level != levelMax)
                {
                    itemList.Entries.RemoveAt(i);
                    wasChanged = true;
                }
            }

            return wasChanged;
        }

        // Checks if an item should be removed based on the minimum and maximum level set in the options.
        private static bool ShouldRemoveItem(ILeveledItemEntryDataGetter? itemData, ILinkCache linkCache)
        {
            if (itemData is null)
                return false;

            int maxLevel = Patcher.ModSettings.Value.LeveledLists.ItemListOptions.MaxItemLevel;
            int minLevel = Patcher.ModSettings.Value.LeveledLists.ItemListOptions.MinItemLevel;
            bool shouldRemove = itemData.Level > maxLevel && maxLevel != 0 || itemData.Level < minLevel;

            if(itemData.Level == maxLevel && !shouldRemove)
            {
                IItemGetter resolvedItem = itemData.Reference.Resolve(linkCache);
                if(resolvedItem is not null && resolvedItem.EditorID is not null)
                    shouldRemove = resolvedItem.EditorID.ToLower().Contains("glass");
            }

            return shouldRemove;
        }

        // Removes items from a list if they should be deleted.
        private static bool RemoveRareItems(LeveledItem itemList, ILinkCache linkCache)
        {
            bool wasChanged = false;
            for (int i = (itemList.Entries?.Count ?? 0) - 1; i >= 0; --i)
            {
                if (ShouldRemoveItem(itemList.Entries![i].Data, linkCache))
                {
                    itemList.Entries!.RemoveAt(i);
                    wasChanged = true;
                }
            }

            return wasChanged;
        }

        // Sets all the item levels in a list to 1.
        private static bool UnlevelList(LeveledItem itemList)
        {
            bool wasChanged = false;
            foreach (var entry in itemList.Entries.EmptyIfNull())
            {
                if (entry.Data == null) continue;
                if (entry.Data.Level != 1)
                {
                    entry.Data.Level = 1;
                    wasChanged = true;
                }
            }

            return wasChanged;
        }

        // Gets the lowest and highest levels in a list.
        private static void GetLevelBoundaries(LeveledItem itemList, out int lvlMin, out int lvlMax)
        {
            lvlMin = Int16.MaxValue; lvlMax = -1;
            foreach (var entry in itemList.Entries.EmptyIfNull())
            {
                if (entry.Data is null) continue;
                if (entry.Data.Level < lvlMin)  lvlMin = entry.Data.Level;
                if (entry.Data.Level > lvlMax)  lvlMax = entry.Data.Level;
            }
        }

        // Removes items in a given level range from a list.
        private static bool RemoveItemsWithRange(LeveledItem itemList, int rangeLow, int rangeHigh)
        {
            bool wasChanged = false;
            for (int i = (itemList.Entries?.Count ?? 0) - 1; i >= 0; --i)
            {
                if (itemList.Entries![i].Data is null) continue;

                if (itemList.Entries![i].Data!.Level >= rangeLow && itemList.Entries![i].Data!.Level <= rangeHigh)
                {
                    itemList.Entries!.RemoveAt(i);
                    wasChanged = true;
                }
            }

            return wasChanged;
        }

        // Main function to unlevel all leveled item lists.
        public static void UnlevelItems(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            artifactKeys = JsonHelper.LoadConfig<ArtifactKeys>(TUSConstants.ArtifactKeysPath);

            uint processedRecords = 0;
            foreach(var lvlItemGetter in state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides())
            {
                bool wasChanged = false;
                LeveledItem listCopy = lvlItemGetter.DeepCopy();

                if (!IsArtifactList(listCopy, state.LinkCache))
                {
                    int lvlMin, lvlMax;

                    wasChanged |= RemoveRareItems(listCopy, state.LinkCache);
                    GetLevelBoundaries(listCopy, out lvlMin, out lvlMax);
                    if (lvlMin != Int16.MaxValue && lvlMax != -1)
                    {
                        LeveledItem weakCopy = state.PatchMod.LeveledItems.AddNew();
                        LeveledItem strongCopy = state.PatchMod.LeveledItems.AddNew();
                        weakCopy.DeepCopyIn(listCopy);
                        strongCopy.DeepCopyIn(listCopy);
                        weakCopy.EditorID += TUSConstants.WeakPostfix;
                        strongCopy.EditorID += TUSConstants.StrongPostfix;

                        if (lvlMin != lvlMax)
                        {
                            int lvlMed = (int)Math.Round((lvlMin + lvlMax) * 0.4);
                            RemoveItemsWithRange(weakCopy, lvlMed + 1, lvlMax);
                            RemoveItemsWithRange(strongCopy, lvlMin, lvlMed - 1);
                        }
                        UnlevelList(weakCopy);
                        UnlevelList(strongCopy);
                        
                        // Console.WriteLine("Added lists: " + weakCopy.EditorID + " and " + strongCopy.EditorID);
                    }

                }
                else wasChanged |= CullArtifactList(listCopy);

                wasChanged |= UnlevelList(listCopy);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " leveled item lists.");

                if (wasChanged)
                {
                    state.PatchMod.LeveledItems.Set(listCopy);
                    // Console.WriteLine("Modifed leveled item list: " + listCopy.EditorID);
                }
            }

            Console.WriteLine("Processed " + processedRecords + " leveled item lists in total.");
        }
    }
}
