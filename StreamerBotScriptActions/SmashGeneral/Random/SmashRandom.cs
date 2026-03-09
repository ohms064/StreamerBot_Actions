using System.Linq;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
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
    private const string IronManCount = "ironManCount";
    private const string IronManMaxCount = "ironManMaxCount";
    
    private int _indexToUpdate = 0;
    private List<string> _ironManChallengeList;
    private BaseGameInfo _smashGameInfo;
    private StreamDeckConfiguration _streamDeckConfiguration;
    
    public bool Execute()
    {
        _indexToUpdate = 0;
        var pathManager = new PathManager(CPH);
        _smashGameInfo = pathManager.GetBaseGameInfo();
        _streamDeckConfiguration = pathManager.GetStreamDeckConfiguration();
        return true;
    }
    
    public bool UpdatePlayer()
    {
    	CPH.LogInfo($"Starting random characters command");
        var characters = GetCharacterList();
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

        if (eventUsers.Count <= _indexToUpdate)
        {
            return false;
        }
        
        var user = eventUsers[_indexToUpdate];
        var randomCharacters = GetRandomCharacters(randomGenerator, characters, count);
        // Sending the command. Maybe a little lazy, but it should work
        var chars = string.Join(",", randomCharacters);
        CPH.LogInfo($"Personajes para {user.Username} {chars}!");
        CPH.SendMessage($"!o {user.Username} {chars}", false);

        _indexToUpdate++;
        
        return true;
    }

    private List<string> GetRandomCharacters(Random randomGenerator, IEnumerable<string> characters, int count)
    {
        var result = new List<string>();
        var nonRepeatedCharacters = characters.ToList();
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
        var characters = GetCharacterList();
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

    public bool ResetIronManChallenge()
    {
        _ironManChallengeList = GetCharacterList().ToList();
        _ironManChallengeList.Remove("Random");
        CPH.SetGlobalVar(IronManCount, 0);
        return true;
    }

    public bool PopIronManCharacter()
    {
        if (_ironManChallengeList.Count == 0)
        {
            return false;
        }
        var randomGenerator = new Random(DateTime.Now.Millisecond);
        var randomIndex = randomGenerator.Next(_ironManChallengeList.Count);
        var character =  _ironManChallengeList[randomIndex].Trim();
        _ironManChallengeList.RemoveAt(randomIndex);
        var ironManCount = CPH.GetGlobalVar<int>(IronManCount) + 1;
        var maxCount = CPH.GetGlobalVar<int>(IronManMaxCount);
        if (ironManCount > maxCount)
        {
            CPH.SetGlobalVar(IronManMaxCount, ironManCount);
        }
        CPH.SetGlobalVar(IronManCount, IronManCount);
        CPH.SetArgument(IronManCount, ironManCount);
        CPH.SetArgument(IronManMaxCount, maxCount);
        CPH.SendMessage($"Next: {character}");
        CPH.SetArgument("ironManCharacter", character);
        CPH.LogDebug($"smash game infno: {JsonConvert.SerializeObject(_smashGameInfo)}");
        if (_smashGameInfo == null) return false;
        if (!_smashGameInfo.CharacterToCodename.TryGetValue(character, out var codename)) return false;
        
        var selectedTheme = CPH.GetGlobalVar<string>("IronManSmashIcons");
        var currentGame = CPH.GetGlobalVar<string>("CurrentGameId");
        CPH.LogDebug($"Getting game {currentGame}");
        var buttonIcons = _streamDeckConfiguration.Theming.GetButtonsIcons(currentGame, selectedTheme, out bool found);
        
        var userInfo = CPH.TwitchGetBroadcaster();
        var skinIndex = CPH.GetTwitchUserVarById<int>(userInfo.UserId, $"skin_{character}");
        var path = buttonIcons.GetCompletePath(codename.Codename, skinIndex);
        CPH.SetArgument("ironManPortrait", path);
        return true;
    }

    private IEnumerable<string> GetCharacterList()
    {
        var pathManager = new PathManager(CPH);
        try
        {
            var file = File.OpenText(pathManager.CharactersFile);
            var result = file.ReadToEnd();
            file.Close();
            return result.Split('\n');
        }
        catch(FileNotFoundException)
        {
            CPH.LogError($"File {pathManager.CharactersFile} not found");
            return ["Verify the file name."];
        }

    }
}