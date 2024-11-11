using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Model;

namespace StreamerBotScriptActions.CrewBattle.CrewBattleStreamDeckSetup;

using SBCustomClasses.TSH.Base;
using SBCustomClasses.StreamDeck;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.StreamDeck.Configuration;

public class CPHInline : CPHInlineBase
{
    private BaseGameInfo _smashGameInfo;
    private StreamDeckConfiguration _streamDeckConfiguration;
    private const string SmashConfigurationFilePath =
        "D:/Streams/TournamentStreamHelper/user_data/games/ssbu/base_files/config.json";
    
    private const string CrewBattleTeamNameRight = "Crew Battle Team Right";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Left";

    public bool Execute()
    {
        //if(_streamDeckConfiguration == null)
        {
            var json = File.ReadAllText(
                "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json");
            _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(json);
        }
        var smashFileContent = File.ReadAllText(SmashConfigurationFilePath);
        _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(smashFileContent);
        return SetupStreamDeck();
    }

    public bool SetupStreamDeck()
    {
        var teamNames = new List<string>
        {
            CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft),
            CPH.GetGlobalVar<string>(CrewBattleTeamNameRight)
        };
        
        var teamRosters = new List<IEnumerable<string>>
        {
            CPH.UsersInGroup(CrewBattleTeamNameLeft).Select(u => u.Id),
            CPH.UsersInGroup(CrewBattleTeamNameRight).Select(u => u.Id),
        };

        var streamDeckPlayerButtons = new List<List<string>>
        {
            _streamDeckConfiguration.Buttons.LeftTeamButtons,
            _streamDeckConfiguration.Buttons.RightTeamButtons
        };
        
        var buttonPlayerDict = 
            CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckPlayerButtons") 
            ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();

        for (var index = 0; index < teamRosters.Count; index++)
        {
            var currentRoster = teamRosters[index];
            var currentStreamDeckButtons = streamDeckPlayerButtons[index];
            var streamDeckIndex = 0;
            foreach (var playerId in currentRoster)
            {
                var twitchUser = CPH.TwitchGetExtendedUserInfoById(playerId);
                CPH.StreamDeckSetBackgroundUrl(currentStreamDeckButtons[streamDeckIndex], twitchUser.ProfileImageUrl);
                buttonPlayerDict[currentStreamDeckButtons[streamDeckIndex]] = new StreamDeckSmashCharacterButtonState()
                {
                    Character = twitchUser.UserName,
                    UserId = twitchUser.UserId,
                    ButtonId = currentStreamDeckButtons[streamDeckIndex],
                    SelectedState = SelectionState.Unselected,
                    ColorState = _streamDeckConfiguration.Theming.UnselectedColor
                };
                streamDeckIndex++;
            }

            for (var i = streamDeckIndex; i < currentStreamDeckButtons.Count; i++)
            {
                buttonPlayerDict[currentStreamDeckButtons[i]] = new StreamDeckSmashCharacterButtonState
                {
                    ButtonId = currentStreamDeckButtons[i],
                    ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor
                };
            }
        }

        var buttonCharacterDict =
            CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons")
            ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();

        var streamDeckCharacterButtons = new List<List<string>>
        {
            _streamDeckConfiguration.Buttons.LeftTeamButtons,
            _streamDeckConfiguration.Buttons.RightTeamButtons,
        };

        var groups = new List<List<GroupUser>>
        {
            CPH.UsersInGroup(CrewBattleTeamNameLeft),
            CPH.UsersInGroup(CrewBattleTeamNameRight),
        };

        for (var index = 0; index < streamDeckCharacterButtons.Count; index++)
        {
            var currentButtonsLayout = streamDeckCharacterButtons[index];
            var currentGroup = groups[index];
            for (var i = 0; i < currentButtonsLayout.Count; i++)
            {
                var currentButton = currentButtonsLayout[i];
                var value = new StreamDeckSmashCharacterButtonState()
                {
                    ColorState = _streamDeckConfiguration.Theming.AlreadyUsedColor,
                    Character = "",
                    ButtonId = currentButton,
                    SelectedState = SelectionState.Disabled,
                    UserId = i < currentGroup.Count ? currentGroup[i].Id : "",
                };
                buttonCharacterDict[currentButton] = value;
                CPH.StreamDeckSetBackgroundColor(currentButton, value.ColorState);
            }
        }

