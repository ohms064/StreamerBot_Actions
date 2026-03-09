using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace StreamerBotScriptActions.CrewBattle.CrewBattlePlayersSetup;

using System.Collections.Generic;
using System.Linq;
using FuzzySharp;

public class CPHInline : CPHInlineBase
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }
        
        // For now just passing directly the rawinput, probably will need to differentiate commands like in squad battles.
        CPH.SetArgument("teamName", rawInput);
        return true;
    }
}