using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.UserReady;

public class CPHInline : CPHInlineBase
{
    private const string OverrideCharacterCommand = "!o ";
    public bool Execute()
    {
        if (!CPH.TryGetArg("targetUserId", out string userId))
        {
            CPH.LogError("Wtf, targetUserId not found. Are you obtaining the target information from the action?");
            return false;
        }
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            CPH.LogError("Wtf, targetUserId not found. Are you obtaining the target information from the action?");
            return false;
        }
        
        if (rawInput.StartsWith(OverrideCharacterCommand))
        {
            var command = rawInput.Split(' ');
            var userLogin = CPH.TwitchGetUserInfoByLogin(command[1].Trim('@'));
            if (userLogin == null)
            {
                return false;
            }
            userId = userLogin.UserId;
            rawInput = "";
            for (var i = 2; i < command.Length; i++)
            {
                rawInput += $"{command[i]} ";
            }

            rawInput = rawInput.Trim();

        }
        CPH.SetTwitchUserVarById(userId, "ready", true );
        return true;
    }
}