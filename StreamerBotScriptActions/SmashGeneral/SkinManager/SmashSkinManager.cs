using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SmashGeneral.SkinManager;
using SBCustomClasses.StreamDeck;

public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        return UpdateCharacterSkin();
    }
    
    private bool UpdateCharacterSkin()
    {
        
        if (!CPH.TryGetArg<string>("userId", out var userId))
        {
            return false;
        }
        
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var split = rawInput.Split(',');

        if (split.Length < 2)
        {
            return false;
        }

        int index = 0;

        if (split.Length == 3)
        {
            var user = CPH.TwitchGetUserInfoByLogin(split[index].Replace("@", ""));
            if (user == null)
            {
                return false;
            }

            userId = user.UserId;
            index++;
        }

        var pathManager = new PathManager(CPH);
        var character = CharacterFuzzyTools.Get(pathManager).SelectCharacterFuzzy(split[index]);
        index++;
        
        if (!int.TryParse(split[index].Trim(), out var skin))
        {
            return false;
        }

        if (skin is < 1 or > 8)
        {
            return false;
        }

        index++;
        CPH.SetTwitchUserVarById(userId, $"skin_{character}", skin - 1);
        
        StreamDeckTSHConnection.Get(CPH).RefreshCharacters(CPH);

        CPH.SendMessage("Se configuró la skin!");

        return true;
    }
}