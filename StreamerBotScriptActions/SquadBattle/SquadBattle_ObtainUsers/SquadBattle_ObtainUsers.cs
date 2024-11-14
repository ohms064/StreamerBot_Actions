using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.SquadBattle.SquadBattle_ObtainUsers;

public class CPHInline : CPHInlineBase
{
    private const string LeftTeam = "teamLeft";
    private const string RightTeam = "teamRight";
    private const string LeftTeamName = "leftTeamName";
    private const string RightTeamName = "rightTeamName";
    private const string WhisperMessage =
        "Envía los personajes que usarás separados por coma, intenta escribir los personajes por su nombre correcto y no por apodos. Por ejemplo: Pit, Duck Hunt, Mario, R.O.B.";

    public bool Execute()
    {
        // your main code goes here
        CPH.LogDebug("Started");
        CPH.TryGetArg("rawInput", out string rawInput);
        CPH.ClearUsersFromGroup(LeftTeam);
        CPH.ClearUsersFromGroup(RightTeam);

        var userNames = rawInput.Split(' ');

        if (userNames.Length != 2)
        {
            return false;
        }

        return AddToTeam(userNames[0], LeftTeam, LeftTeamName) && AddToTeam(userNames[1], RightTeam, RightTeamName);

        bool AddToTeam(string userLogin, string teamName, string teamKey)
        {
            var cleanUserLogin = userLogin.Trim('@');
            var twitchUserInfo = CPH.TwitchGetUserInfoByLogin(cleanUserLogin);
            if (twitchUserInfo == null)
            {
                CPH.SendMessage($"Alguien dígale a Ohms que escribió mal a {cleanUserLogin} :v ");
                CPH.LogDebug($"User wasn't found {userLogin}");
                return false;
            }

            CPH.AddUserIdToGroup(twitchUserInfo.UserId, Platform.Twitch, teamName);
            CPH.SetTwitchUserVarById(twitchUserInfo.UserId, "ready", false);
            CPH.SetGlobalVar(teamKey, twitchUserInfo.UserName);

            var whisper = CPH.GetGlobalVar<bool>("sendWhisper");

            if (!whisper)
            {
                return true;
            }

            if (CPH.SendWhisper(twitchUserInfo.UserName, WhisperMessage))
            {
                return true;
            }
            
            CPH.ShowToastNotification("Error en susurro", $"No se envío susurro a {twitchUserInfo.UserName}");
            CPH.SendMessage($"{twitchUserInfo.UserName} no te pude enviar mensaje! Envíame un susurro para activar nuestro chat, o que ohms te diga que otras opciones hay (o si escribió mal tu nombre por wey :v)");

            return true;
        }
    }
}