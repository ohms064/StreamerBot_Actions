using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
using SocketIO.Serializer.NewtonsoftJson;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public partial class StreamDeckTSHConnection
    {
        #region Static

        private static StreamDeckTSHConnection _instance;

        public static StreamDeckTSHConnection Get(IInlineInvokeProxy CPH)
        {
            if (_instance != null) return _instance;
            CPH.LogInfo("Creating new StreamDechTSHConnection instance");
            _instance = new StreamDeckTSHConnection(CPH);
            return _instance;
        }

        #endregion

        public readonly PathManager PathManager;
        
        private StreamDeckTSHConnection(IInlineInvokeProxy CPH)
        {
            PathManager = new PathManager(CPH);
            _streamDeckConfiguration = PathManager.GetStreamDeckConfiguration();
            _gameInfo = PathManager.GetBaseGameInfo();
            _buttonCharacterDict =
                CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons")
                ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            _charactersFuzzySearch = CharacterFuzzyTools.Get(PathManager);
            
            CPH.LogDebug($"Connected to: localhost with socketio");
        }

        /// <summary>
        /// Sets up a connection with dcata
        /// </summary>
        /// <param name="CPH">Streamer bot interface</param>
        /// <param name="leftTeam">Left team data (players, characters)</param>
        /// <param name="rightTeam">Right team data (players, characters)</param>
        /// <param name="startingStocks">Starting stocks.</param>
        public void InitConnection(IInlineInvokeProxy CPH, TeamData leftTeam, TeamData rightTeam,
            int startingStocks = 0)
        {
            CPH.LogDebug("Updating game Id");
            PathManager.UpdateGameId(CPH);
            CPH.LogDebug("Updating TSH config file");
            ResetFromTSHFile();
            CPH.LogDebug("Fuzzy search");
            _charactersFuzzySearch.UpdateGame(PathManager);
            SetupTeams(CPH, leftTeam, rightTeam, startingStocks);
            UpdateCurrentMatch(CPH);
            //UpdateListData(CPH);
            RefreshStreamDeck(CPH, StreamDeckSections.BothTeams | StreamDeckSections.StartingSections );
        }

        public void SelectCharacter(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            CharacterButtonPressed(CPH, pressedButtonId);
            UpdateCurrentMatch(CPH);
        }

        /// <summary>
        /// Switches the character state. From Selected to Used or from Used to Unselected 
        /// </summary>
        /// <param name="CPH">Streamer bot interface</param>
        /// <param name="pressedButtonId">Streamdeck button that was pressed</param>
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
        
        /// <summary>
        /// Refresh stocks both on stream deck and TSH
        /// </summary>
        /// <param name="CPH"></param>
        /// <param name="isLeftTeam">The leftTeam is being updated, rightTeam otherwise</param>
        /// <param name="stocksAdd">How much to add</param>
        public void UpdateStocks(IInlineInvokeProxy CPH, bool isLeftTeam, int stocksAdd = -1)
        {
            CurrentCharacterButtonPressed(CPH, stocksAdd, isLeftTeam);
            UpdateCurrentMatch(CPH);
        }

        /// <summary>
        /// Refresh characters both on stream deck and TSH
        /// </summary>
        /// <param name="CPH">Streamerbot interface</param>
        public void RefreshCharacters(IInlineInvokeProxy CPH)
        {
            RefreshStreamDeck(CPH, StreamDeckSections.Characters | StreamDeckSections.BothTeams);
            UpdateCurrentMatch(CPH);
        }

        /// <summary>
        /// Adds a new player to the selected team.
        /// If a player with an existing id is added, the player will be replaced
        /// </summary>
        /// <param name="CPH">Streamerbot interface</param>
        /// <param name="teamMember">Team member to add to the team</param>
        /// <param name="isLeftTeam">Add to left team, right otherwise</param>
        public void AddPlayerToTeam(IInlineInvokeProxy CPH, TeamMemberData teamMember, bool isLeftTeam)
        {
            var nickname = CPH.GetTwitchUserVarById<string>(teamMember.Id, "nickname");
            var characters = teamMember.Characters.Select(c => _charactersFuzzySearch.SelectCharacterFuzzy(c))
                .ToList();
            var teamUserInfo = new TeamUserInfo(CPH.TwitchGetExtendedUserInfoById(teamMember.Id),
                nickname, characters);
            var selectedTeam = isLeftTeam ? _teamLeft : _teamRight;
            
            if (selectedTeam.TeamMembers.ContainsKey(teamMember.Id))
            {
                selectedTeam.TeamMembers.Remove(teamMember.Id);
            }
            selectedTeam.TeamMembers.Add(teamMember.Id, teamUserInfo);
            selectedTeam.OrderedPlayers.Add(teamMember.Id);
            CPH.SetTwitchUserVarById(teamMember.Id, "squadRoster", characters);
            CPH.SetTwitchUserVarById(teamMember.Id, "originalSquadRoster", characters);
            
        }

        /// <summary>
        /// Overrides a user's characters list
        /// </summary>
        /// <param name="userId">The user to replace the characters</param>
        /// <param name="characters">Characters that will replace the current roster</param>
        /// <returns>True if any user from any team was updated. If no user was found this returns false</returns>
        public bool OverridePlayerCharacters(string userId, List<string> characters)
        {
            bool isLeftTeam = false;
            if (_teamLeft.TeamMembers.ContainsKey(userId))
            {
                isLeftTeam = true;
            }
            else if (!_teamRight.TeamMembers.ContainsKey(userId))
            {
                // Reaching this point means that we don't have that user in both teams
                return false;
            }

            var selectedTeam = isLeftTeam ? _teamLeft : _teamRight;
            selectedTeam.TeamMembers[userId].Characters = _charactersFuzzySearch.SelectCharacterFuzzy(characters);
            return true;
        }

        public string GetCredits()
        {
            return $"{GetCreditsForTeam(_teamLeft)}\n vs \n{GetCreditsForTeam(_teamLeft)}";

            string GetCreditsForTeam(TeamInfo team)
            {
                var builder = new StringBuilder();
                if (team.TeamMembers.Count > 1)
                {
                    builder.AppendLine(team.TeamName);
                }

                //Add all team members to the credits
                builder.Append(string.Join("\n", 
                    team.TeamMembers.Select(kv => kv.Value.UserName)));

                return builder.ToString();
            }
        }
    }
}