using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;
using SBCustomClasses.TSH.Base;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Model;

namespace SBCustomClasses.StreamDeck
{
    public partial class StreamDeckTSHConnection
    {
        private readonly StreamDeckConfiguration _streamDeckConfiguration;
        private TeamInfo _teamLeft;
        private TeamInfo _teamRight;
        private Dictionary<string, StreamDeckSmashCharacterButtonState> _buttonCharacterDict;

        private CharacterFuzzyTools _charactersFuzzySearch;
        
        /// <summary>
        /// Initialize the teams with players and characters roster for each player.
        /// Characters can be "dirty", a fuzzy search will be made on a list of
        /// SSBU characters ready for TSH
        /// </summary>
        /// <param name="CPH">Streamer bot connection</param>
        /// <param name="leftTeam">Left team players with characters</param>
        /// <param name="rightTeam">Right team players with characters</param>
        /// <param name="startingStocks">How many stocks will the teams start with (default is 0)</param>
        /// <param name="showFirsts">Show the first players of each team on the stream deck (default is true)</param>
        private void SetupTeams(IInlineInvokeProxy CPH, TeamData leftTeam, TeamData rightTeam, int startingStocks = 0)
        {
            _teamLeft = SetIds(leftTeam.TeamMembers);
            _teamRight = SetIds(rightTeam.TeamMembers);
            _teamLeft.Stocks = startingStocks;
            _teamRight.Stocks = startingStocks;
            _teamLeft.TeamName = leftTeam.Name;
            _teamRight.TeamName = rightTeam.Name;

            if (_teamLeft.TeamMembers.Count == 0 || _teamRight.TeamMembers.Count == 0)
                return;

            _teamLeft.CurrentSelectedPlayerId = _teamLeft.TeamMembers.First().Key;
            _teamRight.CurrentSelectedPlayerId = _teamRight.TeamMembers.First().Key;
            _buttonCharacterDict = new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            InitializePlayersState(CPH);
            InitializeCharactersState(CPH);
            CPH.SetGlobalVar("streamDeckCharacterButtons", _buttonCharacterDict);
            return;

            TeamInfo SetIds(IEnumerable<TeamMemberData> team)
            {
                var result = new TeamInfo();
                foreach (var teamMember in team)
                {
                    var nickname = CPH.GetTwitchUserVarById<string>(teamMember.Id, "nickname");
                    // Get clean character names
                    var characters = teamMember.Characters.Select(c => _charactersFuzzySearch.SelectCharacterFuzzy(c))
                        .ToList();
                    var teamUserInfo = new TeamUserInfo(CPH.TwitchGetExtendedUserInfoById(teamMember.Id),
                        nickname, characters);
                    result.TeamMembers.Add(teamMember.Id, teamUserInfo);
                    result.OrderedPlayers.Add(teamMember.Id);
                    CPH.SetTwitchUserVarById(teamMember.Id, "squadRoster", characters);
                    CPH.SetTwitchUserVarById(teamMember.Id, "originalSquadRoster", characters);
                }

                return result;
            }
        }

        private void InitializePlayersState(IInlineInvokeProxy CPH)
        {
            SetupPlayerFor(_teamLeft, _streamDeckConfiguration.Buttons.LeftTeamButtons);
            SetupPlayerFor(_teamRight, _streamDeckConfiguration.Buttons.RightTeamButtons);
            return;
            
            void SetupPlayerFor(TeamInfo team, IEnumerable<string> buttonsId)
            {
                foreach (var zip in buttonsId.Zip(team.TeamMembers,
                             (buttonId, player) => new { buttonId, player }))
                {
                    var buttonState = new StreamDeckSmashCharacterButtonState
                    {
                        ButtonId = zip.buttonId,
                        Character = zip.player.Value.Characters.FirstOrDefault(),
                        UserId = zip.player.Key,
                        SelectedState = SelectionState.Unselected,
                        ColorState = _streamDeckConfiguration.Theming.UnselectedColor
                    };
                    _buttonCharacterDict.Add(zip.buttonId, buttonState);
                }
            }
        }

