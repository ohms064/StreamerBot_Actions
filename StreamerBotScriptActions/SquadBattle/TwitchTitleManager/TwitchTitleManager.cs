using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.TwitchTitleManager;

public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);

        CPH.SetChannelTitle($"SquadBattle! {eventUsers[0].Username} vs {eventUsers[1].Username}");
        return true;
    }
}