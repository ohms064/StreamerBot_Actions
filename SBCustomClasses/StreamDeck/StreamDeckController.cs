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
        #region Constants

        // TODO: Maybe have this as a config file
        private const string SmashFileContent =
            "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json";

        private const string StreamDeckConfig =
            "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json";
        #endregion

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

            if (_teamLeft.Count == 0 || _teamRight.Count == 0)
                return;

            _teamLeft.CurrentSelectedPlayerId = _teamLeft.First().Key;
            _teamRight.CurrentSelectedPlayerId = _teamRight.First().Key;
            _buttonCharacterDict = new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            InitializePlayersState(CPH);
            InitializeCharactersState(CPH);
            CPH.SetGlobalVar("streamDeckCharacterButtons", _buttonCharacterDict);
            RefreshStreamDeck(CPH,
                StreamDeckSections.BothTeams & StreamDeckSections.CurrentPlayerPFP & StreamDeckSections.Characters);
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
                    result.Add(teamMember.Id, teamUserInfo);
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
                foreach (var zip in buttonsId.Zip(team,
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
            StreamDeckSections sectionsToUpdate = StreamDeckSections.Characters;
            if (_teamLeft.ContainsKey(state.UserId))
            {
                currentTeam = _teamLeft;
                sectionsToUpdate &= StreamDeckSections.TeamLeft;
            }
            else if (_teamRight.ContainsKey(state.UserId))
            {
                currentTeam = _teamRight;
                sectionsToUpdate &= StreamDeckSections.TeamRight;
            }
            else
            {
                return;
            }
            
            if (string.IsNullOrEmpty(currentTeam.LastSelectedCharacterButtonId) && 
                _buttonCharacterDict.TryGetValue(currentTeam.LastSelectedCharacterButtonId, out var previousState))
            {
                previousState.ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor;
                previousState.SelectedState = SelectionState.Disabled;
                currentTeam[state.UserId].Characters.Remove(previousState.Character);
                currentTeam[state.UserId].Characters.Add("");
            }

            if (currentTeam.LastSelectedCharacterButtonId != pressedButtonId)
            {
                state.SelectedState = SelectionState.Selected;
                state.ColorState = _streamDeckConfiguration.Theming.SelectedColor;
                currentTeam[state.UserId].Characters.Remove(state.Character);
                currentTeam[state.UserId].Characters.Insert(0, state.Character);
            }
            else
            {
                state.SelectedState = SelectionState.Unselected;
                state.ColorState = _streamDeckConfiguration.Theming.UnselectedColor;
            }

            currentTeam.LastSelectedCharacterButtonId = pressedButtonId;
            CPH.SetGlobalVar("streamDeckCharacterButtons", _buttonCharacterDict);
            RefreshStreamDeck(CPH, sectionsToUpdate);
        }
        
        private void PlayerButtonPressed(IInlineInvokeProxy CPH, string pressedButtonId)
        {
            if (!_buttonCharacterDict.TryGetValue(pressedButtonId, out var state))
            {
                return;
            }

            TeamInfo currentTeam;
            StreamDeckSections sectionsToUpdate = StreamDeckSections.TeamMembers;
            if (_teamLeft.ContainsKey(state.UserId))
            {
                currentTeam = _teamLeft;
                sectionsToUpdate &= StreamDeckSections.TeamLeft;
            }
            else if (_teamRight.ContainsKey(state.UserId))
            {
                currentTeam = _teamRight;
                sectionsToUpdate &= StreamDeckSections.TeamRight;
            }
            else
            {
                return;
            }
            
            if (string.IsNullOrEmpty(currentTeam.LastSelectedPlayerButtonId) && 
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

        #region StreamDeck Refresh

        // ReSharper disable once MemberCanBePrivate.Global
        public void RefreshStreamDeck(IInlineInvokeProxy CPH, StreamDeckSections updateParts)
        {
            #region PFP

            if ((updateParts & StreamDeckSections.CurrentPlayerPFP & StreamDeckSections.TeamLeft) != 0)
                RefreshPortraits(CPH, _streamDeckConfiguration.Buttons.LeftButtonId,
                    _streamDeckConfiguration.Buttons.LeftPlayerPortraits,
                    _teamLeft.CurrentSelectedPlayer.TwitchUserInfo.ProfileImageUrl, _teamLeft.CurrentSelectedPlayer.UserName);

            if ((updateParts & StreamDeckSections.CurrentPlayerPFP & StreamDeckSections.TeamRight) != 0)
                RefreshPortraits(CPH, _streamDeckConfiguration.Buttons.RightButtonId,
                    _streamDeckConfiguration.Buttons.RightPlayerPortraits,
                    _teamRight.CurrentSelectedPlayer.TwitchUserInfo.ProfileImageUrl, _teamRight.CurrentSelectedPlayer.UserName);

            #endregion

            #region Characters

            if ((updateParts & StreamDeckSections.Characters & StreamDeckSections.TeamLeft) != 0)
                RefreshCharacters(CPH, _streamDeckConfiguration.Buttons.LeftCharacterButtons);

            if ((updateParts & StreamDeckSections.Characters & StreamDeckSections.TeamRight) != 0)
                RefreshCharacters(CPH, _streamDeckConfiguration.Buttons.RightCharacterButtons);

            #endregion

            #region Current Player Character

            if ((updateParts & StreamDeckSections.CurrentPlayerCharacter & StreamDeckSections.TeamLeft) != 0)
                CPH.StreamDeckSetBackgroundLocal(_streamDeckConfiguration.Buttons.LeftStocksButtonId,
                    _teamLeft.CurrentSelectedPlayer.TwitchUserInfo.ProfileImageUrl);
            if ((updateParts & StreamDeckSections.CurrentPlayerCharacter & StreamDeckSections.TeamRight) != 0)
                CPH.StreamDeckSetBackgroundLocal(_streamDeckConfiguration.Buttons.RightStocksButtonId,
                    _teamRight.CurrentSelectedPlayer.TwitchUserInfo.ProfileImageUrl);

            #endregion

            #region Stocks

            if ((updateParts & StreamDeckSections.CurrentStocks & StreamDeckSections.TeamLeft) != 0)
                UpdateStocks(CPH, _streamDeckConfiguration.Buttons.LeftStocksButtonId, _teamLeft.Stocks);

            if ((updateParts & StreamDeckSections.CurrentStocks & StreamDeckSections.TeamRight) != 0)
                UpdateStocks(CPH, _streamDeckConfiguration.Buttons.RightStocksButtonId, _teamRight.Stocks);

            #endregion

            #region Team Members

            if ((updateParts & StreamDeckSections.TeamMembers & StreamDeckSections.TeamLeft) != 0)
            {
                RefreshPlayers(CPH, _streamDeckConfiguration.Buttons.LeftTeamButtons);
            }
            
            if ((updateParts & StreamDeckSections.TeamMembers & StreamDeckSections.TeamRight) != 0)
            {
                RefreshPlayers(CPH, _streamDeckConfiguration.Buttons.RightTeamButtons);
            }

            #endregion
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

                if (_teamLeft.TryGetValue(buttonState.UserId, out var user) ||
                    _teamRight.TryGetValue(buttonState.UserId, out user))
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
            CPH.StreamDeckSetBackgroundUrl(buttonId, profileImageUrl);
            CPH.StreamDeckSetTitle(buttonId, userName);
            foreach (var id in otherPortraitsButtonId) CPH.StreamDeckSetBackgroundUrl(id, profileImageUrl);
        }

        private void UpdateStocks(IInlineInvokeProxy CPH, string buttonId, int stocks)
        {
            CPH.StreamDeckSetTitle(buttonId, $"{stocks}");
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

        #endregion
    }
}