        private void InitializeCharactersState(IInlineInvokeProxy CPH)
        {
            SetupCharacterFor(_teamLeft.CurrentSelectedPlayer, _streamDeckConfiguration.Buttons.LeftCharacterButtons);
            SetupCharacterFor(_teamRight.CurrentSelectedPlayer, _streamDeckConfiguration.Buttons.RightCharacterButtons);
            return;

            void SetupCharacterFor(TeamUserInfo user, IEnumerable<string> buttonsId)
            {
                foreach (var zip in buttonsId.Zip(user.Characters,
                             (buttonId, character) => new { buttonId, character }))
                {
                    var buttonState = new StreamDeckSmashCharacterButtonState
                    {
                        ButtonId = zip.buttonId,
                        Character = zip.character,
                        UserId = user.TwitchUserInfo.UserId,
                        SelectedState = SelectionState.Unselected,
                        ColorState = _streamDeckConfiguration.Theming.UnselectedColor
                    };
                    _buttonCharacterDict.Add(zip.buttonId, buttonState);
                }
            }
        }

        private void CharacterButtonPressed(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            if (!_buttonCharacterDict.TryGetValue(pressedButtonId, out var state))
            {
                return;
            }

            TeamInfo currentTeam;
            StreamDeckSections sectionsToUpdate = StreamDeckSections.Characters | StreamDeckSections.CurrentPlayerCharacter;
            if (_teamLeft.TeamMembers.ContainsKey(state.UserId))
            {
                currentTeam = _teamLeft;
                sectionsToUpdate |= StreamDeckSections.TeamLeft;
            }
            else if (_teamRight.TeamMembers.ContainsKey(state.UserId))
            {
                currentTeam = _teamRight;
                sectionsToUpdate |= StreamDeckSections.TeamRight;
            }
            else
            {
                return;
            }
            
            if (!string.IsNullOrEmpty(currentTeam.LastSelectedCharacterButtonId) && 
                _buttonCharacterDict.TryGetValue(currentTeam.LastSelectedCharacterButtonId, out var previousState))
            {
                previousState.ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor;
                previousState.SelectedState = SelectionState.Disabled;
                currentTeam.TeamMembers[state.UserId].Characters.Remove(previousState.Character);
                currentTeam.TeamMembers[state.UserId].Characters.Add("");
            }

            if (currentTeam.LastSelectedCharacterButtonId != pressedButtonId)
            {
                state.SelectedState = SelectionState.Selected;
                state.ColorState = _streamDeckConfiguration.Theming.SelectedColor;
                currentTeam.TeamMembers[state.UserId].Characters.Remove(state.Character);
                currentTeam.TeamMembers[state.UserId].Characters.Insert(0, state.Character);
            }
            else
            {
                state.SelectedState = SelectionState.Unselected;
                state.ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor;
                currentTeam.TeamMembers[state.UserId].Characters.Remove(state.Character);
                currentTeam.TeamMembers[state.UserId].Characters.Add("");
            }

            currentTeam.LastSelectedCharacterButtonId = pressedButtonId;
            CPH.SetGlobalVar("streamDeckCharacterButtons", _buttonCharacterDict);
            var eventTeams = new[] { _teamLeft, _teamRight };
            CPH.LogDebug($"Teams (Stock update): \n{JsonConvert.SerializeObject(eventTeams)}");
            RefreshStreamDeck(CPH, sectionsToUpdate);
        }
        
