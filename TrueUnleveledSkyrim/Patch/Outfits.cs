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

namespace TrueUnleveledSkyrim.Patch
{
    class OutfitsPatcher
    {
        // just go through outfits, make a weak and strong copy, change the LVLI entries inside of them to their weak or strong postfix versions if they exist
        private static bool DoShit(Outfit outfit)
        {
            bool wasChanged = false;


            return wasChanged;
        }

        public static void PatchOutfits(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            uint processedRecords = 0;
            foreach (IOutfitGetter? outfitGetter in state.LoadOrder.PriorityOrder.Outfit().WinningOverrides())
            {
                bool wasChanged = false;
                Outfit outfitCopy = outfitGetter.DeepCopy();

                wasChanged |= DoShit(outfitCopy);

                ++processedRecords;
                if (processedRecords % 100 == 0)
                    Console.WriteLine("Processed " + processedRecords + " outfits.");

                if (wasChanged)
                {
                    state.PatchMod.Outfits.Set(outfitCopy);
                }
            }

            Console.WriteLine("Processed " + processedRecords + " outfits in total.");
        }
    }
}
