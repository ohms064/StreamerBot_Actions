using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SBCustomClasses.TSH.Base;
using SBCustomClasses.TSH.CurrentMatch;
using SBCustomClasses.TSH.MatchList;
using Streamer.bot.Plugin.Interface;
using MatchEntrant = SBCustomClasses.TSH.CurrentMatch.Entrant;
using ListEntrant = SBCustomClasses.TSH.MatchList.Entrant;

namespace SBCustomClasses.StreamDeck
{
    public partial class StreamDeckTSHConnection
    {
        private BaseGameInfo _smashGameInfo;
        #region Current Match
        private MatchData _currentMatchData;

        private void UpdateCurrentMatch(IInlineInvokeProxy CPH)
        {
            CPH.LogInfo("Updating current match");
            var eventTeams = new[] { _teamLeft, _teamRight };
            var score = eventTeams.Select(t => t.Stocks).ToList();
            _currentMatchData = new MatchData(){Entrants = new List<List<MatchEntrant>>()};
            CPH.LogDebug($"Teams: \n{JsonConvert.SerializeObject(eventTeams)}");
            
            CPH.LogInfo($"Settings teams {_teamLeft.TeamMembers.Count} | {_teamRight.TeamMembers.Count}");
            foreach (var team in eventTeams)
            {
                var teamEntrants = new List<MatchEntrant>();
                foreach (var user in team.TeamMembers.Values)
                {
                    CPH.LogInfo($"Setting user: {user}");
                    CPH.LogInfo($"Setting user characters: {JsonConvert.SerializeObject(user.Characters)}");
                    var entrant = GetEntrantFromUserId(CPH, user.TwitchUserInfo.UserId);
                    entrant.Mains = GetCharactersWithSkin(CPH, user.TwitchUserInfo.UserId, user.Characters);
                    teamEntrants.Add(entrant);
                }
                CPH.LogInfo($"Settings team with {teamEntrants.Count} entrants");
                _currentMatchData.Entrants.Add(teamEntrants);
            }
            CPH.LogInfo($"Logging Scores: {score[0]} | {score[1]}");
            _currentMatchData.Team1Score = score[0];
            _currentMatchData.Team2Score = score[1];
            CPH.LogInfo($"Settings entrants: {_currentMatchData.Entrants}");
            _currentMatchData.P1Name = _currentMatchData.Entrants[0][0].GamerTag;
            _currentMatchData.P2Name = _currentMatchData.Entrants[1][0].GamerTag;
            _currentMatchData.HasSelectionData = true;

            CPH.LogInfo("Writing current match");
            CPH.SetGlobalVar("currentMatch", _currentMatchData);
            // Update TSH file
            var currentMatchList = new List<MatchData> { _currentMatchData };
            var jsonToWrite = JsonConvert.SerializeObject(currentMatchList);
            CPH.LogInfo(jsonToWrite);
            File.WriteAllText("D:/Streams/localmatch/current_match.json", jsonToWrite);
        }

        private List<List<string>> GetCharactersWithSkin(IInlineInvokeProxy CPH, string userId, IEnumerable<string> characters)
        {
            var result = new List<List<string>>();

            if (characters == null) return result;
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

        private MatchEntrant GetEntrantFromUserId(IInlineInvokeProxy CPH, string userId)
        {
            var entrant = new MatchEntrant();
            var twitchUser = CPH.TwitchGetExtendedUserInfoById(userId);
            var userNickname = CPH.GetTwitchUserVarById<string>(userId, "nickname");
            var userRoster = CPH.GetTwitchUserVarById<List<string>>(userId, "userRoster");
            entrant.GamerTag = userNickname ?? twitchUser.UserName;
            entrant.Avatar = twitchUser.ProfileImageUrl;
            entrant.Mains = GetCharactersWithSkin(CPH, userId, userRoster);
            return entrant;
        }

        public bool WriteCurrentMatch(IInlineInvokeProxy CPH)
        {
            var currentMatch = CPH.GetGlobalVar<MatchData>("currentMatch");
            File.WriteAllText("D:/Streams/localmatch/current_match.json", JsonConvert.SerializeObject(currentMatch));
            return true;
        }
        #endregion

        #region Match List

        private List<MatchListData>_listData = new List<MatchListData>();
        public void UpdateListData(IInlineInvokeProxy CPH)
        {
            var eventTeams = new[] { _teamLeft, _teamRight };
            var listData = new MatchListData();

            foreach (var team in eventTeams)
            {
                var entrants = new List<ListEntrant>();
                foreach (var user in team.TeamMembers.Values)
                {
                    entrants.Add(GetListEntrantFromUserId(CPH, user.TwitchUserInfo.UserId));    
                }
                listData.Entrants.Add(entrants);
            }
            
            
            _listData.Clear();
            listData.TournamentPhase = "Stream Match";
            listData.Stream = "www.twitch.tv/el_amigohms";
            listData.Id = 0;

            listData.P1Name = listData.Entrants[0][0].GamerTag;
            listData.P2Name = listData.Entrants[1][0].GamerTag;
            _listData.Add(listData);
            CPH.SetGlobalVar("matchList", _listData);
            var json = JsonConvert.SerializeObject(_listData);
            File.WriteAllText("D:/Streams/localmatch/matches.json", json);
            CPH.LogInfo($"Writing:\n{json}");
        }

        private ListEntrant GetListEntrantFromUserId(IInlineInvokeProxy CPH, string userId)
        {
            var entrant = new ListEntrant();
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
        #endregion
        
        public void UpdateFromTSHFile()
        {
            _smashGameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(SmashFileContent);
        }
    }
}