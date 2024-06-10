using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.OnCharactersSet;

using System.Collections.Generic;
using System.Text;
public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        if (!CheckIfListeningForPlayers())
        {
            return false;
        }
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        foreach (var user in eventUsers)
        {
            var isUserReady = CPH.GetTwitchUserVarById<bool>(user.Id, "ready");
            if (!isUserReady)
            {
                // If at least one user is not ready we cannot continue
                CPH.LogDebug("Not ready");
                return false;
            }
        }

        CPH.LogDebug("Both users are ready!");
        return true;
    }

    public bool CheckIfPlayersReady()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        foreach (var user in eventUsers)
        {
            var isUserReady = CPH.GetTwitchUserVarById<bool>(user.Id, "ready");
            if (!isUserReady)
            {
                // If at least one user is not ready we cannot continue
                CPH.LogDebug("Not ready");
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
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        const string separator = ", ";
        CPH.TwitchAnnounce(
            "Se registraron los siguientes personajes, avisen de cualquier error que pronto comenzaremos");
        foreach(var user in eventUsers)
        {
            var message = new StringBuilder();
            var squadRoster = CPH.GetTwitchUserVarById<List<string>>(user.Id, "squadRoster");
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