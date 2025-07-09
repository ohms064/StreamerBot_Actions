using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SBCustomClasses.TSH;
using SBCustomClasses.TSH.Base;
using SBCustomClasses.TSH.CurrentMatch;
using SocketIO.Serializer.NewtonsoftJson;
using Streamer.bot.Plugin.Interface;

namespace SBCustomClasses.StreamDeck
{
    public partial class StreamDeckTSHConnection
    {
        private BaseGameInfo _gameInfo;
        #region Current Match

        /// <summary>
        /// Updates match on TSH
        /// </summary>
        /// <param name="CPH">Streamerbot interface</param>
        // ReSharper disable once MemberCanBePrivate.Global
        private void UpdateCurrentMatch(IInlineInvokeProxy CPH)
        {
            Task.Run(async () =>
            {
                CPH.LogDebug("Starting SocketIO connection");
                var uri = CPH.GetGlobalVar<string>("tsh_url") ?? "ws://127.0.0.1:5000/";
                var webSocket = new SocketIOClient.SocketIO(uri);
                webSocket.Serializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                });
                await webSocket.ConnectAsync();
                
                CPH.LogDebug("SocketIO connected");
                
                CPH.LogInfo("Updating current match");
                var eventTeams = new[] { _teamLeft, _teamRight };
                var score = new[] { _teamLeft.Stocks, _teamRight.Stocks };
                CPH.LogDebug($"Teams: \n{JsonConvert.SerializeObject(eventTeams)}");

                CPH.LogInfo($"Settings teams {_teamLeft.TeamMembers.Count} | {_teamRight.TeamMembers.Count}");
                var teamIndex = 1;
                foreach (var team in eventTeams)
                {
                    var playerIndex = 1;
                    foreach (var user in team.TeamMembers.Values)
                    {
                        CPH.LogInfo($"Setting user: {user}");
                        CPH.LogInfo($"Setting user characters: {JsonConvert.SerializeObject(user.Characters)}");
                        var entrant = GetEntrantFromUserId(CPH, user.TwitchUserInfo.UserId);
                        entrant.Mains = GetCharactersWithSkin(CPH, user.TwitchUserInfo.UserId, user.Characters);
                        entrant.Player = playerIndex;
                        entrant.Team = teamIndex;
                        CPH.LogInfo($"Serializing json entrant");
                        var jsonEntrant = JsonConvert.SerializeObject(entrant);
                        CPH.LogInfo($"Json entrant result: {jsonEntrant}");
                        await webSocket.EmitAsync("update_team", jsonEntrant);
                        CPH.LogInfo($"Sent to websocket");
                        playerIndex++;
                    }

                    teamIndex++;
                }

                CPH.LogInfo($"Logging Scores: {score[0]} | {score[1]}");
                var scoreData = new ScoreRequest()
                {
                    Team1Score = score[0],
                    Team2Score = score[1]
                };

                var jsonScore = JsonConvert.SerializeObject(scoreData);
                CPH.LogInfo($"Json score result: {jsonScore}");
                await webSocket.EmitAsync("score", jsonScore);
                await webSocket.DisconnectAsync();
                CPH.LogInfo($"Ending connection");
            }).GetAwaiter().GetResult();
            
        }

        private Dictionary<string, List<List<string>>> GetCharactersWithSkin(IInlineInvokeProxy CPH, string userId, IEnumerable<string> characters)
        {
            var result = new Dictionary<string, List<List<string>>>();
            var gameList = new List<List<string>>();
            var gameId = CPH.GetGlobalVar<string>("CurrentGameId");

            if (characters == null) return result;
            foreach (var c in characters)
            {
                var innerResult = new List<string>();
                var userVarName = $"skin_{c}";
                var skin = CPH.GetTwitchUserVarById<int>(userId, userVarName);
                CPH.LogInfo($"Getting character with skin: {userVarName} for user {userId}");
                innerResult.Add(c);
                innerResult.Add(skin.ToString());
                gameList.Add(innerResult);
            }
            result.Add(gameId, gameList);

            return result;
        }

        private EntrantRequest GetEntrantFromUserId(IInlineInvokeProxy CPH, string userId)
        {
            var entrant = new EntrantRequest();
            var twitchUser = CPH.TwitchGetExtendedUserInfoById(userId);
            var userNickname = CPH.GetTwitchUserVarById<string>(userId, "nickname");
            var userRoster = CPH.GetTwitchUserVarById<List<string>>(userId, "userRoster");
            entrant.GamerTag = userNickname ?? twitchUser.UserName;
            entrant.Avatar = twitchUser.ProfileImageUrl;
            entrant.Mains = GetCharactersWithSkin(CPH, userId, userRoster);
            return entrant;
        }

        #endregion

        private void UpdateFromTSHFile(string path)
        {
            var smashGameJson = File.ReadAllText(path);
            _gameInfo = JsonConvert.DeserializeObject<BaseGameInfo>(smashGameJson);
        }
    }
}