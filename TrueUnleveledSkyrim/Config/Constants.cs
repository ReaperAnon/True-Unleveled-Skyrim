using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueUnleveledSkyrim.Config
{
    public static class TUSConstants
    {
        // GitHub name of the patcher for the Data folder reference.
        public static string PatcherName { get; } = "True Unleveled Skyrim";
        public static string DataPath { get; } = "Data/Skyrim Special Edition/";

        // List variant postfixes.
        public static string PostfixPart { get; } = "_TUS_";
        public static string WeakPostfix { get; } = "_TUS_Weak";
        public static string StrongPostfix { get; } = "_TUS_Strong";

        // Json config paths.
        public static string ArtifactKeysPath { get; } = DataPath + PatcherName + "/artifactKeys.json";
        public static string FollowersPath { get; } = DataPath + PatcherName + "/customFollowers.json";
        public static string ExcludedNPCsPath { get; } = DataPath + PatcherName + "/excludedNPCs.json";
        public static string ExcludedPerksPath { get; } = DataPath + PatcherName + "/excludedPerks.json";
        public static string NPCEDIDPath { get; } = DataPath + PatcherName + "/NPCsByEDID.json";
        public static string NPCFactionPath { get; } = DataPath + PatcherName + "/NPCsByFaction.json";
        public static string RaceModifiersPath { get; } = DataPath + PatcherName + "/raceLevelModifiers.json";
        public static string ZoneTyesEDIDPath { get; } = DataPath + PatcherName + "/zoneTypesByEDID.json";
        public static string ZoneTyesEDIDMLUPath { get; } = DataPath + PatcherName + "/zoneTypesByEDIDMLU.json";
        public static string ZoneTyesKeywordPath { get; } = DataPath + PatcherName + "/zoneTypesByKeyword.json";
        public static string ZoneTyesKeywordMLUPath { get; } = DataPath + PatcherName + "/zoneTypesByKeywordMLU.json";
    }
}
