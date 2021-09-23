using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

using TrueUnleveledSkyrim.Config;

namespace TrueUnleveledSkyrim.Patch
{
    class OutfitsPatcher
    {
        // Replaces leveled item list entries in weak and strong outfit variants with the respective weak and strong variants of the list.
        private static bool ReplaceLVLIEntries(Outfit outfit, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILinkCache linkCache, bool isWeak)
        {
            bool wasChanged = false;
            for(int i = 0; i<outfit.Items!.Count; ++i)
            {
                ILeveledItemGetter? resolvedItem = outfit.Items[i].TryResolve<ILeveledItemGetter>(linkCache);
                if (resolvedItem is not null)
                {
                    string usedPostfix = isWeak ? TUSConstants.WeakPostfix : TUSConstants.StrongPostfix;
                    LeveledItem? newItem = state.PatchMod.LeveledItems.Where(x => x.EditorID == resolvedItem.EditorID + usedPostfix).FirstOrDefault();
                    if (newItem is not null)
                    {
                        wasChanged = true;
                        outfit.Items[i] = newItem.AsLink();
                    }
                }
            }

            return wasChanged;
        }

        // Main function to unlevel outfits.
        public static void PatchOutfits(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            uint processedRecords = 0;
            foreach (IOutfitGetter? outfitGetter in state.LoadOrder.PriorityOrder.Outfit().WinningOverrides())
            {
                if (outfitGetter.Items is null) continue;

                Outfit weakCopy = new Outfit(state.PatchMod); // state.PatchMod.Outfits.AddNew();
                Outfit strongCopy = new Outfit(state.PatchMod); // state.PatchMod.Outfits.AddNew();
                weakCopy.DeepCopyIn(outfitGetter);
                strongCopy.DeepCopyIn(outfitGetter);
                weakCopy.EditorID += TUSConstants.WeakPostfix;
                strongCopy.EditorID += TUSConstants.StrongPostfix;

                if (ReplaceLVLIEntries(weakCopy, state, Patcher.LinkCache, true))
                    state.PatchMod.Outfits.Set(weakCopy);
                if (ReplaceLVLIEntries(strongCopy, state, Patcher.LinkCache, false))
                    state.PatchMod.Outfits.Set(strongCopy);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " outfits.");
            }

            Console.WriteLine("Processed " + processedRecords + " outfits in total.");
        }
    }
}
