using System;
using System.Linq;
using Streamer.bot.Plugin.Interface;
using Newtonsoft.Json;
using SBCustomClasses.MiscData;

namespace StreamerBotScriptActions.StreamGeneral.StreamCounter;

public class CPHInline : CPHInlineBase
{
    private const string CounterKey = "Counters";

    public bool ReloadGameCounters()
    {
        if (!CPH.TryGetArg("sdButtonId", out string sdButtonId))
        {
            return false;
        }
        GetCounterData(sdButtonId);
        return true;
    }

    public bool UpdateCounter()
    {
        if (!CPH.TryGetArg("sdButtonId", out string sdButtonId))
        {
            return false;
        }
        if (!CPH.TryGetArg("category", out string category))
        {
            category = sdButtonId;
        }

        var counterData = GetCounterData(category);
        counterData.AddCounters(counterData.CurrentSession);
        SaveCounterData(counterData, category);
        CPH.LogDebug($"{counterData.GlobalCounter} {counterData.StreamCounter}");
        UpdateStreamDeck(sdButtonId ,counterData);
        return true;
    }
    
    
    // ReSharper disable once MemberCanBePrivate.Global
    public bool SaveCounterData(StreamCounterData counterData, string category)
    {
        var gameName = CPH.GetGlobalVar<string>("TwitchLastGame");
        if (string.IsNullOrEmpty(gameName))
        {
            return false;
        }

        CPH.SetGlobalVar(GetCounterKey(category), JsonConvert.SerializeObject(counterData));
        return true;
    }

    public bool SetSession()
    {
        if (!CPH.TryGetArg("sdButtonId", out string sdButtonId))
        {
            return false;
        }
        if (!CPH.TryGetArg("sessionId", out string sessionId))
        {
            return false;
        }
        if (!CPH.TryGetArg("category", out string category))
        {
            category = sdButtonId;
        }

        var counterData = GetCounterData(category);
        counterData.CurrentSession = sessionId;
        SaveCounterData(counterData, category);
        UpdateStreamDeck(sdButtonId, counterData);
        return true;
    }
    
    public bool UpdateSessionCounter()
    {
        if (!CPH.TryGetArg("sessionId", out string sessionId))
        {
            return false;
        }
        if (!CPH.TryGetArg("sdButtonId", out string sdButtonId))
        {
            return false;
        }
        
        if (!CPH.TryGetArg("category", out string category))
        {
            category = sdButtonId;
        }

        var counterData = GetCounterData(category);
        counterData.UpdateSpecificSession(sessionId);
        SaveCounterData(counterData, category);
        UpdateStreamDeck(sdButtonId, counterData);
        return true;
    }

    public bool ResetStreamCounter()
    {
        var globals = CPH.GetGlobalVarValues();

        foreach (var globalVariable in globals.Where(x => x.VariableName.StartsWith(CounterKey)))
        {
            
            var data = StreamCounterData.FromJson(globalVariable.Value.ToString()); 
            if(data == null) 
                continue;
            data.StreamCounter = 0;
            var category = globalVariable.VariableName.Substring(globalVariable.VariableName.LastIndexOf("->", StringComparison.Ordinal) + 2);
            SaveCounterData(data, category);
        }
            
            
        return true;
    }

    public bool ExposeValues()
    {
        if (!CPH.TryGetArg("sdButtonId", out string sdButtonId))
        {
            return false;
        }
        if (!CPH.TryGetArg("category", out string category))
        {
            category = sdButtonId;
        }

        var counterData = GetCounterData(category);
        CPH.SetArgument("globalCount", counterData.GlobalCounter.ToString());
        CPH.SetArgument("streamCount", counterData.StreamCounter.ToString());
        CPH.SetArgument("sessionCount", counterData.CurrentSessionCounter.ToString());
        CPH.SetArgument("sessionName", counterData.CurrentSession);
        return true;
    }

    private StreamCounterData GetCounterData(string category)
    {
        var counterJson = CPH.GetGlobalVar<string>(GetCounterKey(category)) ?? "";
        return StreamCounterData.FromJson(counterJson) ?? new StreamCounterData();
    }

    private string GetCounterKey(string category)
    {
        var gameName = CPH.GetGlobalVar<string>("TwitchLastGame");
        return string.IsNullOrEmpty(gameName) ? "" : $"{CounterKey}->{gameName}->{category}";
    }

    private void UpdateStreamDeck(string sdButtonId, StreamCounterData counterData)
    {
        CPH.StreamDeckSetTitle(sdButtonId, counterData.ToFormatedString());
    }
}