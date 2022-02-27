using System.Net.Sockets;
namespace Robert_Framework;


using Discord.WebSocket;
using Discord;

public delegate Task MessageCommadDelegate(SocketMessage msg, MessageCommand msgCmd, Bot bot);
public delegate Task ReactionCommadDelegate(IUserMessage userMsg, IMessageChannel msgCh, SocketReaction reaction, ReactionCommand reactCmd, Bot bot);
public delegate Task ReactionsClearDelegate(IUserMessage userMsg, IMessageChannel msgCh, ReactionCommand reactCmd, Bot bot);
public abstract class Command
{
    public string Label { get; private set; }
    public string Description { get; private set; }
    public string UsagePrompt { get; private set; }

    public Command(string label, string description, string usage)
    {
        Label = label;
        Description = description;
        UsagePrompt = usage;
    }

}

public class MessageCommand : Command
{
    public string CommandString { get; private set; }
    public bool RequirePrefix { get; private set; }
    public MessageCommadDelegate Logic { get; private set; }

    public MessageCommand(string                label, 
                          string                description,
                          string                usage,
                          string                cmdStr, 
                          bool                  reqPrefix,
                          MessageCommadDelegate logic)
        : base(label, description, usage)
    {
        CommandString = cmdStr;
        RequirePrefix = reqPrefix;
        Logic = logic;
    }

    public string[] ParseArguments(SocketMessage msg)
    {
        int cmdLen = CommandString.Length + (RequirePrefix ? 1 : 0);

        string argStr = msg.ToString().Substring(cmdLen);
        return argStr.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    }
}

public class ReactionCommand : Command
{
    public ReactionCommadDelegate ReactLogic { get; private set; }
    public ReactionCommadDelegate? RemoveReactLogic { get; private set; }
    public ReactionsClearDelegate? ClearReactLogic { get; private set; }
    public string Emote;
    public ReactionCommand(string                  label,
                           string                  description,
                           string                  usage,
                           string                  emote,
                           ReactionCommadDelegate  reactLogic,
                           ReactionCommadDelegate? removeReactLogic = null,
                           ReactionsClearDelegate? clearReactLogic = null)
        : base(label, description, usage)
    {
        ReactLogic = reactLogic;
        RemoveReactLogic = removeReactLogic;
        ClearReactLogic = clearReactLogic;
        Emote = emote;
    }
}
