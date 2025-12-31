using Streamer.bot.Plugin.Interface;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SBCustomClasses.MiscData;

namespace StreamerBotScriptActions.StreamGeneral.StreamCounter;

public class CPHInline : CPHInlineBase
{
    private string _gameName;
    private StreamCounterData _counterData;
    private const string CounterKey = "Counters";

    public bool Execute()
    {
        return ResetGame();
    }

    public bool ResetGame()
    {
        SaveCounterData();
        var success = CPH.TryGetArg<string>("gameBoxArt", out var gameBoxArt);
        success &= CPH.TryGetArg("gameName", out _gameName) || CPH.TryGetArg("game", out _gameName);
        UpdateCounterData();
        if (!success) return false;
        
        var streamDeckButton = CPH.GetGlobalVar<string>("CountersStreamDeckButton");
        Task.Run(async () =>
        {
            CPH.StreamDeckSetBackgroundUrl(streamDeckButton, gameBoxArt);
            await Task.Delay(1000);
            UpdateStreamDeck();
                
        });

        return true;
    }

    public bool ReloadGameCounters()
    {
        UpdateCounterData();
        return true;
    }

    public bool UpdateCounter()
    {
        _counterData.AddCounters();
        UpdateStreamDeck();
        return true;
    }
    
    
    // ReSharper disable once MemberCanBePrivate.Global
    public bool SaveCounterData()
    {
        if (string.IsNullOrEmpty(_gameName) || _counterData == null)
        {
            return false;
        }
        CPH.SetGlobalVar($"{CounterKey}-{_gameName}", JsonConvert.SerializeObject(_counterData));
        return true;
    }

    public bool SetSession()
    {
        if (!CPH.TryGetArg("sessionId", out string sessionId))
        {
            return false;
        }
        _counterData.SetCounterId(sessionId);
        UpdateStreamDeck();
        return true;
    }
    
    public bool UpdateSessionCounter()
    {
        if (!CPH.TryGetArg("sessionId", out string sessionId))
        {
            return false;
        }
        _counterData.UpdateSpecificSession(sessionId);
        UpdateStreamDeck();
        return true;
    }

    public bool ResetStreamCounter()
    {
        _counterData.ResetStreamCounter();
        UpdateStreamDeck();
        return true;
    }

    public bool ExposeValues()
    {
        if (_counterData == null)
        {
            return false;
        }
        CPH.SetArgument("globalCount", _counterData.GlobalCounter.ToString());
        CPH.SetArgument("streamCount", _counterData.StreamCounter.ToString());
        CPH.SetArgument("sessionCount", _counterData.SessionCounters.TryGetValue(_counterData.CurrentCounterId, 
            out var counter) ? counter.ToString() : "");
        CPH.SetArgument("sessionName", _counterData.CurrentCounterId.ToString());
        return true;
    }

    private void UpdateCounterData()
    {
        var counterJson = CPH.GetGlobalVar<string>($"{CounterKey}-{_gameName}") ?? "";
        _counterData = StreamCounterData.FromJson(counterJson) ?? new StreamCounterData();
    }

    private string FormatCounterData(StreamCounterData counterData)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"Gl: {counterData.GlobalCounter}\n");
        builder.Append($"Str: {counterData.StreamCounter}\n");
        if (!counterData.SessionCounters.ContainsKey(counterData.CurrentCounterId)) return builder.ToString();
        
        builder.Append($"{counterData.CurrentCounterId}:\n{counterData.SessionCounters[counterData.CurrentCounterId]}");
        
        return builder.ToString();
    }

    private void UpdateStreamDeck()
    {
        var streamDeckButton = CPH.GetGlobalVar<string>("CountersStreamDeckButton");
        var streamDeckTitle = FormatCounterData(_counterData);
        CPH.StreamDeckSetTitle(streamDeckButton, streamDeckTitle);
    }
}