using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.Memeficator.MessageBoxFilter;

using System.Text;
public class CPHInline : CPHInlineBase
{
    private const int MaxStringLength = 25;
    private const int MaxRowsLength = 5;
    public bool Execute()
    {
        // your main code goes here
        if(!CPH.TryGetArg("rawInput", out string rawInput)){
            return false;
        }
        var words = rawInput.Split();
        var builder = new StringBuilder();
        int currentSentenceLength = 0;
        int row = 0;

        foreach (var word in words)
        {
            if (currentSentenceLength > MaxStringLength)
            {
                builder.Append("\n");
                currentSentenceLength = 0;
                row++;
                if (row >= MaxRowsLength)
                {
                    break;
                }
            }

            builder.Append($"{word} ");
            currentSentenceLength += word.Length + 1;
        }
        
        CPH.SetArgument("messageText", builder.ToString());
        
        return true;
    }
}
