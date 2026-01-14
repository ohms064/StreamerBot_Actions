using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SBCustomClasses.MiscData
{
    [System.Serializable]
    public class StreamCounterData
    {
        public const string StreamKey = "Stream";
        public const string GlobalKey = "Global";
        [JsonProperty] 
        public Dictionary<string, int> Counters = new Dictionary<string, int>();

        [JsonProperty]
        public int GlobalCounter { get; set; }
    
        [JsonProperty]
        public int StreamCounter { get; set; }
        
        [JsonProperty]
        public string CurrentSession { get; set; }

        public int CurrentSessionCounter
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentSession))
                {
                    return 0;
                }
                return Counters.TryGetValue(CurrentSession, out var counter) ? counter : 0;
            }
        }

        public void AddCounters(string counterId = "", int counter = 1)
        {
            GlobalCounter += counter;
            StreamCounter += counter;
            if (string.IsNullOrEmpty(counterId)) return;
            
            UpdateSpecificSession(counterId, counter);
        }

        public void UpdateSpecificSession(string counterId, int counter = 1)
        {
            if (!Counters.ContainsKey(counterId))
            {
                Counters.Add(counterId, 0);
            }
            Counters[counterId] += counter;
        }
        
        public void ResetSpecificSession(string counterId)
        {
            if (!Counters.ContainsKey(counterId))
            {
                Counters.Add(counterId, 0);
            }
            Counters[counterId] = 0;
        }

        public void ResetStreamCounter()
        {
            StreamCounter = 0;
        }
        
        public void ResetGlobalCounter()
        {
            GlobalCounter = 0;
        }
    
        public static StreamCounterData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<StreamCounterData>(json, Converter.Settings);
        }

        public string ToFormatedString(string sessionId = "")
        {
            var builder = new StringBuilder();
            
            builder.AppendLine($"Gl: {GlobalCounter}");
            builder.AppendLine($"Str: {StreamCounter}");

            if (string.IsNullOrEmpty(sessionId))
            {
                return builder.ToString();
            }
            
            if (!Counters.ContainsKey(sessionId))
            {
                Counters.Add(sessionId, 0);
            }
            builder.AppendLine($"{sessionId}: {Counters[sessionId]}");
            return builder.ToString();
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