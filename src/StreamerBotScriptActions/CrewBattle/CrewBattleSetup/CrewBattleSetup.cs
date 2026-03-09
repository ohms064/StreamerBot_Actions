using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.CrewBattle.CrewBattleSetup;

using System.Collections.Generic;
using System.Linq;

public class CPHInline : CPHInlineBase
{
    private const string LeftTeam = "teamLeft";
    private const string RightTeam = "teamRight";
    private const string LeftTeamName = "leftTeamName";
    private const string RightTeamName = "rightTeamName";
    public bool Execute()
    {
        CPH.ClearUsersFromGroup(LeftTeam);
        CPH.ClearUsersFromGroup(RightTeam);
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var teamNames = rawInput.Split(',');

        if (teamNames.Length < 2)
        {
            return false;
        }
        
        CPH.SetGlobalVar(LeftTeamName, teamNames[0]);
        CPH.SetGlobalVar(RightTeamName, teamNames[1]);
        
        return true;
    }
}