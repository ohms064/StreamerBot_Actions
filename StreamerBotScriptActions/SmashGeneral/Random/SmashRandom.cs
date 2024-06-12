using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SmashGeneral.Random;

using System.Collections.Generic;
using System.IO;
using System;

public class CPHInline : CPHInlineBase
{
    private const string CharsPath = "D:/Streams/characters.txt";
    private const string CharacterCommand = "!c ";
    private const string OverrideCharacterCommand = "!o ";

    public bool Execute()
    {
        var result = File.ReadAllText(CharsPath);
        var characters = result.Split('\n');
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }
        
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);

        var args = rawInput.Split();
        if (args.Length < 1)
        {
            CPH.LogInfo($"Hay que escribir cuántos personajes quieres (max: 10) {args.Length}");
            return false;
        }
        if (!int.TryParse(args[0], out var count) && count > 0 && count < 11)
        {
            CPH.LogInfo($"Hay que escribir cuántos personajes quieres (max: 10) {args[1]}");
            return false;
        }

        var randomGenerator = new Random(DateTime.Now.Millisecond);

        CPH.LogInfo("Generando personajes!");
        foreach (var user in eventUsers)
        {
            var randomCharacters = GetRandomCharacters(randomGenerator, characters, count);
            // Sending the command, maybe a little lazy but it should work
            var chars = string.Join(",", randomCharacters);
            CPH.LogInfo($"Personajes para {user.Username} {chars}!");
            CPH.SendMessage($"!o {user.Username} {chars}", false);
        }
        
        
        // Before starting, we check if the source is different from a whisper
        return true;
    }

    private List<string> GetRandomCharacters(Random randomGenerator, string[] characters, int count)
    {
        var result = new List<string>();
        var nonRepeatedCharacters = new List<string>(characters);
        nonRepeatedCharacters.Remove("Random");
        for (var i = 0; i < count; i++)
        {
            var index = randomGenerator.Next(nonRepeatedCharacters.Count);
            result.Add(nonRepeatedCharacters[index]);
            nonRepeatedCharacters.RemoveAt(index);
        }

        return result;
    }
}