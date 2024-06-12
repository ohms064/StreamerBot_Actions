using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.DebugAux.StreamDeckButtonIdentifier;

using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;

public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        var json = File.ReadAllText(
            "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json");
        var streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(json);

        var NumberCharacterButtons = (List<string> buttons, string descriptor) =>
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                CPH.StreamDeckSetTitle(buttons[i], $"{descriptor}\n{i}");
            }
        };

        NumberCharacterButtons(streamDeckConfiguration.Buttons.LeftCharacterButtons, "Left");
        NumberCharacterButtons(streamDeckConfiguration.Buttons.RightCharacterButtons, "Right");
        
        CPH.StreamDeckSetTitle(streamDeckConfiguration.Buttons.LeftButtonId, "User\nLeft");
        CPH.StreamDeckSetTitle(streamDeckConfiguration.Buttons.RightButtonId, "User\nRight");
        CPH.StreamDeckSetTitle(streamDeckConfiguration.Buttons.LeftStocksButtonId, "Stocks\nLeft");
        CPH.StreamDeckSetTitle(streamDeckConfiguration.Buttons.RightStocksButtonId, "Stocks\nRight");
        
        return true;
    }
}