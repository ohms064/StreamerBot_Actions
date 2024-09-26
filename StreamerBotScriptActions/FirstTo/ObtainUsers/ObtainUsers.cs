using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.FirstTo.ObtainUsers;

// ReSharper disable once InconsistentNaming
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
        CPH.ClearUsersFromGroup("First To");
    }

    public bool Execute()
    {
        // your main code goes here
        CPH.LogDebug("Started");
        CPH.TryGetArg("rawInput", out string rawInput);

        var squadGroupName = CPH.GetGlobalVar<string>("currentEventGroup");

        CPH.ClearUsersFromGroup(squadGroupName);

        var ftArgs = rawInput.Split(' ');

        if (ftArgs.Length < 3)
        {
            return false;
        }
        
        if (!int.TryParse(ftArgs[0], out int ftResult))
        {
            return false;
        }
        
        var userNames = new string[] { ftArgs[1], ftArgs[2] };
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

    public bool SetArgsForUpdateCharacterSkin()
    {
        
        if (!CPH.TryGetArg<string>("rawInput", out var rawInput))
        {
            return false;
        }

        var args = rawInput.Split();
        if (args.Length < 2)
        {
            return false;
        }

        var character = "";
        for (var i = 1; i < args.Length; i++)
        {
            character += $"{args[i]} ";
        }
        if (!int.TryParse(args[0], out var skin))
        {
            CPH.SendMessage($"Primero escribe el skin que quieres seguido del nombre del personaje");
            return false;
        }

        if (skin < 1 || skin > 8)
        {
            CPH.SendMessage("Número de skin es inválido. Debe ser menor que 8 o mayor que 1");
            return false;
        }

        skin--;
        
        CPH.SetArgument("character", character);
        CPH.SetArgument("skin", skin);

        return true;
    }

    public bool UpdateCharacterSkin()
    {
        
        if (!CPH.TryGetArg<string>("userId", out var userId))
        {
            return false;
        }
        
        if (!CPH.TryGetArg<string>("character", out var character))
        {
            return false;
        }
        
        if (!CPH.TryGetArg<int>("skin", out var skin))
        {
            return false;
        }
        
        CPH.SetTwitchUserVarById(userId, $"skin_{character}", skin);

        return true;
    }
}