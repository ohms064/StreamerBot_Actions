using System.Collections.Generic;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public partial class StreamDeckTSHConnection
    {
        #region Static

        private static StreamDeckTSHConnection _instance;

        public static StreamDeckTSHConnection Get(IInlineInvokeProxy CPH)
        {
            return _instance ?? (_instance = new StreamDeckTSHConnection(CPH));
        }

        #endregion
        
        private StreamDeckTSHConnection(IInlineInvokeProxy CPH)
        {
            _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(StreamDeckConfig);
            _buttonCharacterDict =
                CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons")
                ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            _charactersFuzzySearch = CharacterFuzzyTools.Get();
            // We may not have ready a TSH configuration file
            // _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(SmashFileContent);
        }

        public void InitConnection(IInlineInvokeProxy CPH, TeamData leftTeam, TeamData rightTeam,
            int startingStocks = 0)
        {
            SetupTeams(CPH, leftTeam, rightTeam, startingStocks);
            UpdateListData(CPH);
            UpdateCurrentMatch(CPH);
        }

        public void SelectCharacter(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            CharacterButtonPressed(CPH, pressedButtonId);
            UpdateCurrentMatch(CPH);
        }

        public void SelectPlayer(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            PlayerButtonPressed(CPH, pressedButtonId);
            UpdateCurrentMatch(CPH);
        }
    }
}