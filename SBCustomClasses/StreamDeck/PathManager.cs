using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public class PathManager
    {
        public string CurrentGameId { get; private set; } = "ssbu"; // The same as TSH, currently tested ssbu and roa2

        private const string GamesBaseKey = "TSHBaseGamesKey";
        private const string StreamDeckConfigPathKey = "StreamDeckConfigKey";
        private const string CurrentGameKey = "CurrentGameId";

        private readonly string _baseFilePath = "";

        private string BaseGamePath => $"{_baseFilePath}/{CurrentGameId}";
        private string GameConfigFile => $"{BaseGamePath}/base_files/config.json";
        public string CharactersFile => $"{BaseGamePath}/characters.txt";
        public string LogoFile => $"{BaseGamePath}/base_files/logo.png";
        private string StreamDeckConfigPath { get; }
        
        public string GameMains => $"userMains_{CurrentGameId}";

        public PathManager(IInlineInvokeProxy CPH)
        {
            _baseFilePath = CPH.GetGlobalVar<string>(GamesBaseKey);
            CurrentGameId = CPH.GetGlobalVar<string>(CurrentGameKey);
            StreamDeckConfigPath = CPH.GetGlobalVar<string>(StreamDeckConfigPathKey);
        }

        public StreamDeckConfiguration GetStreamDeckConfiguration()
        {
            return GetConfigFile<StreamDeckConfiguration>(StreamDeckConfigPath);
        }
        
        public BaseGameInfo GetBaseGameInfo()
        {
            return GetConfigFile<BaseGameInfo>(GameConfigFile);
        }

        private static T GetConfigFile<T>(string fileName) where T : new()
        {
            T result;
            try
            {
                var fileContent = File.ReadAllText(fileName);
                result = JsonConvert.DeserializeObject<T>(fileContent);
            }
            catch(FileNotFoundException)
            {
                result = new T();
            }

            return result;
        }


        public void UpdateGameId(IInlineInvokeProxy CPH)
        {
            CurrentGameId = CPH.GetGlobalVar<string>(CurrentGameKey);
        }
    }
}