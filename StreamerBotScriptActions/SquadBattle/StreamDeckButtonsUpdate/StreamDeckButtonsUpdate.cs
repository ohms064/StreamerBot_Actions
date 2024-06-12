using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.StreamDeckButtonsUpdate;

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.TSH.Base;
using SBCustomClasses.StreamDeck;
using SBCustomClasses.StreamDeck.Configuration;

public class CPHInline : CPHInlineBase
{
    private const string SmashConfigurationFilePath =
        "D:/Streams/TournamentStreamHelper/user_data/games/ssbu/base_files/config.json";

    private BaseGameInfo _smashGameInfo;
    private StreamDeckConfiguration _streamDeckConfiguration;
    public void Init()
    {
        var json = File.ReadAllText(
            "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json");
        _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(json);
        CPH.LogInfo("Setting initial configurations for StreamDeck");
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.ContextButtonId, "");
        var smashFileContent = File.ReadAllText(SmashConfigurationFilePath);
        _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(smashFileContent);
    }

    public bool Execute()
    {
        //if(_streamDeckConfiguration == null)
        {
            var json = File.ReadAllText(
                "C:/Users/Ohms/RiderProjects/SBCustomClasses/SBCustomClasses/StreamDeck/Configuration/smash_events_configuration.json");
            _streamDeckConfiguration = JsonConvert.DeserializeObject<StreamDeckConfiguration>(json);
        }
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var twitchUserLeft = CPH.TwitchGetExtendedUserInfoById(eventUsers[0].Id);
        var twitchUserRight = CPH.TwitchGetExtendedUserInfoById(eventUsers[1].Id);
        var userNicknameLeft = CPH.GetTwitchUserVarById<string>(eventUsers[0].Id, "nickname");
        var userNameLeft = userNicknameLeft ?? twitchUserLeft.UserName;
        var userNicknameRight = CPH.GetTwitchUserVarById<string>(eventUsers[1].Id, "nickname");
        var userNameRight = userNicknameRight ?? twitchUserRight.UserName;
        CPH.StreamDeckSetBackgroundUrl(_streamDeckConfiguration.Buttons.LeftButtonId, twitchUserLeft.ProfileImageUrl);
        CPH.StreamDeckSetBackgroundUrl(_streamDeckConfiguration.Buttons.RightButtonId, twitchUserRight.ProfileImageUrl);
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.LeftButtonId, userNameLeft);
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.RightButtonId, userNameRight);
        return UpdateButtonStocks() && SetupCharacterPortraits();
    }

    public bool UpdateButtonStocks()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var userStocksLeft = CPH.GetTwitchUserVarById<int>(eventUsers[0].Id, "stocks");
        var userStocksRight = CPH.GetTwitchUserVarById<int>(eventUsers[1].Id, "stocks");
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.LeftStocksButtonId, $"{userStocksLeft}");
        CPH.StreamDeckSetTitle(_streamDeckConfiguration.Buttons.RightStocksButtonId, $"{userStocksRight}");
        return true;
    }

    public bool UpdatePlayerStocks()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        if (eventUsers.Count < 2)
        {
            CPH.LogDebug("User group is not complete");
            return false;
        }

        if (!CPH.TryGetArg("value2Add", out long value2AddLong))
        {
            CPH.LogDebug("value2Add was not set");
            return false;
        }

        var value2Add = (int)value2AddLong;
        if (!CPH.TryGetArg("userIndex", out long userIndexLong))
        {
            CPH.LogDebug("userIndex was not set");
            return false;
        }

        var userIndex = (int)userIndexLong;
        var userStocks = CPH.GetTwitchUserVarById<int>(eventUsers[userIndex].Id, "stocks");
        var resultStocks = userStocks + value2Add;
        CPH.SetTwitchUserVarById(eventUsers[userIndex].Id, "stocks", resultStocks);
        return UpdateButtonStocks();
    }

    public bool SetupCharacterPortraits()
    {
        CPH.LogInfo("Setting up Character Portraits");
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var characterButtonsLayout = new List<List<string>>
        {
            _streamDeckConfiguration.Buttons.LeftCharacterButtons,
            _streamDeckConfiguration.Buttons.RightCharacterButtons
        };
        if (eventUsers.Count != characterButtonsLayout.Count)
        {
            CPH.LogInfo("Error in Character Portraits");
            return false;
        }

        // This could be also saved on the user, but saving it on globals since this is a stream deck thing.
        var buttonCharacterDict = CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons");
        if (buttonCharacterDict == null)
        {
            buttonCharacterDict = new Dictionary<string, StreamDeckSmashCharacterButtonState>();
            CPH.LogInfo("Dict was null");
        }

        CPH.LogInfo("Character Portraits setting stream deck buttons");
        for (var i = 0; i < eventUsers.Count; i++)
        {
            var originalSquadRoster = CPH.GetTwitchUserVarById<List<string>>(eventUsers[i].Id, "originalSquadRoster");
            var buttonIndex = 0;
            CPH.UnsetTwitchUserVarById(eventUsers[i].Id, "lastSelectedCharacterButtons");
            CPH.LogInfo($"Setting buttons from index {i} {buttonIndex}");
            foreach (var character in originalSquadRoster)
            {
                var buttonId = characterButtonsLayout[i][buttonIndex];
                var imageFile = GetImageFilePath(character, eventUsers[i].Id);
                var color = GetCharacterButtonBackgroundColor(eventUsers[i].Id, character);
                CPH.StreamDeckSetBackgroundLocal(buttonId, imageFile, color);
                var title = GetCharacterButtonTitle(eventUsers[i].Id, character); 
                CPH.StreamDeckSetTitle(buttonId, title);

                buttonCharacterDict[buttonId] = new StreamDeckSmashCharacterButtonState
                {
                    SelectedState = SelectionState.Unselected,
                    Character = character,
                    UserId = eventUsers[i].Id,
                    ButtonId = buttonId
                };
                buttonIndex++;
                CPH.LogInfo($"Button {buttonId} for user {i} setup complete with character {character}");
            }

            CPH.LogInfo($"Cleaning remaining buttons from index {i} {buttonIndex}");
            // Clean the remaining buttons
            for (var j = buttonIndex; j < characterButtonsLayout[i].Count; j++)
            {
                var buttonId = characterButtonsLayout[i][j];
                CPH.StreamDeckSetBackgroundColor(buttonId, _streamDeckConfiguration.Theming.AlreadyUsedColor);
                buttonCharacterDict[buttonId] = new StreamDeckSmashCharacterButtonState
                {
                    ButtonId = buttonId
                };
                CPH.LogInfo($"Button {buttonId} for user {i} clean complete");
            }
            CPH.LogInfo($"Completing setting up {characterButtonsLayout[i].Count} buttons for {i}");
        }
        CPH.UnsetTwitchUserVarById(eventUsers[0].Id, "lastSelectedCharacterButtons");
        CPH.UnsetTwitchUserVarById(eventUsers[1].Id, "lastSelectedCharacterButtons");
        
        CPH.SetGlobalVar("streamDeckCharacterButtons", buttonCharacterDict);

        return true;
    }

    public bool CharacterPortraitPress()
    {
        CPH.LogInfo("Portrait pressed");
        if (!CPH.TryGetArg("sdButtonId", out string buttonId))
        {
            CPH.LogInfo("WTF button not found");
            return false;
        }

        // Get global and user vars
        var buttonCharacterDict = CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons");
        
        if (!buttonCharacterDict.TryGetValue(buttonId, out var streamDeckButtonState))
        {
            CPH.LogInfo("Character not found");
            return false;
        }
        
        
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var indexOfUser = eventUsers.FindIndex(user => user.Id == streamDeckButtonState.UserId);

        CPH.LogInfo("Updating squad");
        var userSquadRoster = CPH.GetTwitchUserVarById<List<string>>(streamDeckButtonState.UserId, "squadRoster");
        var userSquadRosterLosers =
            CPH.GetTwitchUserVarById<List<string>>(streamDeckButtonState.UserId, "userSquadRosterLosers");
        if (userSquadRosterLosers == null)
        {
            userSquadRosterLosers = new List<string>();
        }

        // Update last selected color with its corresponding color
        var lastSelectedButtonState = CPH.GetTwitchUserVarById<StreamDeckSmashCharacterButtonState>( 
            streamDeckButtonState.UserId, "lastSelectedCharacterButtons");
        var updateButtonState = true;
        if(lastSelectedButtonState != null)
        {
            if (buttonCharacterDict.TryGetValue(lastSelectedButtonState.ButtonId, out var lastSelectedButton))
            {
                CPH.LogInfo("Updating last button color");
                var lastSelectedButtonCharacterImage = GetImageFilePath(lastSelectedButton.Character, lastSelectedButtonState.UserId);
                CPH.StreamDeckSetBackgroundLocal(lastSelectedButtonState.ButtonId, lastSelectedButtonCharacterImage, _streamDeckConfiguration.Theming.AlreadyUsedColor);
                CPH.StreamDeckSetTitle(lastSelectedButtonState.ButtonId, "X");
                var previousCharacterIndex = userSquadRoster.IndexOf(lastSelectedButton.Character);
                userSquadRoster.RemoveAt(previousCharacterIndex);
                userSquadRoster.Add("");
                userSquadRosterLosers.Add(lastSelectedButton.Character);
            }
            
            // We pressed the same button as last time
            updateButtonState = lastSelectedButtonState.ButtonId != streamDeckButtonState.ButtonId;
            
        }
        
        // Update current button background color and setup character list for player
        CPH.LogInfo($"Updating button state: {updateButtonState}");
        if(updateButtonState)
        {
            CPH.LogInfo("Updating button color");
            var currentSelectedButtonCharacterImage = GetImageFilePath(streamDeckButtonState.Character, streamDeckButtonState.UserId);
            CPH.StreamDeckSetBackgroundLocal(buttonId, currentSelectedButtonCharacterImage, _streamDeckConfiguration.Theming.SelectedColor);
            CPH.StreamDeckSetTitle(buttonId, "O");
            CPH.StreamDeckSetBackgroundLocal(indexOfUser == 0 ? _streamDeckConfiguration.Buttons.LeftStocksButtonId : _streamDeckConfiguration.Buttons.RightStocksButtonId, currentSelectedButtonCharacterImage);
            var currentIndexOfCharacter = userSquadRoster.IndexOf(streamDeckButtonState.Character);
            userSquadRoster.RemoveAt(currentIndexOfCharacter);
            userSquadRoster.Insert(
                //eventUsers.FindIndex(user => streamDeckButtonState.UserId == user.Id) == 0 ? userSquadRoster.Count : 0,
                0,
                streamDeckButtonState.Character
            );
            CPH.SetTwitchUserVarById(streamDeckButtonState.UserId, "lastSelectedCharacterButtons", streamDeckButtonState);
        }
        else
        {
            CPH.UnsetTwitchUserVarById(streamDeckButtonState.UserId, "lastSelectedCharacterButtons");
        }
        
        // Update changes to global and user vars
        CPH.LogInfo("Commiting changes");
        CPH.SetGlobalVar("streamDeckCharacterButtons", buttonCharacterDict);
        CPH.SetTwitchUserVarById(streamDeckButtonState.UserId, "squadRoster", userSquadRoster);
        CPH.SetTwitchUserVarById(streamDeckButtonState.UserId, "userSquadRosterLosers", userSquadRosterLosers);
        return true;
    }

    private string GetImageFilePath(string characterStartGgName, string userId)
    {
        CPH.LogInfo($"Getting info for {characterStartGgName}");
        var selectedTheme = CPH.GetGlobalVar<string>("smashIcons");
        CPH.LogInfo($"Getting info for {selectedTheme}");
        var buttonIcons = _streamDeckConfiguration.Theming.GetButtonsIcons(selectedTheme);
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

    public bool GetUserCharacterPathWithSkin()
    {
        if (!CPH.TryGetArg<string>("userId", out var userId))
        {
            return false;
        }
        
        if (!CPH.TryGetArg<string>("character", out var character))
        {
            return false;
        }

        var path = GetImageFilePath(userId, character);
        CPH.SetArgument("characterPath", path);

        return true;
    }

    public bool ResetStreamDeckButtons()
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

    public bool ForceUpdateStreamDeckButtons()
    {
        return false;
    }

    private string GetCharacterButtonBackgroundColor(string userId, string character)
    {
        var buttonCharacterDict = CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons");
        var currentSquadRoster = CPH.GetTwitchUserVarById<List<string>>(userId, "squadRoster");
        var color = currentSquadRoster.Contains(character) ? _streamDeckConfiguration.Theming.UnselectedColor : _streamDeckConfiguration.Theming.AlreadyUsedColor;
        
        var lastSelectedButtonState = CPH.GetTwitchUserVarById<StreamDeckSmashCharacterButtonState>(userId, "lastSelectedCharacterButtons");
        
        if (lastSelectedButtonState == null)
            return color;
        
        if (buttonCharacterDict.TryGetValue(lastSelectedButtonState.ButtonId, out var lastSelectedButton))
        {
            color = lastSelectedButton.Character == character ? _streamDeckConfiguration.Theming.SelectedColor : color;
        }


        return color;
    }
    
    private string GetCharacterButtonTitle(string userId, string character)
    {
        var buttonCharacterDict = CPH.GetGlobalVar<Dictionary<string, StreamDeckSmashCharacterButtonState>>("streamDeckCharacterButtons");
        var currentSquadRoster = CPH.GetTwitchUserVarById<List<string>>(userId, "squadRoster");
        var title = currentSquadRoster.Contains(character) ? "" : "X";
        
        var lastSelectedButtonState = CPH.GetTwitchUserVarById<StreamDeckSmashCharacterButtonState>(userId, "lastSelectedCharacterButtons");
        
        if (lastSelectedButtonState == null)
            return title;
        
        if (buttonCharacterDict.TryGetValue(lastSelectedButtonState.ButtonId, out var lastSelectedButton))
        {
            title = lastSelectedButton.Character == character ? "O" : title;
        }


        return title;
    }
}