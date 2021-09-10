using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

namespace TrueUnleveledSkyrim
{
    public class LeveledItemsPatcher
    {
        // Checks if the given leveled list is an artifact list by checking if all the item names in the list are the same.
        private static bool IsArtifactList(LeveledItem itemList, ILinkCache linkCache)
        {
            int entryCount = 0;
            string? itemName = null;
            foreach(LeveledItemEntry? itemEntry in itemList.Entries.EmptyIfNull())
            {
                if (itemEntry.Data is null) continue;
                if (!itemEntry.Data.Reference.TryResolve(linkCache, out var resolvedItem)) continue;
                
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

        public static void UnlevelItems(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach(var lvlItemGetter in state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides())
            {
                bool wasChanged = false;
                LeveledItem listCopy = lvlItemGetter.DeepCopy();

                wasChanged |= IsArtifactList(listCopy, state.LinkCache) ? CullArtifactList(listCopy) : RemoveRareItems(listCopy, state.LinkCache);
                wasChanged |= UnlevelList(listCopy);
                if (wasChanged)
                {
                    state.PatchMod.LeveledItems.Set(listCopy);
                    Console.WriteLine("Modifed leveled item list: " + listCopy.EditorID);
                }
            }
        }
    }
}
