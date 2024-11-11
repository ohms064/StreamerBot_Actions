using System.Collections.Generic;
using System.IO;
using System.Linq;
using FuzzySharp;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public class CharacterFuzzyTools
    {
        private const string CharsKey = "CharKey";
        
        private string[] _characterList;

        private static CharacterFuzzyTools _instance;

        public static CharacterFuzzyTools Get(PathManager pathManager)
        {
            return _instance ?? (_instance = new CharacterFuzzyTools(pathManager));
        }

        private CharacterFuzzyTools(PathManager pathManager)
        {
            _characterList = File.ReadAllText(pathManager.CharactersFile).Split('\n');
        }

        public void UpdateGame(PathManager pathManager)
        {
            _characterList = File.ReadAllText(pathManager.CharactersFile).Split('\n');
        }

        public static string ExtractFuzzy(string value, string[] validValues)
        {
            var result = Process.ExtractOne(value, validValues);
            return result.Value.Trim();
        }

        public string SelectCharacterFuzzy(string character)
        {
            return ExtractFuzzy(character, _characterList);
        }

        public List<string> SelectCharacterFuzzy(IEnumerable<string> characters)
        {
            return characters.Select(SelectCharacterFuzzy).ToList();
        }
    }
}