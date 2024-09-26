using System.IO;
using FuzzySharp;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public class CharacterFuzzyTools
    {
        private const string CharsPath = "D:/Streams/characters.txt";
        private readonly string[] _characterList;

        private static CharacterFuzzyTools _instance;

        public static CharacterFuzzyTools Get()
        {
            return _instance ?? (_instance = new CharacterFuzzyTools());
        }

        private CharacterFuzzyTools()
        {
            _characterList = File.ReadAllText(CharsPath).Split('\n');
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
    }
}