        CPH.StreamDeckSetBackgroundColor(_streamDeckConfiguration.Buttons.LeftStocksButtonId, _streamDeckConfiguration.Theming.UnselectedColor);
        CPH.StreamDeckSetBackgroundColor(_streamDeckConfiguration.Buttons.RightStocksButtonId, _streamDeckConfiguration.Theming.UnselectedColor);
        
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.LeftStocksButtonId, CPH.GetGlobalVar<int>($"{CrewBattleTeamNameLeft} Stocks").ToString());
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.RightStocksButtonId, CPH.GetGlobalVar<int>($"{CrewBattleTeamNameRight} Stocks").ToString());
        
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.LeftButtonId, teamNames[0]);
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.RightButtonId, teamNames[1]);
        CPH.StreamDeckSetBackgroundColor(_streamDeckConfiguration.Buttons.LeftButtonId, "#ff584d"); // red
        CPH.StreamDeckSetBackgroundColor(_streamDeckConfiguration.Buttons.RightButtonId, "#4569d6"); // blue
        
        CPH.SetGlobalVar("streamDeckCharacterButtons", buttonCharacterDict);
        CPH.SetGlobalVar("streamDeckPlayerButtons", buttonPlayerDict);

        return true;
    }

    public bool UpdateStreamDeck()
    {
        var buttonPlayerDict = 
            CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckPlayerButtons") 
            ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();
        
        var buttonCharacterDict = 
            CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons") 
            ?? new Dictionary<string, StreamDeckSmashCharacterButtonState>();

        CPH.LogDebug($"Updating player dict {buttonPlayerDict.Count}");
        foreach (var buttonInfo in buttonPlayerDict)
        {
            if (buttonInfo.Value == null || string.IsNullOrEmpty(buttonInfo.Value.UserId))
            {
                CPH.LogDebug($"Invalid user, setting default {buttonInfo.Key}");
                CPH.StreamDeckSetTitle(buttonInfo.Key, "");
                CPH.StreamDeckSetBackgroundColor(buttonInfo.Key, _streamDeckConfiguration.Theming.AlreadyUsedColor);
                continue;
            }
            CPH.LogDebug("Getting twitch user");
            var twitchUser = CPH.TwitchGetExtendedUserInfoById(buttonInfo.Value.UserId);
            CPH.StreamDeckSetTitle(buttonInfo.Key, buttonInfo.Value.Character);
            CPH.StreamDeckSetBackgroundUrl(buttonInfo.Key, twitchUser.ProfileImageUrl);
        }

        CPH.LogDebug("Updating character dict");
        foreach (var buttonInfo in buttonCharacterDict)
        {
            CPH.StreamDeckSetBackgroundLocal(buttonInfo.Key, GetImageFilePath(buttonInfo.Value.Character, buttonInfo.Value.UserId), buttonInfo.Value.ColorState);
        }


        var currentPlayers = new List<string>
        {
            CPH.GetGlobalVar<string>("leftSelectedPlayerId"),
            CPH.GetGlobalVar<string>("rightSelectedPlayerId")
        };
        
        var currentPlayerCharacters = new List<string>
        {
            CPH.GetGlobalVar<string>("leftSelectedPlayerCharacter"),
            CPH.GetGlobalVar<string>("rightSelectedPlayerCharacter")
        };
        
        var portraitButtons = new List<List<string>>
        {
            _streamDeckConfiguration.Buttons.LeftPlayerPortraits,
            _streamDeckConfiguration.Buttons.RightPlayerPortraits
        };

        var stockButtons = new List<string>
        {
            _streamDeckConfiguration.Buttons.LeftStocksButtonId,
            _streamDeckConfiguration.Buttons.RightStocksButtonId
        };

        var teamButtons = new List<string>
        {
            _streamDeckConfiguration.Buttons.LeftButtonId,
            _streamDeckConfiguration.Buttons.RightButtonId
        };

        var teamColors = new List<string>
        {
            "#ff584d", // red
            "#4569d6" // blue
        };
        CPH.LogDebug("Updating users buttons");
        for (var index = 0; index < 2; index++)
        {
            var userId = currentPlayers[index];
            var character = currentPlayerCharacters[index];
            var stockButtonId = stockButtons[index];
            var teamButtonId = teamButtons[index];
            var portraitButtonIds = portraitButtons[index];
            var color = teamColors[index];
            
            if (string.IsNullOrEmpty(userId))
            {
                CPH.LogDebug("User not selected");
                CPH.StreamDeckSetBackgroundColor(stockButtonId, _streamDeckConfiguration.Theming.UnselectedColor);
                CPH.StreamDeckSetBackgroundColor(teamButtonId, color);
                continue;
            }
            CPH.LogDebug("User selected");
            var userInfo = CPH.TwitchGetExtendedUserInfoById(userId);
            CPH.StreamDeckSetBackgroundUrl(teamButtonId, userInfo.ProfileImageUrl);
            CPH.LogDebug("Setting player's character");
            if(!string.IsNullOrEmpty(character))
            {
                CPH.StreamDeckSetBackgroundUrl(stockButtonId, GetImageFilePath(character, userId));
            }
            else
            {
                CPH.StreamDeckSetBackgroundColor(stockButtonId, _streamDeckConfiguration.Theming.UnselectedColor);
            }
            CPH.LogDebug("Setting portraits");
            foreach (var portraitButtonId in portraitButtonIds)
            {
                CPH.StreamDeckSetBackgroundUrl(portraitButtonId, userInfo.ProfileImageUrl);
            }
        }

        CPH.LogDebug("Setting stocks");
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.LeftStocksButtonId, CPH.GetGlobalVar<int>($"{CrewBattleTeamNameLeft} Stocks").ToString());
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.RightStocksButtonId, CPH.GetGlobalVar<int>($"{CrewBattleTeamNameRight} Stocks").ToString());
        return true;
    }
    
    private string GetImageFilePath(string characterStartGgName, string userId)
    {
        CPH.LogInfo($"Getting info for {characterStartGgName}");
        var selectedTheme = CPH.GetGlobalVar<string>("smashIcons");
        CPH.LogInfo($"Getting info for {selectedTheme}");
        var currentGame = CPH.GetGlobalVar<string>("CurrentGameId");
        var buttonIcons = _streamDeckConfiguration.Theming.GetButtonsIcons(currentGame, selectedTheme, out bool found);
        CPH.LogInfo($"Info from {selectedTheme}: {buttonIcons.Path}/{buttonIcons.Filename} {buttonIcons.GetCompletePath(characterStartGgName, 0)}");
        var path = buttonIcons.DefaultPath;
        var skinIndex = 0;
        if (!string.IsNullOrEmpty(userId))
        {
            skinIndex = CPH.GetTwitchUserVarById<int>(userId, $"skin_{characterStartGgName}");
        }
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
}