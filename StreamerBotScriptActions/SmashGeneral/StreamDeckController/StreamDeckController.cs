
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;
using Streamer.bot.Plugin.Interface.Model;

namespace StreamerBotScriptActions.SmashGeneral.StreamDeckController;

using System.Linq;
using System.Collections.Generic;
using SBCustomClasses.StreamDeck;

public class CPHInline : CPHInlineBase
{
    private StreamDeckTSHConnection _streamDeck;
    private bool _matchPlaying = false;
    private string _matchType = "";
    private const string LeftTeam = "teamLeft";
    private const string RightTeam = "teamRight";
    private const string LeftTeamName = "leftTeamName";
    private const string RightTeamName = "rightTeamName";
    private const string TeamRoster = "currentRoster";
    
    private const string CharacterCommand = "!c ";
    private const string OverrideCharacterCommand = "!o ";
    public bool Execute()
    {
        _streamDeck = StreamDeckTSHConnection.Get(CPH);
        return true;
    }

    public bool CleanButtons()
    {
        _streamDeck.ResetStreamDeckButtons(CPH);
        return true;
    }

    /// <summary>
    /// Updates the corresponding sections on the stream deck. Ideally just update what is needed for performance.
    /// </summary>
    /// <returns></returns>
    public bool RefreshStreamDeck()
    {
        StreamDeckSections sectionsToUpdate;
        if (!CPH.TryGetArg("groupIndex", out int groupIndex))
        {
            sectionsToUpdate = StreamDeckSections.BothTeams;
        }
        else
        {
            sectionsToUpdate = groupIndex == 0 ? StreamDeckSections.TeamLeft : StreamDeckSections.TeamRight;
        }

        if (!CPH.TryGetArg("section", out string section))
        {
            sectionsToUpdate |= StreamDeckSections.AllSections;
        }
        else
        {
            sectionsToUpdate |= section switch
            {
                "characters" => StreamDeckSections.Characters,
                "players" => StreamDeckSections.TeamMembers,
                "currentPlayers" => StreamDeckSections.CurrentPlayerPFP | StreamDeckSections.CurrentPlayerCharacter 
                                                                        | StreamDeckSections.CurrentStocks,
                _ => StreamDeckSections.AllSections
            };
        }
        
        _streamDeck.RefreshStreamDeck(CPH, sectionsToUpdate);
        return true;
    }

    public bool StartMatch()
    {
        var leftTeamGroup = CPH.UsersInGroup(LeftTeam);
        var rightTeamGroup = CPH.UsersInGroup(RightTeam);
        var leftTeam = CreateTeam(leftTeamGroup, CPH.GetGlobalVar<string>(LeftTeamName));
        var rightTeam = CreateTeam(rightTeamGroup, CPH.GetGlobalVar<string>(RightTeamName));
        
        _streamDeck.InitConnection(CPH, leftTeam, rightTeam);
        _matchPlaying = true;
        _matchType = CPH.GetGlobalVar<string>("currentGameMode");
        return true;

        TeamData CreateTeam(List<GroupUser> users, string teamName)
        {
            var data = new TeamData
            {
                TeamMembers = users.Select(u =>
                {
                    var characters = CPH.GetTwitchUserVarById<List<string>>(u.Id, TeamRoster);
                    return new TeamMemberData(u.Id, characters);
                }).ToList(),
                Name = teamName
            };
            return data;
        }
    }

    public bool EndMatch()
    {
        CPH.AddToCredits(_matchType, _streamDeck.GetCredits(), false);
        _matchPlaying = false;
        _matchType = "";
        return true;
    }

    /// <summary>
    /// Adds a user to the left team
    /// </summary>
    /// <returns></returns>
    public bool AddUserToLeftTeam()
    {
        return AddUserToGroup(true);
    }
    
    /// <summary>
    /// Adds a user to the right team
    /// </summary>
    /// <returns></returns>
    public bool AddUserToRightTeam()
    {
        return AddUserToGroup(false);
    }

    /// <summary>
    /// Should be called from stream deck button event, changes the current player's character.
    /// </summary>
    /// <returns></returns>
    public bool CharacterPortraitPress()
    {
        CPH.LogInfo("Portrait pressed");
        if (!CPH.TryGetArg("sdButtonId", out string buttonId))
        {
            CPH.LogInfo("WTF button not found");
            return false;
        }
        
        _streamDeck.SelectCharacter(CPH, buttonId);
        return true;
    }
    
