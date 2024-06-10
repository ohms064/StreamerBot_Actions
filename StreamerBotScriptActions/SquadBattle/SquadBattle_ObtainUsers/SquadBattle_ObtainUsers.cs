using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.SquadBattle.SquadBattle_ObtainUsers;

public class CPHInline : CPHInlineBase
{
    private const string WhisperMessage =
        "Envía los personajes que usarás separados por coma, intenta escribir los personajes por su nombre correcto y no por apodos. Por ejemplo: Pit, Duck Hunt, Mario, R.O.B.";

    public void Init()
    {
        // Clean all the groups maybe
        /* Maybe this still doesn't exist
        if (!CPH.GroupExists(""))
        {
            CPH.AddGroup(SquadGroupName);
        }
        */
        CPH.ClearUsersFromGroup("Squad Battle");
    }

    public bool Execute()
    {
        // your main code goes here
        CPH.LogDebug("Started");
        CPH.TryGetArg("rawInput", out string rawInput);

        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");

        CPH.ClearUsersFromGroup(squadGroupName);

        var userNames = rawInput.Split(' ');
        foreach (var userLogin in userNames)
        {
            var cleanUserLogin = userLogin.Trim('@');
            var twitchUserInfo = CPH.TwitchGetUserInfoByLogin(cleanUserLogin);
            if (twitchUserInfo == null)
            {
                CPH.ClearUsersFromGroup(squadGroupName);
                CPH.SendMessage($"Alguien dígale a Ohms que escribió mal a {cleanUserLogin} :v ");
                CPH.LogDebug($"User wasn't found {userLogin}");
                return false;
            }

            CPH.AddUserIdToGroup(twitchUserInfo.UserId, Platform.Twitch, squadGroupName);

            if (!CPH.SendWhisper(twitchUserInfo.UserName, WhisperMessage))
            {
                CPH.ShowToastNotification("Error en susurro", $"No se envío susurro a {twitchUserInfo.UserName}");
                CPH.SendMessage($"{twitchUserInfo.UserName} no te pude enviar mensaje! Envíame un susurro para activar nuestro chat, o que ohms te diga que otras opciones hay (o si escribió mal tu nombre por wey :v)");
                return true;
            }

            CPH.SetTwitchUserVarById(twitchUserInfo.UserId, "ready", false);
        }

        return true;
    }
}