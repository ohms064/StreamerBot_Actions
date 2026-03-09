using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.FirstTo.MatchUpdater;

using System.Collections.Generic;
using SBCustomClasses.StreamDeck;
// ReSharper disable once InconsistentNaming
public class CPHInline : CPHInlineBase
{
    private StreamDeckTSHConnection TshConnection;
    public bool Execute()
    {
        TshConnection = StreamDeckTSHConnection.Get(CPH);
        
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var teams = new List<TeamData>(eventUsers.Count);
        if (eventUsers.Count != 2)
        {
            return false;
        }
        foreach (var t in eventUsers)
        {
            var userRecurringCharacters = CPH.GetTwitchUserVarById<List<string>>(t.Id, "userRecurringCharacters_Smash");
            var teamMemberData = new TeamMemberData(t.Id, userRecurringCharacters);
            var teamData = new TeamData(teamMemberData, squadGroupName);
            teams.Add(teamData);
        }

        var firstTo = CPH.GetGlobalVar<int>("First To");
        TshConnection.InitConnection(CPH, teams[0], teams[1], firstTo);
        return true;
    }
}