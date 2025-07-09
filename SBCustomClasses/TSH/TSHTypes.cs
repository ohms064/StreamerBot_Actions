using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SBCustomClasses.TSH
{
    [Serializable]
    public class ScoreRequest
    {
        [JsonProperty("team1score")] public int Team1Score;  
        
        [JsonProperty("team2score")] public int Team2Score;   
    }
    
    [Serializable]
    public class EntrantRequest
    {
        [JsonProperty("scoreboardNumber")] public int ScoreboardNumber { get; set; }

        [JsonProperty("team")] public int Team { get; set; } = 1;

        [JsonProperty("player")] public int Player { get; set; } = 1;

        [JsonProperty("has_selection_data")] public bool HasSelectionData { get; set; } = true;
        
        [JsonProperty("overwrite")] public bool Overwrite { get; set; }
        
        [JsonProperty("gamerTag")] public string GamerTag { get; set; }

        [JsonProperty("country_code")] public string CountryCode { get; set; } = "";

        [JsonProperty("twitter")] public string Twitter { get; set; } = "";

        [JsonProperty("seed")] public long Seed { get; set; }

        [JsonProperty("prefix")] public string Prefix { get; set; } = "";
 
        [JsonProperty("mains")] public Dictionary<string, List<List<string>>> Mains { get; set; }

        [JsonProperty("avatar")] public string Avatar { get; set; } = "";
    }
}