using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.OnCharactersSet;

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
        // your main code goes here
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
}