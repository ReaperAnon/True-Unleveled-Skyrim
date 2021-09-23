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
        public TUSConfig_LeveledLists Unleveling { get; set; } = new TUSConfig_LeveledLists();


        public TUSConfig_ItemAdjustments ItemAdjustments { get; set; } = new TUSConfig_ItemAdjustments();
    }

    public class TUSConfig_LeveledLists
    {
        [Tooltip("If enabled, leveled item lists will be unleveled and purged of items higher or lower than the specified levels.")]
        public bool UnlevelGame { get; set; } = true;


        public TUSConfig_Unleveling Options { get; set; } = new TUSConfig_Unleveling();
    }

    public class TUSConfig_ItemAdjustments
    {
        [Tooltip("If enabled, changes item stats to match Morrowloot's vision. Glass, Ebony, Stalhrim, Dragon, Daedric, and unique items will be stronger.")]
        public bool MorrowlootifyItems { get; set; } = true;


        public TUSConfig_Morrowloot Options { get; set; } = new TUSConfig_Morrowloot();
    }

    public class TUSConfig_Unleveling
    {
        public TUSConfig_Zones Zones { get; set; } = new();
        [SettingName("NPCs")]
        public TUSConfig_NPCs NPCs { get; set; } = new();
        public TUSConfig_Items Items { get; set; } = new();
    }

    public class TUSConfig_Items
    {
        [Tooltip("The level from which items are purged from leveled lists. Setting it to 0 means there is no upper level limit.\nTiers (based on vanilla leveled lists):\n1 - Iron\n2 - Steel\n6 - Orcish\n12 - Dwarven\n19 - Elven\n27 - Glass\n36 - Ebony\n46 - Daedric")]
        public int MaxItemLevel { get; set; } = 27;


        [Tooltip("The level below which items are purged from leveled lists. Setting it to 0 means there is no lower level limit.\nTiers (based on vanilla leveled lists):\n1 - Iron\n2 - Steel\n6 - Orcish\n12 - Dwarven\n19 - Elven\n27 - Glass\n36 - Ebony\n46 - Daedric")]
        public int MinItemLevel { get; set; } = 0;


        [Tooltip("If enabled, artifact items will always have the highest level variant in the leveled list.")]
        public bool UnlevelArtifacts { get; set; } = true;
    }

    public class TUSConfig_Zones
    {
        [Tooltip("If enabled, the patcher will use Morrowloot Ultimate-like encounter zone level values for balancing.")]
        public bool UseMorrowlootZoneBalance { get; set; } = true;


        [Tooltip("If enabled, zones will not have different minimum and maximum levels and will not scale even minimally with the player, regardless of the defined ranges in the used configuration files.")]
        public bool StaticZoneLevels { get; set; } = true;


        [Tooltip("The level multiplier for easy spawns in encounter zones. At default, easy spawns will have a level that is 0.75x the level of the area itself. Vanilla value is 0.33.")]
        public float EasySpawnLevelMult { get; set; } = 0.75f;


        [Tooltip("The level multiplier for normal spawns in encounter zones. At default, normal spawns will have a level that is 1x the level of the area itself. Vanilla value is 0.67.")]
        public float NormalSpawnLevelMult { get; set; } = 1f;


        [Tooltip("The level multiplier for hard spawns in encounter zones. At default, hard spawns will have a level that is 1.25x the level of the area itself. Vanilla value is 1.")]
        public float HardSpawnLevelMult { get; set; } = 1.25f;


        [Tooltip("The level multiplier for very hard spawns in encounter zones. At default, very hard spawns will have a level that is 1.5x the level of the area itself. Vanilla value is 1.25.")]
        public float VeryHardSpawnLevelMult { get; set; } = 1.5f;
    }

    public class TUSConfig_NPCs
    {
        [Tooltip("If enabled, NPCs will have all their previously assigned perks removed before getting new ones added.")]
        public bool RemoveOldPerks { get; set; } = true;


        [Tooltip("The amount of perk points NPCs get per every level they have. Set to 0 to not grant them any perks. The perks are distributed according to their requirements.")]
        public float NPCPerksPerLevel { get; set; } = 1f;


        [Tooltip("The amount of skillpoints NPCs get per every level they have. Set to 0 to not change NPC skill levels. The points are distributed among their skills based on their class and their major and minor skills. All of their skills start at 15, these are applied on top of that.")]
        public float NPCSkillsPerLevel { get; set; } = 9.5f;


        [Tooltip("The maximum level an NPC's skills can have. If increased, high level NPCs can have some skills above level 100.")]
        public byte NPCMaxSkillLevel { get; set; } = 100;
    }

    public class TUSConfig_Morrowloot
    {
        [Tooltip("If enabled, skips stat adjusment on all items tagged as a \"Daedric Artifact\".")]
        public bool SkipArtifacts { get; set; } = false;


        [Tooltip("If enabled, skips stat adjustment on all unique items, namely the ones marked with the tag to \"Disallow Enchanting\".")]
        public bool SkipUniques { get; set; } = false;


        [Tooltip("If enabled, tempering is made about 40% less effective across the board for balancing reasons, making way for the artifacts to shine without Smithing being mandatory.")]
        public bool TemperingDebuff { get; set; } = true;
    }
}