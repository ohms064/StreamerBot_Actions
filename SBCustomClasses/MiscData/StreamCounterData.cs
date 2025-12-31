using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SBCustomClasses.MiscData
{
    [System.Serializable]
    public class StreamCounterData
    {
        [JsonProperty] 
        public int GlobalCounter { get; private set; } = 0;
        [JsonProperty]
        public int StreamCounter { get; private set; } = 0;
        [JsonProperty]
        public string CurrentCounterId { get; private set; } = "";

        [JsonProperty]
        public Dictionary<string, int> SessionCounters { get; private set; } = new Dictionary<string, int>();

        public void AddCounters(int counter = 1)
        {
            GlobalCounter += counter;
            StreamCounter += counter;
            if (string.IsNullOrEmpty(CurrentCounterId)) return;
            
            if (!SessionCounters.ContainsKey(CurrentCounterId))
            {
                SessionCounters.Add(CurrentCounterId, 0);
            }
            SessionCounters[CurrentCounterId] += counter;
        }

        public void UpdateSpecificSession(string counterId, int counter = 1)
        {
            if (!SessionCounters.ContainsKey(counterId))
            {
                SessionCounters.Add(counterId, 0);
            }
            SessionCounters[CurrentCounterId] += counter;
        }

        public void SetCounterId(string counterId)
        {
            CurrentCounterId = counterId;
            if (!SessionCounters.ContainsKey(CurrentCounterId))
            {
                SessionCounters.Add(CurrentCounterId, 0);
            }
        }

        public void ResetStreamCounter()
        {
            StreamCounter = 0;
        }
    
        public static StreamCounterData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<StreamCounterData>(json, Converter.Settings);
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