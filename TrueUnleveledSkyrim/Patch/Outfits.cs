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
        private static void ReplaceLVLIEntries(Outfit outfit, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILinkCache linkCache, bool isWeak)
        {
            for(int i = 0; i<outfit.Items!.Count; ++i)
            {
                ILeveledItemGetter? resolvedItem = outfit.Items[i].TryResolve<ILeveledItemGetter>(linkCache);
                if(resolvedItem is not null)
                {
                    string usedPostfix = isWeak ? TUSConstants.WeakPostfix : TUSConstants.StrongPostfix;
                    LeveledItem? newItem = state.PatchMod.LeveledItems.Where(x => x.EditorID == resolvedItem.EditorID + usedPostfix).FirstOrDefault();
                    if (newItem is not null)
                        outfit.Items[i] = newItem.AsLink();
                }
            }
        }

        // just go through outfits, make a weak and strong copy, change the LVLI entries inside of them to their weak or strong postfix versions if they exist
        public static void PatchOutfits(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            uint processedRecords = 0;
            foreach (IOutfitGetter? outfitGetter in state.LoadOrder.PriorityOrder.Outfit().WinningOverrides())
            {
                if (outfitGetter.Items is null) continue;

                Outfit weakCopy = state.PatchMod.Outfits.AddNew();
                Outfit strongCopy = state.PatchMod.Outfits.AddNew();
                weakCopy.DeepCopyIn(outfitGetter);
                strongCopy.DeepCopyIn(outfitGetter);
                weakCopy.EditorID += TUSConstants.WeakPostfix;
                strongCopy.EditorID += TUSConstants.StrongPostfix;

                ReplaceLVLIEntries(weakCopy, state, Patcher.LinkCache, true);
                ReplaceLVLIEntries(strongCopy, state, Patcher.LinkCache, false);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " outfits.");
            }

            Console.WriteLine("Processed " + processedRecords + " outfits in total.");
        }
    }
}