        private void PlayerButtonPressed(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            if (!_buttonCharacterDict.TryGetValue(pressedButtonId, out var state))
            {
                return;
            }

            TeamInfo currentTeam;
            var sectionsToUpdate = StreamDeckSections.TeamMembers | StreamDeckSections.Characters
                | StreamDeckSections.CurrentPlayerPFP | StreamDeckSections.CurrentPlayerCharacter;
            List<string> characterButtons;
            if (_teamLeft.TeamMembers.ContainsKey(state.UserId))
            {
                currentTeam = _teamLeft;
                sectionsToUpdate |= StreamDeckSections.TeamLeft;
                characterButtons = _streamDeckConfiguration.Buttons.LeftCharacterButtons;
            }
            else if (_teamRight.TeamMembers.ContainsKey(state.UserId))
            {
                currentTeam = _teamRight;
                sectionsToUpdate |= StreamDeckSections.TeamRight;
                characterButtons = _streamDeckConfiguration.Buttons.RightCharacterButtons;
            }
            else
            {
                return;
            }
            
            if (!string.IsNullOrEmpty(currentTeam.LastSelectedPlayerButtonId) && 
                _buttonCharacterDict.TryGetValue(currentTeam.LastSelectedPlayerButtonId, out var previousState))
            {
                previousState.ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor;
                previousState.SelectedState = SelectionState.Disabled;
                currentTeam.OrderedPlayers.Remove(state.UserId);
                currentTeam.OrderedPlayers.Add("");
            }

            if (currentTeam.LastSelectedPlayerButtonId != pressedButtonId)
            {
                state.SelectedState = SelectionState.Selected;
                state.ColorState = _streamDeckConfiguration.Theming.SelectedColor;
                currentTeam.OrderedPlayers.Remove(state.UserId);
                currentTeam.OrderedPlayers.Insert(0, state.UserId);
                currentTeam.CurrentSelectedPlayerId = state.UserId;
                foreach(var buttonId in characterButtons)
                {
                    _buttonCharacterDict.Remove(buttonId);
                }
                
                foreach (var zip in characterButtons.Zip(currentTeam.CurrentSelectedPlayer.Characters,
                             (buttonId, character) => new { buttonId, character }))
                {
                    var buttonState = new StreamDeckSmashCharacterButtonState
                    {
                        ButtonId = zip.buttonId,
                        Character = zip.character,
                        UserId = currentTeam.CurrentSelectedPlayer.TwitchUserInfo.UserId,
                        SelectedState = SelectionState.Unselected,
                        ColorState = _streamDeckConfiguration.Theming.UnselectedColor
                    };
                    _buttonCharacterDict.Add(zip.buttonId, buttonState);
                }
            }
            else
            {
                state.SelectedState = SelectionState.Unselected;
                state.ColorState = _streamDeckConfiguration.Theming.UnselectedColor;
            }

            currentTeam.LastSelectedPlayerButtonId = pressedButtonId;
            CPH.SetGlobalVar("streamDeckCharacterButtons", _buttonCharacterDict);
            RefreshStreamDeck(CPH, sectionsToUpdate);
        }
        
        private void CurrentCharacterButtonPressed(IInlineInvokeProxy CPH, int stockAdd, bool leftTeam)
        {
            var side = leftTeam ? "Right" : "Left";
            CPH.LogInfo($"Updating stocks: {side} team");
            TeamInfo team;
            var streamDeckSection = StreamDeckSections.CurrentStocks;
            if (leftTeam)
            {
                team = _teamLeft;
                streamDeckSection |= StreamDeckSections.TeamLeft;
            }
            else
            {
                team = _teamRight;
                streamDeckSection |= StreamDeckSections.TeamRight;
            }
            
            team.Stocks += stockAdd;
            var eventTeams = new[] { _teamLeft, _teamRight };
            CPH.LogDebug($"Teams (Stock update): \n{JsonConvert.SerializeObject(eventTeams)}");
            RefreshStreamDeck(CPH, streamDeckSection);
        }

        #region StreamDeck Refresh

