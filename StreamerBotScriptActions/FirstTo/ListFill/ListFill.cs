using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SBCustomClasses.TSH.MatchList;
using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.FirstTo.ListFill;

// ReSharper disable once InconsistentNaming
public class CPHInline : CPHInlineBase
{
    private List<MatchListData> _listData;

    public void Init()
    {
        _listData = new List<MatchListData>();
    }

    public bool Execute()
    {
        // your main code goes here
        return UpdateListData();
    }

    public bool UpdateListData()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var entrants = new List<Entrant>();
        _listData.Clear();
        var listData = new MatchListData();
        listData.TournamentPhase = "Stream Match";
        listData.Stream = "www.twitch.tv/el_amigohms";
        listData.Id = 0;
        foreach (var user in eventUsers) entrants.Add(GetEntrantFromUserId(user.Id));

        // The format requires a weird List setup, probably because it considers teams as a possiblity.
        listData.Entrants = new List<List<Entrant>>
        {
            new List<Entrant> { entrants[0] },
            new List<Entrant> { entrants[1] }
        };
        listData.P1Name = entrants[0].GamerTag;
        listData.P2Name = entrants[1].GamerTag;
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