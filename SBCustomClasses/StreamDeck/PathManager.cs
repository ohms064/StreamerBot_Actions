using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public class PathManager
    {
        public string CurrentGameId { get; private set; } = "ssbu"; // The same as TSH, currently tested ssbu and roa2

        private const string GamesBaseKey = "TSHBaseGamesKey";
        private const string StreamDeckConfigKey = "StreamDeckConfigKey";
        private const string CurrentGameKey = "CurrentGameId";

        private readonly string _baseFilePath = "";

        private string BaseGamePath => $"{_baseFilePath}/{CurrentGameId}";
        public string GameConfigFile => $"{BaseGamePath}/base_files/config.json";
        public string CharactersFile => $"{BaseGamePath}/characters.txt";
        public string LogoFile => $"{BaseGamePath}/base_files/logo.png";
        public string StreamDeckConfig { get; }
        
        public string GameMains => $"userMains_{CurrentGameId}";

        public PathManager(IInlineInvokeProxy CPH)
        {
            _baseFilePath = CPH.GetGlobalVar<string>(GamesBaseKey);
            StreamDeckConfig = CPH.GetGlobalVar<string>(StreamDeckConfigKey);
            CurrentGameId = CPH.GetGlobalVar<string>(CurrentGameKey);
        }

        public void UpdateGameId(IInlineInvokeProxy CPH)
        {
            CurrentGameId = CPH.GetGlobalVar<string>(CurrentGameKey);
        }
    }
}