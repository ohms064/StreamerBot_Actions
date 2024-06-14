using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.CrewBattle.CrewBattleSetup;

using System.Collections.Generic;
using System.Linq;

public class CPHInline : CPHInlineBase
{
    private const string CrewBattleTeamNameRight = "Crew Battle Team Left";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Right";
    public bool Execute()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var teamNames = rawInput.Split(',');

        if (teamNames.Length < 2)
        {
            return false;
        }
        
        CPH.SetGlobalVar(CrewBattleTeamNameLeft, teamNames[0]);
        CPH.SetGlobalVar(CrewBattleTeamNameLeft, teamNames[1]);
        
        return true;
    }

    public List<string> GetLeftCrewBattleGroup()
    {
        return GetCrewBattleGroup(CrewBattleTeamNameLeft).ToList();
    }
    
    public List<string> GetRightCrewBattleGroup()
    {
        return GetCrewBattleGroup(CrewBattleTeamNameRight).ToList();
    }
    
    private IEnumerable<string> GetCrewBattleGroup(string groupName)
    {
        var users = CPH.UsersInGroup(groupName);
        return users.Select(user => user.Id);
    }
}