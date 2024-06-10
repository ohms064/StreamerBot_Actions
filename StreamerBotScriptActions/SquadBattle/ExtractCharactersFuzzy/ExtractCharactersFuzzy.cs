using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.ExtractCharactersFuzzy;

using System.Collections.Generic;
using FuzzySharp;
using System.IO;

public class CPHInline : CPHInlineBase
{
    private const string CharsPath = "D:/Streams/characters.txt";
    private const string CharacterCommand = "!c ";
    private const string OverrideCharacterCommand = "!o ";

    public bool Execute()
    {
        CPH.LogDebug("Starting Fuzzy search");
        var result = File.ReadAllText(CharsPath);
        var characters = result.Split('\n');
        CPH.TryGetArg("rawInput", out string rawInput);
        CPH.TryGetArg("targetUserId", out string userId);
        var characterNicknames = CPH.GetGlobalVar<Dictionary<string, string>>("characterNicknames");
        
        // Before starting, we check if the source is different from a whisper
        if (rawInput.StartsWith(CharacterCommand))
        {
            rawInput = rawInput.Remove(0, CharacterCommand.Length);
        }
        else if (rawInput.StartsWith(OverrideCharacterCommand))
        {
            var command = rawInput.Split(' ');
            var userLogin = CPH.TwitchGetUserInfoByLogin(command[1]);
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
        
        // Obtain the characters
        var userSelectedCharacters = rawInput.Split(',');
        var resultCharactersForUser = new List<string>(userSelectedCharacters.Length);
        foreach (var selectedChar in userSelectedCharacters)
        {
            // Really only for aegis being Pyra and Mythra, but in case there are more that I haven't think of
            if (characterNicknames.TryGetValue(selectedChar, out var correctedSelectedChar))
            {
                resultCharactersForUser.Add(ExtractFuzzy(correctedSelectedChar, characters));
                continue;
            }
            resultCharactersForUser.Add(ExtractFuzzy(selectedChar, characters));
        }

        CPH.SetTwitchUserVarById(userId, "squadRoster", resultCharactersForUser);
        CPH.SetTwitchUserVarById(userId, "userSquadRosterLosers", new List<string>());
        CPH.SetTwitchUserVarById(userId, "stocks", userSelectedCharacters.Length * 3);
        CPH.SetTwitchUserVarById(userId, "ready", true);

        CPH.LogDebug(@"Completed Fuzzy search {resultCharactersForUser}");

        return true;
    }

    public string ExtractFuzzy(string value, string[] validValues)
    {
        var result = Process.ExtractOne(value, validValues);
        CPH.SetGlobalVar("fuzzyResult", result.Value);
        return result.Value.Trim();
    }
}