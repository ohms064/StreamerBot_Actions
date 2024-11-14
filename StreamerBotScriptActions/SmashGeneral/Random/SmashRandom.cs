using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Model;

namespace StreamerBotScriptActions.SmashGeneral.Random;

using SBCustomClasses.StreamDeck;
using System.Collections.Generic;
using System.IO;
using System;

public class CPHInline : CPHInlineBase
{
    private const string LeftTeam = "teamLeft";
    private const string RightTeam = "teamRight";
    private int indexToUpdate = 0;

    public bool Execute()
    {
        indexToUpdate = 0;
        return true;
    }
    
    public bool UpdatePlayer()
    {
    	CPH.LogInfo($"Starting random characters command");
        var pathManager = new PathManager(CPH);
        var result = File.ReadAllText(pathManager.CharactersFile);
        var characters = result.Split('\n');
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
        	CPH.LogInfo($"WTF rawInput not found");
            return false;
        }
        
        var eventUsers = new List<GroupUser>();
        eventUsers.AddRange(CPH.UsersInGroup(LeftTeam));
        eventUsers.AddRange(CPH.UsersInGroup(RightTeam));
        
        if(eventUsers.Count == 0){
        	CPH.LogInfo($"Event users not set");
        	return false;
        }

        var args = rawInput.Split();
        if (args.Length < 1)
        {
            CPH.LogInfo($"Hay que escribir cuántos personajes quieres (max: 10) {args.Length}");
            return false;
        }
        if (!int.TryParse(args[0], out var count) && count > 0 && count < 11)
        {
            CPH.LogInfo($"Valor inválido (max: 10) {args[1]}");
            return false;
        }

        var randomGenerator = new Random(DateTime.Now.Millisecond);

        CPH.LogInfo("Generando personajes!");

        if (eventUsers.Count <= indexToUpdate)
        {
            return false;
        }
        
        var user = eventUsers[indexToUpdate];
        var randomCharacters = GetRandomCharacters(randomGenerator, characters, count);
        // Sending the command, maybe a little lazy but it should work
        var chars = string.Join(",", randomCharacters);
        CPH.LogInfo($"Personajes para {user.Username} {chars}!");
        CPH.SendMessage($"!o {user.Username} {chars}", false);

        indexToUpdate++;
        
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

    public bool MessageRandomCharacters()
    {
        int count = 1;
        var pathManager = new PathManager(CPH);
        var result = File.ReadAllText(pathManager.CharactersFile);
        var characters = result.Split('\n');
        if (CPH.TryGetArg<string>("rawInput", out var rawInput))
        {
            if (int.TryParse(rawInput, out var parseResult))
            {
                count = parseResult;
            }
        }
        
        var randomGenerator = new Random(DateTime.Now.Millisecond);
        var characterResult = GetRandomCharacters(randomGenerator, characters, count);
        CPH.SendMessage(string.Join(", ", characterResult));
        return true;
    }
}