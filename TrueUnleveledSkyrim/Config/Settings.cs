using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueUnleveledSkyrim.Config
{
    public class TUSConfig
    {
        public TUSConfig_LeveledLists LeveledLists { get; set; } = new TUSConfig_LeveledLists();

        public TUSConfig_ItemAdjustments ItemAdjustments { get; set; } = new TUSConfig_ItemAdjustments();
    }

    public class TUSConfig_LeveledLists
    {
        [Tooltip("If enabled, leveled item lists will be unleveled and purged of items higher or lower than the specified levels.")]
        public bool UnlevelItemLists { get; set; } = true;
        public TUSConfig_Items ItemListOptions { get; set; } = new TUSConfig_Items();
    }

    public class TUSConfig_ItemAdjustments
    {
        [Tooltip("If enabled, changes item stats to match Morrowloot's vision. Glass, Ebony, Stalhrim, Dragon, Daedric, and unique items will be stronger.")]
        public bool MorrowlootifyItems { get; set; } = true;
        public TUSConfig_Morrowloot MorrowlootifyOptions { get; set; } = new TUSConfig_Morrowloot();
    }

    public class TUSConfig_Items
    {
        [Tooltip("The level from which items are purged from leveled lists. Setting it to 0 means there is no upper level limit.\nTiers (based on vanilla leveled lists):\n1 - Iron\n2 - Steel\n6 - Orcish\n12 - Dwarven\n19 - Elven\n27 - Glass\n36 - Ebony\n46 - Daedric")]
        public int MaxItemLevel { get; set; } = 27;
        [Tooltip("The level below which items are purged from leveled lists. Setting it to 0 means there is no lower level limit.\nTiers (based on vanilla leveled lists):\n1 - Iron\n2 - Steel\n6 - Orcish\n12 - Dwarven\n19 - Elven\n27 - Glass\n36 - Ebony\n46 - Daedric")]
        public int MinItemLevel { get; set; } = 0;
        [Tooltip("If enabled, artifact items will always have the highest level variant in the leveled list.")]
        public bool UnlevelArtifacts { get; set; } = true;
        [Tooltip("If enabled, artifact unleveling will search for artifact lists manually instead of using the predefined artifact keys. Takes much longer, but also works on leveled artifacts added by other mods.")]
        public bool SlowArtifactUnleveling { get; set; } = false;
    }
    public class TUSConfig_Morrowloot
    {
        [Tooltip("If enabled, skips stat adjusment on all items tagged as a \"Daedric Artifact\".")]
        public bool SkipArtifacts { get; set; } = false;
        [Tooltip("If enabled, skips stat adjustment on all unique items, namely the ones marked with the tag to \"Disallow Enchanting\".")]
        public bool SkipUniques { get; set; } = false;
        [Tooltip("NOT YET IMPLEMENTED - If enabled, tempering is made about 40% less effective across the board for balancing reasons, making way for the artifacts to shine without Smithing being mandatory.")]
        public bool TemperingDebuff { get; set; } = true;
    }
}
