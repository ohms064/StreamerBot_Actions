using Streamer.bot.Plugin.Interface;
using WebSocketSharp;

namespace StreamerBotScriptActions.SquadBattle.PredictionForEvent;

using System.Collections.Generic;
using Newtonsoft.Json;
using SBCustomClasses.StreamerBotTwitch;

public class CPHInline : CPHInlineBase
{
    private const int PredictionDuration = 60 * 10;
    public bool Execute()
    {
        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        const string predictionTitle = "¿Quién gana?";
        var predictionOptions = new List<string>();
        foreach (var user in eventUsers)
        {
            var twitchUser = CPH.TwitchGetUserInfoById(user.Id);
            var userNickname = CPH.GetTwitchUserVarById<string>(user.Id, "nickname");
            var userName = userNickname ?? twitchUser.UserName;
            predictionOptions.Add(userName);
        }

        var currentPredictionJson = CPH.TwitchPredictionCreate(predictionTitle, predictionOptions, PredictionDuration);
        if (string.IsNullOrEmpty(currentPredictionJson))
        {
            return true;
        }
        var currentPredictionData = JsonConvert.DeserializeObject<TwitchPredictionData>(currentPredictionJson);
        
        CPH.SetGlobalVar("currentPredictionData", currentPredictionData);
        CPH.SetGlobalVar("ongoingPrediction", true);
        
        //CPH.SetGlobalVar("currentPrediction", predictionId);
        return true;
    }

    public bool TryCancelPrediction()
    {
        var ongoingPrediction = CPH.GetGlobalVar<bool>("ongoingPrediction");
        var currentPrediction = CPH.GetGlobalVar<TwitchPredictionData>("currentPredictionData");
        if (!ongoingPrediction)
        {
            return true;
        }

        CPH.TwitchPredictionCancel(currentPrediction.Id.ToString());
        CPH.SetGlobalVar("currentPredictionData", new TwitchPredictionData());
        CPH.SetGlobalVar("ongoingPrediction", false);
        return true;
    }

    public bool SetPredictionResult()
    {
        var ongoingPrediction = CPH.GetGlobalVar<bool>("ongoingPrediction");
        var currentPrediction = CPH.GetGlobalVar<TwitchPredictionData>("currentPredictionData");
        if (!ongoingPrediction)
        {
            CPH.LogInfo("No ongoing prediction");
            return true;
        }

        if (!CPH.TryGetArg("sdButtonId", out string originButtonId))
        {
            CPH.LogInfo("Button info was not found");
            return false;
        }

        if (!CPH.TryGetArg("userIndex", out long userIndexLong))
        {
            CPH.LogInfo("No userIndex found");
            return false;
        }
        
        var userIndex = (int) userIndexLong;

        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");
        var eventUsers = CPH.UsersInGroup(squadGroupName);
        var userStocksLeft = CPH.GetTwitchUserVarById<int>(eventUsers[0].Id, "stocks");
        var userStocksRight = CPH.GetTwitchUserVarById<int>(eventUsers[1].Id, "stocks");
        if (userStocksLeft > 0 && userStocksRight > 0)
        {
            CPH.StreamDeckShowAlert(originButtonId);
            CPH.ShowToastNotification("prediction_unfinished", "Error en la predicción!", "El score aun no es cero", "", "D:/Streams/Streamer.bot-x64-0.2.2/streamer.bot.png");
            return false;
        }

        //var predictionId = CPH.GetGlobalVar<string>("currentPrediction");
        CPH.LogInfo($"Current Prediction: {currentPrediction.Id.ToString()} Result Id: {currentPrediction.Outcomes[userIndex].Id.ToString()}");
        CPH.StreamDeckShowOk(originButtonId);
        CPH.TwitchPredictionResolve(currentPrediction.Id.ToString(), currentPrediction.Outcomes[userIndex].Id.ToString());
        CPH.SetGlobalVar("ongoingPrediction", false);
        return true;
    }
}