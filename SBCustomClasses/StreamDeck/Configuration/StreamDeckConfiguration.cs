// YApi QuickType插件生成，具体参考文档:https://plugins.jetbrains.com/plugin/18847-yapi-quicktype/documentation

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SBCustomClasses.StreamDeck.Configuration
{
    [Serializable]
    public partial class StreamDeckConfiguration
    {
        [JsonProperty("buttons")]
        public Buttons Buttons { get; set; }

        [JsonProperty("theming")]
        public Theming Theming { get; set; }
    }
    
    [Serializable]
    public partial class Buttons
    {
        [JsonProperty("left_stocks_button_id")]
        public string LeftStocksButtonId { get; set; }

        [JsonProperty("right_character_buttons")]
        public List<string> RightCharacterButtons { get; set; }
        
        [JsonProperty("right_character_portraits")]
        public List<string> RightPlayerPortraits { get; set; }
        
        [JsonProperty("left_character_portraits")]
        public List<string> LeftPlayerPortraits { get; set; }

        [JsonProperty("right_stocks_buttons_id")]
        public string RightStocksButtonId { get; set; }

        [JsonProperty("contexT_buttons_id")]
        public string ContextButtonId { get; set; }

        [JsonProperty("right_button_id")]
        public string RightButtonId { get; set; }

        [JsonProperty("left_character_buttons")]
        public List<string> LeftCharacterButtons { get; set; }

        [JsonProperty("left_button_id")]
        public string LeftButtonId { get; set; }

        public List<string> AllButtons
        {
            get
            {
                var result = new List<string>()
                {
                    LeftStocksButtonId,
                    RightStocksButtonId,
                    ContextButtonId,
                    RightButtonId,
                };
                
                result.AddRange(LeftCharacterButtons);
                result.AddRange(RightCharacterButtons);

                return result;
            }
        }
        
        public List<string> CharacterButtons
        {
            get
            {
                var result = new List<string>();
                
                result.AddRange(LeftCharacterButtons);
                result.AddRange(RightCharacterButtons);

                return result;
            }
        }
    }

    [Serializable]
    public partial class Theming
    {
        [JsonProperty("already_used_color")]
        public string AlreadyUsedColor { get; set; }

        [JsonProperty("smash_icons")]
        public Dictionary<string, CharacterIcons> SmashIcons { get; set; }

        [JsonProperty("unselected_color")]
        public string UnselectedColor { get; set; }

        [JsonProperty("selected_color")]
        public string SelectedColor { get; set; }

        public CharacterIcons GetButtonsIcons(string key)
        {
            if (!SmashIcons.TryGetValue(key, out var result))
            {
                return result;
            }

            return SmashIcons["character_stocks"];
        }
    }

    [Serializable]
    public partial class CharacterIcons
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("default")]
        public string Default { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        public string GetCompletePath(string characterCodename, int skin = 0)
        {
            // File existing and character codename verification should be handled externally
            const string characterReplacementString = "<char_codename>";
            const string skinReplacementString = "<skin_index>";
            return $"{Path}/{Filename.Replace(characterReplacementString, characterCodename).Replace(skinReplacementString, skin.ToString())}";
        }

        public string DefaultPath => $"{Path}/{Default}";
    }

    public partial class StreamDeckConfiguration
    {
        public static StreamDeckConfiguration FromJson(string json) => JsonConvert.DeserializeObject<StreamDeckConfiguration>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this StreamDeckConfiguration self) => JsonConvert.SerializeObject(self, Converter.Settings);
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
