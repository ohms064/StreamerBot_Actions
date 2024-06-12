using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.StreamDeckButtonsUpdate;

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.TSH.Base;
using SBCustomClasses.StreamDeck;

public class CPHInline : CPHInlineBase
{
    private const string LeftButtonId = "a0cb21de-05b2-477b-a7b1-ec4afcc590be";
    private const string RightButtonId = "fb313b14-7fb7-4b3c-be36-0090a94fc644";
    private const string LeftButtonIdStocks = "332515bd-64a8-4c0b-bf75-08b98515b327";
    private const string RightButtonIdStocks = "24c86ecf-c942-4fee-86fb-0a61c42eadb7";
    private const string ContextButtonId = "ccccae24-f19b-43de-b76a-9b873ee1da9c";

    private readonly List<string> _leftCharactersButtonId = new List<string>
    {
        "ecceca82-2149-4812-9883-1d014baffe4a",
        "59005484-7d6e-4efb-aa63-b242dc72c50a",
        "c7470403-2f19-48c1-b305-44f833f3b42b",
        "370e25ba-ecc8-40c0-b0a3-76e0d0c97378",
        "6dd778bc-3464-4b4d-8dfc-8be6307c654d"
    };

    private readonly List<string> _rightCharactersButtonId = new List<string>
    {
        "1d0a501a-6a70-4f8e-95aa-4c2aa858a97a",
        "86bb6519-47e7-48e4-a960-5eacac61ad63",
        "3b9d82a6-34c4-457f-aae2-c960adc856b2",
        "b92aaa34-1cad-4353-8f0a-04cf753d3769",
        "57012a0b-fb08-4b6b-8f0b-b797a726d599"
    };

    private const string SelectedColor = "#1ed86150";
    private const string AlreadyUsedColor = "#FF000050";
    private const string UnselectedColor = "#00000050";

    private const string SmashConfigurationFilePath =
        "D:/Streams/TournamentStreamHelper/user_data/games/ssbu/base_files/config.json";

    //private const string PortraitsFilesPath = "D:/Streams/TournamentStreamHelper/user_data/games/ssbu/full";
    private const string PortraitsFilesPath = "D:\\Streams\\TournamentStreamHelper\\user_data\\games\\ssbu\\base_files\\icon";

    private BaseGameInfo _smashGameInfo;

    public void Init()
    {
        CPH.LogInfo("Setting initial configurations for StreamDeck");
        CPH.StreamDeckSetTitle(ContextButtonId, "");
        var smashFileContent = File.ReadAllText(SmashConfigurationFilePath);
        _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(smashFileContent);
    }

    public bool Execute()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var twitchUserLeft = CPH.TwitchGetExtendedUserInfoById(eventUsers[0].Id);
        var twitchUserRight = CPH.TwitchGetExtendedUserInfoById(eventUsers[1].Id);
        var userNicknameLeft = CPH.GetTwitchUserVarById<string>(eventUsers[0].Id, "nickname");
        var userNameLeft = userNicknameLeft ?? twitchUserLeft.UserName;
        var userNicknameRight = CPH.GetTwitchUserVarById<string>(eventUsers[1].Id, "nickname");
        var userNameRight = userNicknameRight ?? twitchUserRight.UserName;
        CPH.StreamDeckSetBackgroundUrl(LeftButtonId, twitchUserLeft.ProfileImageUrl);
        CPH.StreamDeckSetBackgroundUrl(RightButtonId, twitchUserRight.ProfileImageUrl);
        CPH.StreamDeckSetTitle(LeftButtonId, userNameLeft);
        CPH.StreamDeckSetTitle(RightButtonId, userNameRight);
        return UpdateButtonStocks() && SetupCharacterPortraits();
    }

    public bool UpdateButtonStocks()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var userStocksLeft = CPH.GetTwitchUserVarById<int>(eventUsers[0].Id, "stocks");
        var userStocksRight = CPH.GetTwitchUserVarById<int>(eventUsers[1].Id, "stocks");
        CPH.StreamDeckSetTitle(LeftButtonIdStocks, $"{userStocksLeft}");
        CPH.StreamDeckSetTitle(RightButtonIdStocks, $"{userStocksRight}");
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
            _leftCharactersButtonId,
            _rightCharactersButtonId
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
            var currentSquadRoster = CPH.GetTwitchUserVarById<List<string>>(eventUsers[i].Id, "squadRoster");
            var buttonIndex = 0;
            CPH.UnsetTwitchUserVarById(eventUsers[i].Id, "lastSelectedCharacterButtons");
            foreach (var character in originalSquadRoster)
            {
                var buttonId = characterButtonsLayout[i][buttonIndex];
                var imageFile = GetImageFilePath(character, eventUsers[i].Id);
                CPH.LogDebug(imageFile);
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
            }

            CPH.LogInfo("Cleaning remaining buttons");
            // Clean the remaining buttons
            for (var j = buttonIndex; j < characterButtonsLayout[i].Count; j++)
            {
                var buttonId = characterButtonsLayout[i][j];
                CPH.StreamDeckSetBackgroundLocal(buttonId, "", UnselectedColor);
                buttonCharacterDict[buttonId] = new StreamDeckSmashCharacterButtonState
                {
                    ButtonId = buttonId
                };
            }
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
                CPH.StreamDeckSetBackgroundLocal(lastSelectedButtonState.ButtonId, lastSelectedButtonCharacterImage, AlreadyUsedColor);
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
            CPH.StreamDeckSetBackgroundLocal(buttonId, currentSelectedButtonCharacterImage, SelectedColor);
            CPH.StreamDeckSetTitle(buttonId, "O");
            CPH.StreamDeckSetBackgroundLocal(indexOfUser == 0 ? LeftButtonIdStocks : RightButtonIdStocks, currentSelectedButtonCharacterImage);
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
        //var path = $"{PortraitsFilesPath}/chara_1_random_00.png";
        var path = $"{PortraitsFilesPath}/chara_2_random_00.png";
        var skinIndex = 0;
        if (!string.IsNullOrEmpty(userId))
        {
            skinIndex = CPH.GetTwitchUserVarById<int>(userId, $"skin_{characterStartGgName}");
        }
        if (_smashGameInfo.CharacterToCodename.TryGetValue(characterStartGgName.Trim(), out var result))
        {
            //var imagePath = $"{PortraitsFilesPath}/chara_1_{result.Codename}_0{skinIndex}.png";
            var imagePath = $"{PortraitsFilesPath}/chara_2_{result.Codename}_0{skinIndex}.png";
            if (File.Exists(imagePath))
            {
                path = imagePath;
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
        CPH.LogInfo("Reseting stream deck buttons");
        var buttonsLayout = new List<string>
        {
            LeftButtonId,
            RightButtonId,
            LeftButtonIdStocks,
            RightButtonIdStocks,
            ContextButtonId
        };
        buttonsLayout.AddRange(_leftCharactersButtonId);
        buttonsLayout.AddRange(_rightCharactersButtonId);

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
        var color = currentSquadRoster.Contains(character) ? UnselectedColor : AlreadyUsedColor;
        
        var lastSelectedButtonState = CPH.GetTwitchUserVarById<StreamDeckSmashCharacterButtonState>(userId, "lastSelectedCharacterButtons");
        
        if (lastSelectedButtonState == null)
            return color;
        
        if (buttonCharacterDict.TryGetValue(lastSelectedButtonState.ButtonId, out var lastSelectedButton))
        {
            color = lastSelectedButton.Character == character ? SelectedColor : color;
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