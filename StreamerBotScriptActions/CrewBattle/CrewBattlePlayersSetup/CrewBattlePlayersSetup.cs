using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.CrewBattle.CrewBattlePlayersSetup;

using System.Collections.Generic;
using System.Linq;
using FuzzySharp;

public class CPHInline : CPHInlineBase
{
    private const string CrewBattleTeamNameRight = "Crew Battle Team Left";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Right";
    
    public bool Execute()
    {
        if (!CPH.TryGetArg("targetUserId", out string userId))
        {
            return false;
        }

        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var teams = new List<string>
        {
            CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft),
            CPH.GetGlobalVar<string>(CrewBattleTeamNameRight)
        };

        foreach (var t in teams.Where(t => CPH.UserIdInGroup(userId, Platform.Twitch, t)))
        {
            CPH.RemoveUserIdFromGroup(userId, Platform.Twitch, t);
        }

        var userSelectedTeam = Process.ExtractOne(rawInput, teams);
        var joined = CPH.AddUserIdToGroup(userId, Platform.Twitch, userSelectedTeam.Value.Trim());

        if (!joined)
        {
            return false;
        }

        var leftGroup = CPH.UsersInGroup(CrewBattleTeamNameLeft);
        var rightGroup = CPH.UsersInGroup(CrewBattleTeamNameRight);
        
        CPH.SendMessage($"Se ha registrado! {teams[0]}:{leftGroup.Count} {teams[1]}:{rightGroup.Count}");
        
        return true;
    }
}