using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SmashGeneral.SkinManager;
using SBCustomClasses.StreamDeck;

public class CPHInline : CPHInlineBase
{
    
    private BaseGameInfo _smashGameInfo;
    private StreamDeckConfiguration _streamDeckConfiguration;
    public bool Execute()
    {
        var pathManager =  new PathManager(CPH);
        _smashGameInfo = pathManager.GetBaseGameInfo();
        _streamDeckConfiguration = pathManager.GetStreamDeckConfiguration();
        return UpdateCharacterSkin();
    }
    
    private bool UpdateCharacterSkin()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var split = rawInput.Split(',');

        if (split.Length < 2)
        {
            CPH.SendMessage("El formato del comando es: !skin (personaje),(skin)");
            return false;
        }

        int index = 0;
        if (!CPH.TryGetArg<string>("userId", out var userId))
        {
            return false;
        }

        if (split.Length == 3)
        {
            var user = CPH.TwitchGetUserInfoByLogin(split[index].Replace("@", ""));
            if (user == null)
            {
                CPH.SendMessage("No se encontró el usuario");
                return false;
            }

            userId = user.UserId;
            index++;
        }

        var pathManager = new PathManager(CPH);
        var character = CharacterFuzzyTools.Get(pathManager).SelectCharacterFuzzy(split[index]);
        index++;
        CPH.LogDebug("wtf skin 1");
        if (!int.TryParse(split[index].Trim(), out var skin))
        {
            CPH.SendMessage("El skin debe ser un número");
            return false;
        }
        if (skin is < 1 or > 8)
        {
            CPH.SendMessage("El skin debe estar entre 1 y 8.");
            return false;
        }
        CPH.SetTwitchUserVarById(userId, $"skin_{character}", skin - 1);
        
        CPH.SendMessage("Se configuró la skin!");
        
        if (_smashGameInfo != null)
        {
            if (!_smashGameInfo.CharacterToCodename.TryGetValue(character, out var codename)) return false;

            var selectedTheme = CPH.GetGlobalVar<string>("IronManSmashIcons");
            CPH.LogDebug($"Getting game {pathManager.CurrentGameId}");
            var buttonIcons =
                _streamDeckConfiguration.Theming.GetButtonsIcons(pathManager.CurrentGameId, selectedTheme, out bool found);

            var skinIndex = CPH.GetTwitchUserVarById<int>(userId, $"skin_{character}");
            var path = buttonIcons.GetCompletePath(codename.Codename, skinIndex);
            CPH.SetArgument("skinPortrait", path);
            CPH.SetArgument("skinFound", true);
        }
        else
        {
            CPH.SetArgument("skinFound", false);
        }
        
        // TODO: Nice to have so the stream decks updates if there is a character already there
        // but it fails the second time for some reason and stops the execution.
        //StreamDeckTSHConnection.Get(CPH).RefreshCharacters(CPH);
        return true;
    }
}