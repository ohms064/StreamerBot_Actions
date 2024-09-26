// YApi QuickType插件生成，具体参考文档:https://plugins.jetbrains.com/plugin/18847-yapi-quicktype/documentation

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SBCustomClasses.TSH.MatchList
{
    [Serializable]
    public partial class MatchListData
    {
        [JsonProperty("p2_name")] public string P2Name { get; set; } = "";

        [JsonProperty("stream")] public string Stream { get; set; } = "";

        [JsonProperty("round_name")] public string RoundName { get; set; } = "";

        [JsonProperty("station")] public long Station { get; set; }

        [JsonProperty("entrants")] public List<List<Entrant>> Entrants { get; set; } = new List<List<Entrant>>();

        [JsonProperty("isOnline")] public bool IsOnline { get; set; }

        [JsonProperty("isPools")] public bool IsPools { get; set; }

        [JsonProperty("id")] public long Id { get; set; }

        [JsonProperty("tournament_phase")] public string TournamentPhase { get; set; } = "";

        [JsonProperty("bracket_type")] public string BracketType { get; set; } = "";

        [JsonProperty("p1_name")] public string P1Name { get; set; } = "";
    }

    [Serializable]
    public partial class Entrant
    {
        [JsonProperty("gamerTag")] public string GamerTag { get; set; } = "";

        [JsonProperty("prefix")] public string Prefix { get; set; } = "";

        [JsonProperty("id")] public List<long?> Id { get; set; }
    }

    public partial class MatchListData
    {
        public static MatchListData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MatchListData>(json, Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this MatchListData self)
        {
            return JsonConvert.SerializeObject(self, (JsonSerializerSettings)Converter.Settings);
        }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            }
        };
    }
}