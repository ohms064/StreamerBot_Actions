using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.StreamDeckButtonsUpdate;

using System.Linq;
using System.Collections.Generic;
using SBCustomClasses.StreamDeck;

public class CPHInline : CPHInlineBase
{
    private StreamDeckTSHConnection _streamDeckTSHConnection;

    public bool Execute()
    {
        _streamDeckTSHConnection = StreamDeckTSHConnection.Get(CPH);
        
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var teams = new List<TeamData>(eventUsers.Count);
        foreach (var t in eventUsers)
        {
            var originalSquadRoster = CPH.GetTwitchUserVarById<List<string>>(t.Id, "originalSquadRoster");
            var teamMemberData = new TeamMemberData(t.Id, originalSquadRoster);
            var teamData = new TeamData(teamMemberData, squadGroupName);
            teams.Add(teamData);
        }

        if (teams[0].TeamMembers.First().Characters.Count() != teams[1].TeamMembers.First().Characters.Count())
        {
            return false;
        }

        var stocks = teams[0].TeamMembers.First().Characters.Count() * 3;
        _streamDeckTSHConnection.InitConnection(CPH, teams[0], teams[1], stocks);
        return true;
    }

    public bool UpdatePlayerStocks()
    {
        if (!CPH.TryGetArg("value2Add", out long value2AddLong))
        {
            CPH.LogDebug("value2Add was not set");
            return false;
        }

        if (!CPH.TryGetArg("userIndex", out long userIndexLong))
        {
            CPH.LogDebug("userIndex was not set");
            return false;
        }

        var value2Add = (int)value2AddLong;
        var userIndex = (int)userIndexLong;
        _streamDeckTSHConnection.UpdateStocks(CPH, userIndex == 0, value2Add);
        return true;
    }

    public bool CharacterPortraitPress()
    {
        CPH.LogInfo("Portrait pressed");
        if (!CPH.TryGetArg("sdButtonId", out string buttonId))
        {
            CPH.LogInfo("WTF button not found");
            return false;
        }
        
        _streamDeckTSHConnection.SelectCharacter(CPH, buttonId);
        return true;
    }

    public bool CharacterPortraitPressToggle()
    {
        if (!CPH.TryGetArg("sdButtonId", out string buttonId))
        {
            CPH.LogInfo("WTF button not found");
            return false;
        }
        
        _streamDeckTSHConnection.ToggleCharacterState(CPH, buttonId);
        return true;
    }

    public bool ResetStreamDeckButtons()
    {
        _streamDeckTSHConnection.ResetStreamDeckButtons(CPH);
        return true;
    }
}