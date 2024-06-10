// YApi QuickType插件生成，具体参考文档:https://plugins.jetbrains.com/plugin/18847-yapi-quicktype/documentation

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SBCustomClasses.TSH.CurrentMatch
{
    [Serializable]
    public partial class MatchData
    {
        [JsonProperty("strikedBy")]
        public List<List<object>> StrikedBy { get; set; } = new List<List<object>>();

        [JsonProperty("ruleset")]
        public List<string> Ruleset { get; set; } = new List<string>();

        [JsonProperty("team2score")]
        public long Team2Score { get; set; }

        [JsonProperty("has_selection_data")]
        public bool HasSelectionData { get; set; }

        [JsonProperty("isOnline")]
        public bool IsOnline { get; set; }

        [JsonProperty("isPools")]
        public bool IsPools { get; set; }

        [JsonProperty("bracket_type")]
        public string BracketType { get; set; } = "";

        [JsonProperty("p1_name")]
        public string P1Name { get; set; } = "";

        [JsonProperty("currPlayer")]
        public long CurrPlayer { get; set; }

        [JsonProperty("p2_name")]
        public string P2Name { get; set; } = "";

        [JsonProperty("top_n")]
        public long TopN { get; set; }

        [JsonProperty("stream")]
        public string Stream { get; set; } = "";

        [JsonProperty("stage_strike")]
        public List<string> StageStrike { get; set; } = new List<string>();

        [JsonProperty("round_name")]
        public string RoundName { get; set; } = "";

        [JsonProperty("team2losers")]
        public bool Team2Losers { get; set; }

        [JsonProperty("station")]
        public string Station { get; set; } = "";

        [JsonProperty("entrants")]
        public List<List<Entrant>> Entrants { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("tournament_phase")]
        public string TournamentPhase { get; set; } = "";

        [JsonProperty("team1losers")]
        public bool Team1Losers { get; set; }

        [JsonProperty("team1score")]
        public long Team1Score { get; set; }
    }
    [Serializable]
    public partial class Entrant
    {
        [JsonProperty("gamerTag")]
        public string GamerTag { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; } = "";

        [JsonProperty("twitter")]
        public string Twitter { get; set; }  = "";

        [JsonProperty("seed")]
        public long Seed { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; } = "";

        [JsonProperty("startggMain")]
        public long StartggMain { get; set; }

        [JsonProperty("mains")]
        public List<List<string>> Mains { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; } = "";

        [JsonProperty("id")]
        public List<object> Id { get; set; } = new List<object>();
    }

    public partial class MatchData
    {
        public static MatchData FromJson(string json) => JsonConvert.DeserializeObject<MatchData>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this MatchData self) => JsonConvert.SerializeObject((object)self, (JsonSerializerSettings)Converter.Settings);
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
            },
        };
    }
}
