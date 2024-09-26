// YApi QuickType插件生成，具体参考文档:https://plugins.jetbrains.com/plugin/18847-yapi-quicktype/documentation

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SBCustomClasses.TSH.Base
{
    public partial class BaseGameInfo
    {
        [JsonProperty("smashgg_game_id")] public long SmashggGameId { get; set; }

        [JsonProperty("character_to_codename")]
        public Dictionary<string, CharacterBaseInfo> CharacterToCodename { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("version")] public string Version { get; set; }

        [JsonProperty("challonge_game_id")] public long ChallongeGameId { get; set; }

        public string GetCodename(string characterName)
        {
            var result = "";

            if (CharacterToCodename.TryGetValue(characterName, out var value)) result = value.Codename;

            return result;
        }
    }

    public partial class CharacterBaseInfo
    {
        [JsonProperty("smashgg_name")] public string SmashggName { get; set; }

        [JsonProperty("codename")] public string Codename { get; set; }

        [JsonProperty("locale")] public Dictionary<string, string> LocaleNames { get; set; }

        [JsonProperty("skin_name", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, SkinName> SkinName { get; set; }
    }

    public partial class SkinName
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("locale")] public Dictionary<string, string> Locale { get; set; }
    }

    public partial class BaseGameInfo
    {
        public static BaseGameInfo FromJson(string json)
        {
            return JsonConvert.DeserializeObject<BaseGameInfo>(json, Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this BaseGameInfo self)
        {
            return JsonConvert.SerializeObject(self, Converter.Settings);
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