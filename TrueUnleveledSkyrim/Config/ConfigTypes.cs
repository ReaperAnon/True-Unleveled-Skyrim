using System.Collections.Generic;

using Newtonsoft.Json;


namespace TrueUnleveledSkyrim.Config
{
    public abstract class ConfigType {}

    // artifactKeys.json
    public class ArtifactKeys : ConfigType
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
    }

    // customFollowers.json
    public class FollowerEntry
    {
        [JsonProperty] public string Key { get; set; } = string.Empty;
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
    }

    public class FollowerList : ConfigType
    {
        [JsonProperty] public List<FollowerEntry> Followers { get; set; } = new();
    }

    // excludedNPCs.json
    public class ExcludedNPCs : ConfigType
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
    }

    // excludedPerks.json
    public class ExcludedPerks : ConfigType
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
    }

    // NPCsByEDID.json
    public class NPCEDIDEntry
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
        [JsonProperty] public short Level { get; set; }
    }

    public class NPCEDIDs : ConfigType
    {
        [JsonProperty] public List<NPCEDIDEntry> NPCs { get; set; } = new();
    }

    // NPCsByFaction.json
    public class NPCFactionEntry
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
        [JsonProperty] public short? MinLevel { get; set; }
        [JsonProperty] public short? MaxLevel { get; set; }
        [JsonProperty] public short? Level { get; set; }
    }

    public class NPCFactions : ConfigType
    {
        [JsonProperty] public List<NPCFactionEntry> NPCs { get; set; } = new();
    }

    // raceLevelModifiers.json
    public class RaceEntries
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
        [JsonProperty] public short? LevelModifierAdd { get; set; }
        [JsonProperty] public float? LevelModifierMult { get; set; }
    }

    public class RaceModifiers : ConfigType
    {
        [JsonProperty] public List<RaceEntries> Data { get; set; } = new();
    }

    // zoneTypesBy****.json
    public class ZoneEntry
    {
        [JsonProperty] public List<string> Keys { get; set; } = new();
        [JsonProperty] public List<string> ForbiddenKeys { get; set; } = new();
        [JsonProperty] public short MinLevel { get; set; }
        [JsonProperty] public short MaxLevel { get; set; }
        [JsonProperty] public short Range { get; set; }
        [JsonProperty] public bool? EnableCombatBoundary { get; set; }
    }

    public class ZoneList : ConfigType
    {
        [JsonProperty] public List<ZoneEntry> Zones { get; set; } = new();
    }
}