        // ReSharper disable once MemberCanBePrivate.Global
        internal void RefreshStreamDeck(IInlineInvokeProxy CPH, StreamDeckSections updateParts)
        {
            CPH.LogInfo($"Refreshing stream deck: {updateParts}");
            #region PFP

            if (updateParts.HasFlag(StreamDeckSections.CurrentPlayerPFP | StreamDeckSections.TeamLeft))
                RefreshPortraits(CPH, _streamDeckConfiguration.Buttons.LeftButtonId,
                    _streamDeckConfiguration.Buttons.LeftPlayerPortraits,
                    _teamLeft.CurrentSelectedPlayer.TwitchUserInfo.ProfileImageUrl, _teamLeft.CurrentSelectedPlayer.UserName);

            if (updateParts.HasFlag(StreamDeckSections.CurrentPlayerPFP | StreamDeckSections.TeamRight))
                RefreshPortraits(CPH, _streamDeckConfiguration.Buttons.RightButtonId,
                    _streamDeckConfiguration.Buttons.RightPlayerPortraits,
                    _teamRight.CurrentSelectedPlayer.TwitchUserInfo.ProfileImageUrl, _teamRight.CurrentSelectedPlayer.UserName);

            #endregion

            #region Characters

            if (updateParts.HasFlag(StreamDeckSections.Characters | StreamDeckSections.TeamLeft))
                RefreshCharacters(CPH, _streamDeckConfiguration.Buttons.LeftCharacterButtons);

            if (updateParts.HasFlag(StreamDeckSections.Characters | StreamDeckSections.TeamRight))
                RefreshCharacters(CPH, _streamDeckConfiguration.Buttons.RightCharacterButtons);

            #endregion

            #region Current Player Character

            if (updateParts.HasFlag(StreamDeckSections.CurrentPlayerCharacter | StreamDeckSections.TeamLeft))
            {
                RefreshCurrentSelectedCharacter(CPH, _streamDeckConfiguration.Buttons.LeftStocksButtonId, _teamLeft);
            }
            if (updateParts.HasFlag(StreamDeckSections.CurrentPlayerCharacter | StreamDeckSections.TeamRight))
            {
                RefreshCurrentSelectedCharacter(CPH, _streamDeckConfiguration.Buttons.RightStocksButtonId, _teamRight);
            }

            #endregion

            #region Stocks

            if (updateParts.HasFlag(StreamDeckSections.CurrentStocks | StreamDeckSections.TeamLeft))
                RefreshStocks(CPH, _streamDeckConfiguration.Buttons.LeftStocksButtonId, _teamLeft.Stocks);

            if (updateParts.HasFlag(StreamDeckSections.CurrentStocks | StreamDeckSections.TeamRight))
                RefreshStocks(CPH, _streamDeckConfiguration.Buttons.RightStocksButtonId, _teamRight.Stocks);

            #endregion

            #region Team Members

            if (updateParts.HasFlag(StreamDeckSections.TeamMembers | StreamDeckSections.TeamLeft))
            {
                RefreshPlayers(CPH, _streamDeckConfiguration.Buttons.LeftTeamButtons);
            }
            
            if (updateParts.HasFlag(StreamDeckSections.TeamMembers | StreamDeckSections.TeamRight))
            {
                RefreshPlayers(CPH, _streamDeckConfiguration.Buttons.RightTeamButtons);
            }

            #endregion
            CPH.LogInfo($"Refreshed stream deck: {updateParts}");
        }

        private void RefreshCharacters(IInlineInvokeProxy CPH, IEnumerable<string> characterButtonIds)
        {
            foreach (var id in characterButtonIds)
            {
                if (!_buttonCharacterDict.TryGetValue(id, out var buttonState)) return;

                var characterProfileFile = GetImageFilePath(CPH, buttonState.Character, buttonState.UserId);
                CPH.StreamDeckSetBackgroundLocal(id, characterProfileFile, buttonState.ColorState);
            }
        }
        
