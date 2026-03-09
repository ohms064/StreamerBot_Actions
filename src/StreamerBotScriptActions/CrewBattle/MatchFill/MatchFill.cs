using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.CrewBattle.MatchFill;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SBCustomClasses.TSH.CurrentMatch;

public class CPHInline : CPHInlineBase
{
    private MatchData _currentMatchData;
    private const string CrewBattleTeamNameRight = "Crew Battle Team Right";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Left";
    public void Init()
    {
        _currentMatchData = new MatchData
        {
            TournamentPhase = "Stream Match",
            Stream = "www.twitch.tv/el_amigohms",
            IsOnline = true
        };
    }

    public bool Execute()
    {
        return UpdateCurrentMatch();
    }

    public bool UpdateCurrentMatch()
    {
        CPH.LogInfo("Starting match file update");

        var teamRosters = new List<List<Entrant>>
        {
            CPH.UsersInGroup(CrewBattleTeamNameLeft).Select(u => GetEntrantFromUserId(u.Id)).ToList(),
            CPH.UsersInGroup(CrewBattleTeamNameRight).Select(u => GetEntrantFromUserId(u.Id)).ToList(),
        };

        // The format requires a weird List setup, probably because it considers teams as a possiblity.
        _currentMatchData.Entrants = teamRosters;
        _currentMatchData.Team1Score = CPH.GetGlobalVar<int>($"{CrewBattleTeamNameLeft} Stocks");
        _currentMatchData.Team2Score = CPH.GetGlobalVar<int>($"{CrewBattleTeamNameRight} Stocks");
        CPH.LogInfo("match mains update");
        _currentMatchData.P1Name = CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft);
        _currentMatchData.P2Name = CPH.GetGlobalVar<string>(CrewBattleTeamNameRight);
        _currentMatchData.HasSelectionData = true;

        CPH.LogInfo("Writing current match");
        CPH.SetGlobalVar("currentMatch", _currentMatchData);
        // Update TSH file
        var currentMatchList = new List<MatchData>{_currentMatchData};
        File.WriteAllText("D:/Streams/localmatch/current_match.json", JsonConvert.SerializeObject(currentMatchList));
        return true;
    }

    private List<List<string>> GetCharactersWithSkin(string userId, List<string> characters)
    {
        var result = new List<List<string>>();
        
        if (characters == null)
        {
            return result;
        }
        foreach (var c in characters)
        {
            var innerResult = new List<string>();
            var userVarName = $"skin_{c}";
            var skin = CPH.GetTwitchUserVarById<int>(userId, userVarName);
            innerResult.Add(c);
            innerResult.Add(skin.ToString());
            result.Add(innerResult);
        }
        return result;
    }

    private Entrant GetEntrantFromUserId(string userId)
    {
        var entrant = new Entrant();
        var twitchUser = CPH.TwitchGetExtendedUserInfoById(userId);
        var userNickname = CPH.GetTwitchUserVarById<string>(userId, "nickname");
        var userRoster = CPH.GetTwitchUserVarById<List<string>>(userId, "userRoster");
        entrant.GamerTag = userNickname ?? twitchUser.UserName;
        entrant.Avatar = twitchUser.ProfileImageUrl;
        entrant.Mains = GetCharactersWithSkin(userId, userRoster);
        return entrant;
    }

    public bool WriteCurrentMatch()
    {
        var currentMatch = CPH.GetGlobalVar<MatchData>("currentMatch");
        File.WriteAllText("D:/Streams/localmatch/current_match.json", JsonConvert.SerializeObject(currentMatch));
        return true;
    }
}