using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.CrewBattle.CrewBattlePlayersSetup;

using System.Collections.Generic;
using System.Linq;
using FuzzySharp;

public class CPHInline : CPHInlineBase
{
    private const string CrewBattleTeamNameRight = "Crew Battle Team Right";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Left";
    
    public bool Execute()
    {
        if (!CPH.TryGetArg("userId", out string userId))
        {
            return false;
        }
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }
        
        return AddCharacter(userId, rawInput);
    }

    public bool AddCharacterCommand()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }
        
        var commandArgs = new List<string>(rawInput.Split());

        if (commandArgs.Count < 2)
        {
            CPH.SendMessage("Faltó el usuario o el equipo que se va a unir.");
            return false;
        }

        var userName = commandArgs[0].Replace("@", "");
        var userInfo = CPH.TwitchGetUserInfoByLogin(userName);
        if (userInfo == null)
        {
            CPH.SendMessage("Alguien dígale a ohms que escrbió mal el nombre :v");
            return false;
        }

        commandArgs.RemoveAt(0);

        var team = string.Join(" ", commandArgs);

        return AddCharacter(userInfo.UserId, team, true);
    }


    public bool RemoveCharacterCommand()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }
        
        var userInfo = CPH.TwitchGetUserInfoByLogin(rawInput);
        
        return RemovePlayer(userInfo.UserId);
    }

    public bool RemoveCharacter()
    {
        if (!CPH.TryGetArg("userId", out string userId))
        {
            return false;
        }

        return RemovePlayer(userId);
    }

    private bool AddCharacter(string userId, string team, bool forceNoReply=false)
    {
        CPH.LogDebug("Adding player");
        var teams = new List<string>
        {
            CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft),
            CPH.GetGlobalVar<string>(CrewBattleTeamNameRight)
        };

        foreach (var t in teams.Where(t => CPH.UserIdInGroup(userId, Platform.Twitch, t)))
        {
            CPH.RemoveUserIdFromGroup(userId, Platform.Twitch, t);
        }

        var userSelectedTeam = Process.ExtractOne(team, teams).Value.Trim();
        var joined = CPH.AddUserIdToGroup(userId, Platform.Twitch, userSelectedTeam);

        if (!joined)
        {
            CPH.SendMessage("Error mágico para meter al usuario.");
            return false;
        }

        var leftGroup = CPH.UsersInGroup(teams[0]);
        var rightGroup = CPH.UsersInGroup(teams[1]);

        if (CPH.TryGetArg("msgId", out string msgId) && !forceNoReply)
        {
            CPH.TwitchReplyToMessage($"Te has registrado a: {userSelectedTeam}! {teams[0]}:{leftGroup.Count} {teams[1]}:{rightGroup.Count}", msgId);
        }
        else
        {
            var userInfo = CPH.TwitchGetUserInfoById(userId);
            CPH.SendMessage($"Se ha registrado a {userInfo.UserName} en:  {userSelectedTeam}! {teams[0]}:{leftGroup.Count} {teams[1]}:{rightGroup.Count}");
        }

        return true;
    }

    private bool RemovePlayer(string userId, bool forceNoReply=false)
    {
        var teams = new List<string>
        {
            CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft),
            CPH.GetGlobalVar<string>(CrewBattleTeamNameRight)
        };
        
        foreach (var t in teams.Where(t => CPH.UserIdInGroup(userId, Platform.Twitch, t)))
        {
            CPH.RemoveUserIdFromGroup(userId, Platform.Twitch, t);
        }
        
        if (CPH.TryGetArg("msgId", out string msgId) && !forceNoReply)
        {
            CPH.TwitchReplyToMessage($"Te has salido de la Crew Battle", msgId);
        }
        else
        {
            var userInfo = CPH.TwitchGetUserInfoById(userId);
            CPH.SendMessage($"Se ha eliminado a {userInfo.UserName} de la crew battle ");
        }

        return true;
    }
}