using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SquadBattle.PredictionForEvent;

using System.Collections.Generic;
public class CPHInline : CPHInlineBase
{
    private List<string> _predictionResultIds;
    private string _currentPredictionId;
    private bool _ongoingPrediction;
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

        _currentPredictionId = CPH.TwitchPredictionCreate(predictionTitle, predictionOptions, 300);
        _ongoingPrediction = true;
        //CPH.SetGlobalVar("currentPrediction", predictionId);
        return true;
    }

    public bool SetPredictionsId()
    {
        if (!_ongoingPrediction)
        {
            return true;
        }

        if (!CPH.TryGetArg("prediction.outcome0.id", out string leftUserPredictionId))
        {
            return false;
        }

        if (!CPH.TryGetArg("prediction.outcome1.id", out string rightUserPredictionId))
        {
            return false;
        }

        _predictionResultIds = new List<string>
        {
            leftUserPredictionId,
            rightUserPredictionId
        };
        return true;
    }

    public bool TryCancelPrediction()
    {
        if (!_ongoingPrediction)
        {
            return true;
        }

        CPH.TwitchPredictionCancel(_currentPredictionId);
        _currentPredictionId = "";
        _ongoingPrediction = false;
        return true;
    }

    public bool SetPredictionResult()
    {
        if (!_ongoingPrediction)
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
        if (userStocksLeft != 0 && userStocksRight != 0)
        {
            CPH.StreamDeckShowAlert(originButtonId);
            CPH.ShowToastNotification("prediction_unfinished", "Error en la predicción!", "El score aun no es cero", "", "D:/Streams/Streamer.bot-x64-0.2.2/streamer.bot.png");
            return false;
        }

        //var predictionId = CPH.GetGlobalVar<string>("currentPrediction");
        CPH.StreamDeckShowOk(originButtonId);
        CPH.TwitchPredictionResolve(_currentPredictionId, _predictionResultIds[userIndex]);
        CPH.StreamDeckShowOk(originButtonId);
        _ongoingPrediction = false;
        return true;
    }
}