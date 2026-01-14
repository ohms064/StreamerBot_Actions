using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SmashGeneral.StreamDeckGeneral;

using System.Collections.Generic;
using SBCustomClasses.StreamDeck;
using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;

public class CPHInline : CPHInlineBase
{
    private BaseGameInfo _smashGameInfo;
    private StreamDeckConfiguration _streamDeckConfiguration;
    // Move to PathManager
    private const string SmashConfigurationFilePath =
        "D:/Streams/TournamentStreamHelper/user_data/games/ssbu/base_files/config.json";
    public bool Execute()
    {
        {
            //TODO: Fix this hardcoded path. Maybe add it to the path manager, same with SmashRandom.cs
            var json = File.ReadAllText(
                "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json");
            _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(json);
        }
        var smashFileContent = File.ReadAllText(SmashConfigurationFilePath);
        _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(smashFileContent);
        return CleanButtons();
    }

    public bool CleanButtons()
    {
        foreach(var button in _streamDeckConfiguration.Buttons.AllButtons)
        {
            CPH.StreamDeckSetBackgroundColor(button, _streamDeckConfiguration.Theming.UnselectedColor);
            CPH.StreamDeckSetTitle(button, "");
        }
        
        CPH.SetGlobalVar("streamDeckCharacterButtons", new Dictionary<string, StreamDeckSmashCharacterButtonState>() );
        CPH.SetGlobalVar("streamDeckPlayerButtons", new Dictionary<string, StreamDeckSmashCharacterButtonState>());
        CPH.SetGlobalVar("currentGameMode", "");
        
        return true;
    }
}