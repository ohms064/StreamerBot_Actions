using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace SBCustomClasses.TSH.ScoreboardValue
{
    public class ScoreboardValue<T>
    {
        [JsonProperty("scoreboardNumber")] public int ScoreboardNumber { get; set; }
        [JsonProperty("team")] public int Team { get; set; }
        [JsonProperty("player")] public int Player { get; set; }
        [JsonProperty("has_selection_data")] public bool HasSelectionData { get; set; }
        [JsonProperty("overwrite")] public bool Overwrite { get; set; }
        [JsonProperty] public T Data { get; set; }
    }

    public class ScoreboardValueConverter<T> : JsonConverter<ScoreboardValue<T>>
    {
        public override void WriteJson(JsonWriter writer, ScoreboardValue<T> value, JsonSerializer serializer)
        {
            JObject result = new JObject();
            result.Add("scoreboardNumber", value.ScoreboardNumber);
            result.Add("team", value.Team);
            result.Add("player", value.Player);
            result.Add("has_selection_data", value.HasSelectionData);
            result.Add("overwrite", value.Overwrite);

            var data = value.Data;
            var t = data.GetType();
            var properties = t.GetProperties();
            foreach(var property in properties)
            {
                var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                var propertyName = jsonAttribute != null ? jsonAttribute.PropertyName : property.Name;
                var propertyValue = property.GetValue(data);
                result.Add(propertyName, propertyValue.ToString());
            }
            
            result.WriteTo(writer);
        }

        public override ScoreboardValue<T> ReadJson(JsonReader reader, Type objectType, ScoreboardValue<T> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            // I don't think we're using this
            throw new NotImplementedException();
        }
    }
}