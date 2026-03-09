using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.SmashGeneral.PlayerCharacters;

using System.Collections.Generic;
using System.Linq;
using SBCustomClasses.StreamDeck;
using UpdateAction = System.Action<System.Collections.Generic.List<string>, System.Collections.Generic.IEnumerable<string>>;
// ReSharper disable once InconsistentNaming
public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        return AddToPlayerRoster();
    }

    private bool UpdatePlayerRoster(UpdateAction Action)
    {
        
        if (!CPH.TryGetArg<string>("userId", out var userId))
        {
            return false;
        }
        
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var commandArgs = rawInput.Split(' ').ToList();

        if (commandArgs.Count == 0)
        {
            return false;
        }

        if (commandArgs.First().StartsWith("@"))
        {
            // Make sure this is an admin
            var callingUser = CPH.TwitchGetExtendedUserInfoById(userId);
            if (!CPH.UserInGroup(callingUser.UserName, Platform.Twitch, "Mods"))
            {
                CPH.SendMessage("No tienes permisos para ésto");
                return false;
            }
            
            var user = CPH.TwitchGetUserInfoByLogin(commandArgs.First().Replace("@", ""));
            if (user == null)
            {
                CPH.SendMessage($"No se encontró el usuario {commandArgs.First()}");
                return false;
            }

            userId = user.UserId;
            commandArgs.RemoveAt(0);
            rawInput = string.Join(" ", commandArgs);
        }
        
        var split = rawInput.Split(',');
        var fuzzy = CharacterFuzzyTools.Get(new PathManager(CPH));
        var characters = split.Select(c => fuzzy.SelectCharacterFuzzy(c));
        const string recurringCharactersKey = "userRecurringCharacters_Smash";

        var startingCharacters = CPH.GetTwitchUserVarById<List<string>>(userId, recurringCharactersKey) ?? new List<string>();
        Action.Invoke(startingCharacters, characters);
        CPH.SetTwitchUserVarById(userId, recurringCharactersKey, startingCharacters);
        
        
        StreamDeckTSHConnection.Get(CPH).RefreshCharacters(CPH);

        CPH.SendMessage("Se actualizó el roster");

        return true;
    }

    public bool RemoveFromPlayerRoster()
    {
        return UpdatePlayerRoster((mainRoster, characters) =>
        {
            mainRoster.RemoveAll(characters.Contains);
        });

    }
    
    public bool AddToPlayerRoster()
    {
        return UpdatePlayerRoster((mainRoster, characters) =>
        {
            foreach (var character in characters)
            {
                if(!mainRoster.Contains(character))
                    mainRoster.Add(character);
            }
        });

    }
}