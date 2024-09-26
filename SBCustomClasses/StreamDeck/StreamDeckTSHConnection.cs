using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public partial class StreamDeckTSHConnection
    {
        #region Constants

        // TODO: Maybe have this as a config file
        private const string SmashFileContent =
            "D:/Streams/TournamentStreamHelper/user_data/games/ssbu/base_files/config.json";

        private const string StreamDeckConfig =
            "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json";
        #endregion

        #region Static

        private static StreamDeckTSHConnection _instance;

        public static StreamDeckTSHConnection Get(IInlineInvokeProxy CPH)
        {
            return _instance ?? (_instance = new StreamDeckTSHConnection(CPH));
        }

        #endregion
        
        private StreamDeckTSHConnection(IInlineInvokeProxy CPH)
        {
            CPH.LogDebug($"Deserializing: {StreamDeckConfig}");
            var streamDeckConfigJson = File.ReadAllText(StreamDeckConfig);
            _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(streamDeckConfigJson);
            var smashGameJson = File.ReadAllText(SmashFileContent);
            _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(smashGameJson);
            _buttonCharacterDict =
                CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons")
                ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            _charactersFuzzySearch = CharacterFuzzyTools.Get();
        }

        public void InitConnection(IInlineInvokeProxy CPH, TeamData leftTeam, TeamData rightTeam,
            int startingStocks = 0)
        {
            SetupTeams(CPH, leftTeam, rightTeam, startingStocks);
            UpdateCurrentMatch(CPH);
            UpdateListData(CPH);
            RefreshStreamDeck(CPH, StreamDeckSections.BothTeams | StreamDeckSections.StartingSections );
        }

        public void SelectCharacter(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            CharacterButtonPressed(CPH, pressedButtonId);
            UpdateCurrentMatch(CPH);
        }

        public void ToggleCharacterState(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            CPH.LogDebug("Toggling button state");
            if (!_buttonCharacterDict.TryGetValue(pressedButtonId, out var streamDeckButtonState))
            {
                return;
            }
            
            TeamInfo team;
            TeamUserInfo userInfo;
            CPH.LogDebug($"Selecting team {streamDeckButtonState.UserId}");
            var streamDeckSection = StreamDeckSections.Characters;
            if (_teamLeft.TeamMembers.ContainsKey(streamDeckButtonState.UserId))
            {
                team = _teamLeft;
                streamDeckSection |= StreamDeckSections.TeamLeft;
            }
            else if(_teamRight.TeamMembers.ContainsKey(streamDeckButtonState.UserId))
            {
                team = _teamRight;
                streamDeckSection |= StreamDeckSections.TeamRight;
            }
            else
            {
                CPH.LogError("Button not assigned to any team");
                return;
            }
            CPH.LogDebug($"Toggling character state: {streamDeckButtonState.Character}");
            userInfo = team.TeamMembers[streamDeckButtonState.UserId];
            if (userInfo.Characters.Contains(streamDeckButtonState.Character))
            {
                userInfo.Characters.Remove(streamDeckButtonState.Character);
                userInfo.Characters.Add("");
                streamDeckButtonState.SelectedState = SelectionState.Selected;
                streamDeckButtonState.ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor;
            }
            else
            {
                streamDeckButtonState.SelectedState = SelectionState.Unselected;
                streamDeckButtonState.ColorState = _streamDeckConfiguration.Theming.UnselectedColor;
                userInfo.Characters.RemoveAll((string item) => item == "");
                userInfo.Characters.Add(streamDeckButtonState.Character);
                CPH.LogDebug($"Filling with empty: {userInfo.Characters.Count} | {userInfo.OriginalCharacters.Count}");
                while (userInfo.Characters.Count < userInfo.OriginalCharacters.Count)
                {
                    userInfo.Characters.Add("");
                }
            }
            RefreshStreamDeck(CPH, streamDeckSection);
            UpdateCurrentMatch(CPH);
        }

        public void SelectPlayer(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            PlayerButtonPressed(CPH, pressedButtonId);
            UpdateCurrentMatch(CPH);
        }
        
        public void UpdateStocks(IInlineInvokeProxy CPH, bool leftTeam, int stocksAdd = -1)
        {
            CurrentCharacterButtonPressed(CPH, stocksAdd, leftTeam);
            UpdateCurrentMatch(CPH);
        }
    }
}