    /// <summary>
    /// Should be called from stream deck button event, changes the current player.
    /// </summary>
    /// <returns></returns>
    public bool PlayerPortraitPress()
    {
        CPH.LogInfo("Portrait pressed");
        if (!CPH.TryGetArg("sdButtonId", out string buttonId))
        {
            CPH.LogInfo("WTF button not found");
            return false;
        }
        
        _streamDeck.SelectPlayer(CPH, buttonId);
        return true;
    }
    
    /// <summary>
    /// Should be called from stream deck button event, changes the current player character state.
    /// (Selected or Unselected->Used, Used->Unselected)
    /// </summary>
    /// <returns></returns>
    public bool CharacterPortraitPressToggle()
    {
        if (!CPH.TryGetArg("sdButtonId", out string buttonId))
        {
            CPH.LogInfo("WTF button not found");
            return false;
        }
        
        _streamDeck.ToggleCharacterState(CPH, buttonId);
        return true;
    }

    /// <summary>
    /// Overrides characters for a player
    /// </summary>
    /// <returns></returns>
    public bool OverrideUserCharacters()
    {
        CPH.LogDebug("Overriding characters");
        var fuzzyTools = CharacterFuzzyTools.Get(_streamDeck.PathManager);
        CPH.TryGetArg("rawInput", out string rawInput);
        CPH.TryGetArg("targetUserId", out string userId);
        
        // Before starting, we check if the source is different from a whisper
        if (rawInput.StartsWith(CharacterCommand))
        {
            rawInput = rawInput.Remove(0, CharacterCommand.Length);
        }
        else if (rawInput.StartsWith(OverrideCharacterCommand))
        {
            var command = rawInput.Split(' ');
            var userLogin = CPH.TwitchGetUserInfoByLogin(command[1].Trim('@'));
            if (userLogin == null)
            {
                return false;
            }
            userId = userLogin.UserId;
            rawInput = "";
            for (var i = 2; i < command.Length; i++)
            {
                rawInput += $"{command[i]} ";
            }

            rawInput = rawInput.Trim();

        }
        
        // Obtain the characters
        var userSelectedCharacters = rawInput.Split(',');
        var resultCharactersForUser = fuzzyTools.SelectCharacterFuzzy(userSelectedCharacters);

        CPH.SetTwitchUserVarById(userId, TeamRoster, resultCharactersForUser);

        if (!_matchPlaying) return true;

        _streamDeck.OverridePlayerCharacters(userId, resultCharactersForUser);

        return true;
    }
    
    public bool UpdatePlayerStocks()
    {
        if (!CPH.TryGetArg("value2Add", out long value2AddLong))
        {
            CPH.LogDebug("value2Add was not set");
            return false;
        }

        if (!CPH.TryGetArg("groupIndex", out long groupIndexLong))
        {
            CPH.LogDebug("userIndex was not set");
            return false;
        }

        var value2Add = (int)value2AddLong;
        var groupIndex = (int)groupIndexLong;
        _streamDeck.UpdateStocks(CPH, groupIndex == 0, value2Add);
        return true;
    }

    private bool AddUserToGroup(bool isLeftTeam)
    {
        var team = isLeftTeam ? LeftTeam : RightTeam;
        if (!CPH.TryGetArg("userId", out string userId))
        {
            CPH.LogError("userId was not an argument");
            return false;
        }
        
        var opposingTeam = isLeftTeam ? RightTeam : LeftTeam;

        if (CPH.UserIdInGroup(userId, Platform.Twitch, opposingTeam))
        {
            CPH.LogInfo("User already in opposing team");
            return false;
        }
        
        if (!CPH.AddUserIdToGroup(userId, Platform.Twitch, team))
        {
            return false;
        }

        // By default, set the user mains as the team roster
        var mains = CPH.GetTwitchUserVarById<List<string>>(userId, _streamDeck.PathManager.GameMains);
        CPH.SetTwitchUserVarById(userId, TeamRoster, mains);
        
        if (!_matchPlaying) return true;
        
        //If match already playing, we also update the stream deck state
        var characters = CPH.GetTwitchUserVarById<List<string>>(userId, TeamRoster);
        var teamMember = new TeamMemberData(userId, characters);
        _streamDeck.AddPlayerToTeam(CPH, teamMember, isLeftTeam);
        _streamDeck.RefreshCharacters(CPH);
        
        return true;
    }
}