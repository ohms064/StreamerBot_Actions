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
    public bool Execute()
    {
        var pathManager = new PathManager(CPH);
        _streamDeckConfiguration = pathManager.GetStreamDeckConfiguration();
        _smashGameInfo = pathManager.GetBaseGameInfo();
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