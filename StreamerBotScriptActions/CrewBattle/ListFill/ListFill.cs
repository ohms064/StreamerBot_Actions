using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.CrewBattle.ListFill;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SBCustomClasses.TSH.MatchList;

public class CPHInline : CPHInlineBase
{
    private List<MatchListData> _listData;
    private const string CrewBattleTeamNameRight = "Crew Battle Team Right";
    private const string CrewBattleTeamNameLeft = "Crew Battle Team Left";

    public void Init()
    {
        _listData = new List<MatchListData>();
    }

    public bool Execute()
    {
        // your main code goes here
        var teamRosters = new List<List<Entrant>>
        {
            CPH.UsersInGroup(CrewBattleTeamNameLeft).Select(u => GetEntrantFromUserId(u.Id)).ToList(),
            CPH.UsersInGroup(CrewBattleTeamNameRight).Select(u => GetEntrantFromUserId(u.Id)).ToList(),
        };
        
        CPH.SetGlobalVar($"{CrewBattleTeamNameLeft} Stocks", teamRosters[0].Count * 3);
        CPH.SetGlobalVar($"{CrewBattleTeamNameRight} Stocks", teamRosters[1].Count * 3);
        
        return UpdateListData();
    }

    public bool UpdateListData()
    {
        var teamNames = new List<string>
        {
            CPH.GetGlobalVar<string>(CrewBattleTeamNameLeft),
            CPH.GetGlobalVar<string>(CrewBattleTeamNameRight)
        };
        
        var teamRosters = new List<List<Entrant>>
        {
            CPH.UsersInGroup(CrewBattleTeamNameLeft).Select(u => GetEntrantFromUserId(u.Id)).ToList(),
            CPH.UsersInGroup(CrewBattleTeamNameRight).Select(u => GetEntrantFromUserId(u.Id)).ToList(),
        };
        _listData.Clear();
        var listData = new MatchListData
        {
            TournamentPhase = "Stream Match",
            Stream = "www.twitch.tv/el_amigohms",
            Id = 0,
            RoundName = "Crew Battle",
            // The format requires a weird List setup, probably because it considers teams as a possiblity.
            Entrants = teamRosters,
            P1Name = teamNames[0],
            P2Name = teamNames[1]
        };

        _listData.Add(listData);
        CPH.SetGlobalVar("matchList", _listData);
        var json = JsonConvert.SerializeObject(_listData);
        File.WriteAllText("D:/Streams/localmatch/matches.json", json);
        CPH.LogInfo($"Writing:\n{json}");
        return true;
    }

    private Entrant GetEntrantFromUserId(string userId)
    {
        var entrant = new Entrant();
        var twitchUser = CPH.TwitchGetUserInfoById(userId);
        var userNickname = CPH.GetTwitchUserVarById<string>(userId, "nickname", true);
        entrant.GamerTag = userNickname ?? twitchUser.UserName;
        return entrant;
    }

    public bool WriteCurrentListMatch()
    {
        File.WriteAllText("D:/Streams/localmatch/matches.json", JsonConvert.SerializeObject(_listData));
        return true;
    }
}