        private void RefreshPlayers(IInlineInvokeProxy CPH, IEnumerable<string> characterButtonIds)
        {
            foreach (var id in characterButtonIds)
            {
                if (!_buttonCharacterDict.TryGetValue(id, out var buttonState)) return;

                if (_teamLeft.TeamMembers.TryGetValue(buttonState.UserId, out var user) ||
                    _teamRight.TeamMembers.TryGetValue(buttonState.UserId, out user))
                {
                    CPH.StreamDeckSetBackgroundUrl(id, user.TwitchUserInfo.ProfileImageUrl, buttonState.ColorState);
                    return;
                }
                
                var characterProfileFile = GetImageFilePath(CPH, buttonState.Character, buttonState.UserId);
                CPH.StreamDeckSetBackgroundLocal(id, characterProfileFile, buttonState.ColorState);
            }
        }

        private void RefreshPortraits(IInlineInvokeProxy CPH, string buttonId,
            IEnumerable<string> otherPortraitsButtonId, string profileImageUrl, string userName)
        {
            CPH.LogInfo($"Refreshing portrait: {buttonId}");
            CPH.StreamDeckSetBackgroundUrl(buttonId, profileImageUrl);
            CPH.StreamDeckSetTitle(buttonId, userName);
            foreach (var id in otherPortraitsButtonId)
            {
                CPH.StreamDeckSetBackgroundUrl(id, profileImageUrl);
                CPH.StreamDeckSetTitle(id, userName);
            }
        }
        
        private void RefreshStocks(IInlineInvokeProxy CPH, string buttonId, int stocks)
        {
            CPH.StreamDeckSetTitle(buttonId, $"{stocks}");
        }

        private void RefreshCurrentSelectedCharacter(IInlineInvokeProxy CPH, string buttonId, TeamInfo team)
        {
            if (!_buttonCharacterDict.TryGetValue(team.LastSelectedCharacterButtonId, out var lastButtonState))
            {
                CPH.StreamDeckSetBackgroundColor(buttonId, _streamDeckConfiguration.Theming.UnselectedColor);
                return;
            }

            var characterFilePath = GetImageFilePath(CPH, lastButtonState.Character,
                team.CurrentSelectedPlayer.TwitchUserInfo.UserId);
            CPH.StreamDeckSetBackgroundLocal(buttonId, characterFilePath);
        }

        private string GetImageFilePath(IInlineInvokeProxy CPH, string characterStartGgName, string userId)
        {
            CPH.LogInfo($"Getting info for {characterStartGgName}");
            var selectedTheme = CPH.GetGlobalVar<string>("smashIcons");
            CPH.LogInfo($"Getting info for {selectedTheme}");
            var buttonIcons = _streamDeckConfiguration.Theming.GetButtonsIcons(selectedTheme);
            CPH.LogInfo(
                $"Info from {selectedTheme}: {buttonIcons.Path}/{buttonIcons.Filename} {buttonIcons.GetCompletePath(characterStartGgName, 0)}");
            var path = buttonIcons.DefaultPath;
            var skinIndex = 0;
            if (!string.IsNullOrEmpty(userId))
                skinIndex = CPH.GetTwitchUserVarById<int>(userId, $"skin_{characterStartGgName}");
            if (_smashGameInfo.CharacterToCodename.TryGetValue(characterStartGgName.Trim(), out var result))
            {
                var imagePath = buttonIcons.GetCompletePath(result.Codename, skinIndex);
                if (File.Exists(imagePath))
                {
                    CPH.LogInfo($"{result.Codename} was found! {imagePath}");
                    path = imagePath;
                }
                else
                {
                    CPH.LogInfo($"Codename {result} found but file was not found {imagePath}");
                }
            }
            else
            {
                CPH.LogInfo($"{characterStartGgName} was not found");
            }

            CPH.LogInfo($"{characterStartGgName} - Returning path: {path}");
            return path;
        }

        public bool ResetStreamDeckButtons(IInlineInvokeProxy CPH)
        {
            CPH.LogInfo("Reset stream deck buttons");
            var buttonsLayout = _streamDeckConfiguration.Buttons.AllButtons;

            foreach (var id in buttonsLayout)
            {
                CPH.StreamDeckSetTitle(id, "");
                CPH.StreamDeckSetBackgroundColor(id, "#00000000");
            }
            return true;
        }
        #endregion
    }
}