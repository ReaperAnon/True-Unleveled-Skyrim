using System.IO;

using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;


namespace TrueUnleveledSkyrim.Config
{
    public static class TUSConstants
    {
        // List variant postfixes.
        public static string PostfixPart { get; } = "_TUS_";
        public static string WeakPostfix { get; } = "_TUS_Weak";
        public static string StrongPostfix { get; } = "_TUS_Strong";

        // Json config paths.
        public static string ArtifactKeysPath { get; set; } = "artifactKeys.json";
        public static string FollowersPath { get; set; } = "customFollowers.json";
        public static string ExcludedNPCsPath { get; set; } = "excludedNPCs.json";
        public static string ExcludedPerksPath { get; set; } = "excludedPerks.json";
        public static string NPCEDIDPath { get; set; } = "NPCsByEDID.json";
        public static string NPCFactionPath { get; set; } = "NPCsByFaction.json";
        public static string RaceModifiersPath { get; set; } = "raceLevelModifiers.json";
        public static string ZoneTyesEDIDPath { get; set; } = "zoneTypesByEDID.json";
        public static string ZoneTyesEDIDMLUPath { get; set; } = "zoneTypesByEDIDMLU.json";
        public static string ZoneTyesKeywordPath { get; set; } = "zoneTypesByKeyword.json";
        public static string ZoneTyesKeywordMLUPath { get; set; } = "zoneTypesByKeywordMLU.json";

        public static void GetPaths(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            ArtifactKeysPath = Path.Combine(state.ExtraSettingsDataPath, ArtifactKeysPath);
            FollowersPath = Path.Combine(state.ExtraSettingsDataPath, FollowersPath);
            ExcludedNPCsPath = Path.Combine(state.ExtraSettingsDataPath, ExcludedNPCsPath);
            ExcludedPerksPath = Path.Combine(state.ExtraSettingsDataPath, ExcludedPerksPath);
            NPCEDIDPath = Path.Combine(state.ExtraSettingsDataPath, NPCEDIDPath);
            NPCFactionPath = Path.Combine(state.ExtraSettingsDataPath, NPCFactionPath);
            RaceModifiersPath = Path.Combine(state.ExtraSettingsDataPath, RaceModifiersPath);
            ZoneTyesEDIDPath = Path.Combine(state.ExtraSettingsDataPath, ZoneTyesEDIDPath);
            ZoneTyesEDIDMLUPath = Path.Combine(state.ExtraSettingsDataPath, ZoneTyesEDIDMLUPath);
            ZoneTyesKeywordPath = Path.Combine(state.ExtraSettingsDataPath, ZoneTyesKeywordPath);
            ZoneTyesKeywordMLUPath = Path.Combine(state.ExtraSettingsDataPath, ZoneTyesKeywordMLUPath);
        }

    }
}
