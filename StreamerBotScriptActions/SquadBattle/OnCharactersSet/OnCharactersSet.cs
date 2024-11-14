using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Model;

namespace StreamerBotScriptActions.SquadBattle.OnCharactersSet;

using System.Collections.Generic;
using System.Text;
public class CPHInline : CPHInlineBase
{
    private const string LeftTeam = "teamLeft";
    private const string RightTeam = "teamRight";
    private const string TeamRoster = "currentRoster";
    
    public bool Execute()
    {
        if (!CheckIfListeningForPlayers())
        {
            return false;
        }

        var eventUsers = new List<GroupUser>();
        eventUsers.AddRange(CPH.UsersInGroup(LeftTeam));
        eventUsers.AddRange(CPH.UsersInGroup(RightTeam));
        foreach (var user in eventUsers)
        {
            var isUserReady = CPH.GetTwitchUserVarById<bool>(user.Id, "ready");
            if (!isUserReady)
            {
                // If at least one user is not ready we cannot continue
                CPH.LogDebug($"{user.Username} not ready {eventUsers.Count}");
                return false;
            }
        }

        CPH.LogDebug("Both users are ready!");
        return true;
    }

    public bool CheckIfPlayersReady()
    {
        var eventUsers = new List<GroupUser>();
        eventUsers.AddRange(CPH.UsersInGroup(LeftTeam));
        eventUsers.AddRange(CPH.UsersInGroup(RightTeam));
        foreach (var user in eventUsers)
        {
            var isUserReady = CPH.GetTwitchUserVarById<bool>(user.Id, "ready");
            if (!isUserReady)
            {
                // If at least one user is not ready we cannot continue
                CPH.LogDebug($"{user.Username} not ready");
                return false;
            }
        }

        CPH.LogDebug("Both users are ready!");
        return true;
    }

    public bool CheckIfListeningForPlayers()
    {
        return CPH.GetGlobalVar<bool>("listeningToCharacters");
    }

    public bool AnnounceCharacters()
    {
        var eventUsers = new List<GroupUser>();
        eventUsers.AddRange(CPH.UsersInGroup(LeftTeam));
        eventUsers.AddRange(CPH.UsersInGroup(RightTeam));
        const string separator = ", ";
        CPH.TwitchAnnounce(
            "Se registraron los siguientes personajes, avisen de cualquier error que pronto comenzaremos");
        foreach(var user in eventUsers)
        {
            var message = new StringBuilder();
            var squadRoster = CPH.GetTwitchUserVarById<List<string>>(user.Id, TeamRoster);
            message.Append($"{user.Username} [{squadRoster.Count}]: ");
            foreach (var character in squadRoster)
            {
                message.Append($"{character}{separator}");
            }

            message.Remove(message.Length - separator.Length, separator.Length);
            message.Append("\n");
            CPH.SendMessage(message.ToString());
        }
        return true;
    }
}