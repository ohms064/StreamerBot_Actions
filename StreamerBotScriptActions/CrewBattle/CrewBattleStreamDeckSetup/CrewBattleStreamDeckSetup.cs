using System.Collections.Generic;
using System.Linq;
using SBCustomClasses.StreamDeck;
using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.CrewBattle.CrewBattleStreamDeckSetup;

using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;

public class CPHInline : CPHInlineBase
{
    private StreamDeckConfiguration _streamDeckConfiguration;
    private const string CrewBattleTeamNameRight = "Crew Battle Team Left";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Right";

    public bool Execute()
    {
        //if(_streamDeckConfiguration == null)
        {
            var json = File.ReadAllText(
                "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json");
            _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(json);
        }
        return UpdateStreamDeck();
    }

    public bool SetupStreamDeckButtonData()
    {
        var buttonCharacterDict = CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons");
        if (buttonCharacterDict == null)
        {
            buttonCharacterDict = new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            CPH.LogInfo("Dict was null");
        }
        
        
        return true;
    }

    public bool UpdateStreamDeck()
    {
        var teamNames = new List<string>
        {
            CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft),
            CPH.GetGlobalVar<string>(CrewBattleTeamNameRight)
        };
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.LeftButtonId, teamNames[0]);
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.RightButtonId, teamNames[1]);

        var teamRosters = new List<IEnumerable<string>>
        {
            CPH.UsersInGroup(CrewBattleTeamNameLeft).Select(u => u.Id),
            CPH.UsersInGroup(CrewBattleTeamNameRight).Select(u => u.Id),
        };

        var streamDeckPlayerButtons = new List<List<string>>
        {
            _streamDeckConfiguration.Buttons.LeftCharacterButtons,
            _streamDeckConfiguration.Buttons.RightCharacterButtons
        };

        for (var index = 0; index < teamRosters.Count; index++)
        {
            var roster = teamRosters[index];
            var currentStreamDeckButtons = streamDeckPlayerButtons[index];
            var streamDeckIndex = 0;
            foreach (var playerId in roster)
            {
                var twitchUser = CPH.TwitchGetExtendedUserInfoById(playerId);
                CPH.StreamDeckSetBackgroundUrl(currentStreamDeckButtons[streamDeckIndex], twitchUser.ProfileImageUrl);
                streamDeckIndex++;
            }
        }

        return true;
    }
}