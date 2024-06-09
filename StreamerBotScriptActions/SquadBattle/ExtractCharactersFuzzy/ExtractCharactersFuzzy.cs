using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.ExtractCharactersFuzzy;

using System.Collections.Generic;
using FuzzySharp;
using System.IO;

public class CPHInline : CPHInlineBase
{
    private const string CharsPath = "D:/Streams/characters.txt";

    public bool Execute()
    {
        CPH.LogDebug("Starting Fuzzy search");
        var result = File.ReadAllText(CharsPath);
        var characters = result.Split('\n');
        CPH.TryGetArg("rawInput", out string rawInput);
        CPH.TryGetArg("targetUserId", out string userId);
        var userSelectedCharacters = rawInput.Split(',');
        var resultCharactersForUser = new List<string>(userSelectedCharacters.Length);
        foreach (var selectedChar in userSelectedCharacters)
        {
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