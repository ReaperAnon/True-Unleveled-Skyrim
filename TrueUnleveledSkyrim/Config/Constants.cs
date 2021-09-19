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

        // List variant postfixes.
        public static string PostfixPart { get; } = "_TUS_";
        public static string WeakPostfix { get; } = "_TUS_Weak";
        public static string StrongPostfix { get; } = "_TUS_Strong";

        // Json config paths.
        public static string ArtifactKeysPath { get; } = "Data/" + PatcherName + "/artifactKeys.json";
        public static string FollowersPath { get; } = "Data/" + PatcherName + "/customFollowers.json";
        public static string ExcludedNPCsPath { get; } = "Data/" + PatcherName + "/excludedNPCs.json";
        public static string ExcludedPerksPath { get; } = "Data/" + PatcherName + "/excludedPerks.json";
        public static string NPCEDIDPath { get; } = "Data/" + PatcherName + "/NPCsByEDID.json";
        public static string NPCFactionPath { get; } = "Data/" + PatcherName + "/NPCsByFaction.json";
        public static string RaceModifiersPath { get; } = "Data/" + PatcherName + "/raceLevelModifiers.json";
        public static string ZoneTyesEDIDPath { get; } = "Data/" + PatcherName + "/zoneTypesByEDID.json";
        public static string ZoneTyesEDIDMLUPath { get; } = "Data/" + PatcherName + "/zoneTypesByEDIDMLU.json";
        public static string ZoneTyesKeywordPath { get; } = "Data/" + PatcherName + "/zoneTypesByKeyword.json";
        public static string ZoneTyesKeywordMLUPath { get; } = "Data/" + PatcherName + "/zoneTypesByKeywordMLU.json";
    }
}
