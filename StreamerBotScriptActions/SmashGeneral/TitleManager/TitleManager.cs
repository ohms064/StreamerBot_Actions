using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.SmashGeneral.TitleManager;

public class CPHInline : CPHInlineBase
{
    private const string LeftTeamName = "leftTeamName";
    private const string RightTeamName = "rightTeamName";
    private const string GameMode = "currentGameMode";
    public bool Execute()
    {
        var leftTeamName = CPH.GetGlobalVar<string>(LeftTeamName);
        var rightTeamName = CPH.GetGlobalVar<string>(RightTeamName);
        var currentGame = CPH.GetGlobalVar<string>(GameMode);

        CPH.SetChannelTitle($"El Coliseohms: {currentGame}! {leftTeamName} vs {rightTeamName}");
        
        return true;
    }
}