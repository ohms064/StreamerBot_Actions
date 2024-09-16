using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace SBCustomClasses.StreamDeck
{
    [Serializable]
    public enum SelectionState
    {
        Unselected,
        Selected,
        Disabled // When selected but selected another button
    }

    [Serializable]
    public partial class StreamDeckSmashCharacterButtonState
    {
        [JsonProperty("character")] public string Character { get; set; } = "";

        [JsonProperty("selected_state")] public SelectionState SelectedState { get; set; } = SelectionState.Unselected;

        [JsonProperty("user_id")] public string UserId { get; set; } = "";

        [JsonProperty("button_id")] public string ButtonId { get; set; } = "";

        [JsonProperty("color_state")] public string ColorState { get; set; } = "";

        public bool CharacterSet => Character != "";
    }

    public partial class StreamDeckSmashCharacterButtonState
    {
        public static StreamDeckSmashCharacterButtonState FromJson(string json)
        {
            return JsonConvert.DeserializeObject<StreamDeckSmashCharacterButtonState>(json, Converter.Settings);
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