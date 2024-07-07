using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.Stream_General.EndCredits;

using System.Text;

public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        return AddSquadBattleToCredits();
    }

    public bool AddSquadBattleToCredits()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);

        if (eventUsers.Count < 2)
        {
            return false;
        }
        
        
        CPH.AddToCredits("Squad Battle", $"{eventUsers[0].Username} vs {eventUsers[1].Username}", false);
        return true;
    }

    public bool AddTargetUserToCategory()
    {
        if (!CPH.TryGetArg("userId", out string userId))
        {
            return false;
        }
        
        if (!CPH.TryGetArg("creditsCategory", out string creditsCategory))
        {
            return false;
        }

        var userInfo = CPH.TwitchGetUserInfoById(userId);
        CPH.AddToCredits(creditsCategory, userInfo.UserName);
        
        return true;
    }
}