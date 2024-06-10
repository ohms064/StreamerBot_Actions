using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.MatchFill;

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.TSH.CurrentMatch;
public class CPHInline : CPHInlineBase
{
    private MatchData _currentMatchData;
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
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var entrants = new List<Entrant>();
        var score = new List<int>();
        var mains = new List<List<string>>();
        foreach (var user in eventUsers)
        {
            entrants.Add(GetEntrantFromUserId(user.Id));
            var userStocks = CPH.GetTwitchUserVarById<int>(user.Id, "stocks");
            var userMains = CPH.GetTwitchUserVarById<List<string>>(user.Id, "squadRoster");
            score.Add(userStocks);
            mains.Add(userMains);
        }

        // The format requires a weird List setup, probably because it considers teams as a possiblity.
        _currentMatchData.Entrants = new List<List<Entrant>>
        {
            new List<Entrant>
            {
                entrants[0]
            },
            new List<Entrant>
            {
                entrants[1]
            }
        };
        _currentMatchData.Team1Score = score[0];
        _currentMatchData.Team2Score = score[1];
        CPH.LogInfo("match mains update");
        _currentMatchData.Entrants[0][0].Mains = GetCharactersWithSkin(eventUsers[0].Id, mains[0]);
        _currentMatchData.Entrants[1][0].Mains = GetCharactersWithSkin(eventUsers[1].Id, mains[1]);
        _currentMatchData.P1Name = _currentMatchData.Entrants[0][0].GamerTag;
        _currentMatchData.P2Name = _currentMatchData.Entrants[1][0].GamerTag;
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