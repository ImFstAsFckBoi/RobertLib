using System.Runtime.InteropServices;
using Discord;
using Discord.WebSocket;

namespace Robert_Framework;

internal static class BuiltinCommands
{
    public static MessageCommand ECHO = new MessageCommand("Echo", "For testing!\nEchoes everything received.", "$PREFIXecho ...", "echo", true, EchoLogic);
    public static MessageCommand HELP = new MessageCommand("Help", "Show all commands availble.", "$PREFIXhelp  or $PREFIXhelp <command>", "help", true, HelpLogic);


    public static Task EchoLogic(SocketMessage msg, MessageCommand msgCmd, Bot bot)
    {
        string outmsg = $"Message: {msg.ToString()}\n\nArgs:";
        foreach (var arg in msgCmd.ParseArguments(msg))
        {
            outmsg += " " + arg;
        }

        return msg.Channel.SendMessageAsync(outmsg);
    }


    public static Task HelpLogic(SocketMessage msg, MessageCommand msgCmd, Bot bot)
    {
        string outmsg = ">>> ";

        string[] args = msgCmd.ParseArguments(msg);

        if (args.Length > 0)
        {
            foreach(var arg in args)
            {
                foreach(var cmd in bot.MessageCommands.Where( (cmd) => cmd.CommandString == arg || bot.Prefix + cmd.CommandString == arg))
                {
                    outmsg += $"**{cmd.Label}**  |  Usage: *{cmd.UsagePrompt.Replace("$PREFIX", bot.Prefix.ToString())}*\n{cmd.Description}\n\n";
                }
            }
        }
        else
        {
            outmsg += $"Use *{bot.Prefix}help <command>* for command description.\n\n";
            foreach(var cmd in bot.MessageCommands)
            {
                outmsg += $"**{(cmd.Label+":**").PadRight(20)} *{(cmd.RequirePrefix ? bot.Prefix : "")}{cmd.CommandString}*\n";
            }
        }

        return msg.Channel.SendMessageAsync(outmsg);
    }
}


public class Bot
{
    public DiscordSocketClient Client { get; private set; }
    public List<MessageCommand> MessageCommands;
    public List<ReactionCommand> ReactCommands;

    public char Prefix;


    public Bot(string token, char prefix, bool logging = true)
    {
        Client = new DiscordSocketClient();
        
        bool ready = false;
        Client.Ready += () =>
        {
            ready = true;
            return Task.CompletedTask;
        };
        
        Task login = Client.LoginAsync(TokenType.Bot, token);

        Prefix = prefix;

        MessageCommands = new List<MessageCommand>();
        ReactCommands = new List<ReactionCommand>();

        MessageCommands.Add(BuiltinCommands.HELP);
        MessageCommands.Add(BuiltinCommands.ECHO);

        if (logging)
            Client.Log += Logger;

        Client.MessageReceived  += MessageHandler;
        Client.ReactionAdded    += ReactionAddHandler;
        Client.ReactionRemoved  += ReactionRemoveHandler;
        Client.ReactionsCleared += ReactionClearHandler;

        login.Wait();
        Client.StartAsync().Wait();


        while(!ready) Task.Delay(100).Wait();
    }


    /// <summary>
    /// Function for logging.
    /// Can be overridden for custom logic or left as is for basic logging.
    /// </summary>
    /// <param name="msg">Object contaning log message.</param>
    public virtual Task Logger(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }


    /// <summary>
    /// Function that handels all incoming messages. Can be overloaded for custom logic, but base.MessageHandler should always be called.
    /// </summary>
    /// <param name="msg">Object conationing incoming message.</param>
    /// <returns></returns>    
    public virtual Task MessageHandler(SocketMessage msg)
    {
        if(msg.ToString().Length == 0)
            return Task.CompletedTask;

        if (msg.ToString()[0] == Prefix)
        {
            foreach (var cmd in MessageCommands.Where(cmd => cmd.RequirePrefix))
            {
                try
                {
                    if (msg.ToString().Substring(1, cmd.CommandString.Length) == cmd.CommandString)
                    {
                        cmd.Logic(msg, cmd, this);
                    }
                }
                catch(ArgumentOutOfRangeException)
                {
                    continue;
                }
            }
        }
        else
        {
            foreach (var cmd in MessageCommands.Where(cmd => !cmd.RequirePrefix))
            {
                 try
                {
                    if (msg.ToString().Substring(0, cmd.CommandString.Length) == cmd.CommandString)
                    {
                        cmd.Logic(msg, cmd, this);
                    }
                }
                catch(ArgumentException)
                {
                    continue;
                }
            }
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// Function that handels all added reaction to a message. Can be overloaded for custom logic, but base.ReactionAddHandler should always be called.
    /// </summary>
    /// <param name="userMsg">See Discord.NET documentation</param>
    /// <param name="msgCh">See Discord.NET documentation</param>
    /// <param name="reaction">See Discord.NET documentation</param>
    /// <returns>Returns a Task</returns>
    public virtual Task ReactionAddHandler(Cacheable<IUserMessage, ulong> userMsg, Cacheable<IMessageChannel, ulong> msgCh, SocketReaction reaction)
    {
        foreach (ReactionCommand rCmd in ReactCommands)
        {
            if (rCmd.Emote == reaction.Emote.Name)
            {
                rCmd.ReactLogic.Invoke(userMsg.Value, msgCh.Value, reaction, rCmd, this);
            }
        }

        return Task.CompletedTask;
    }
    /// <summary>
    /// Function that handels all removed reactions from a message. Can be overloaded for custom logic, but base.MessageHandler(msg) should always be called.
    /// </summary>
    /// <param name="userMsg">See Discord.NET documentation</param>
    /// <param name="msgCh">See Discord.NET documentation</param>
    /// <param name="reaction">See Discord.NET documentation</param>
    /// <returns>Returns a Task</returns>
     public virtual Task ReactionRemoveHandler(Cacheable<IUserMessage, ulong> userMsg, Cacheable<IMessageChannel, ulong> msgCh, SocketReaction reaction)
    {
        foreach (ReactionCommand rCmd in ReactCommands.Where(x => x.RemoveReactLogic != null))
        {
            
            if (rCmd.Emote == reaction.Emote.Name)
            {
                if(rCmd.RemoveReactLogic == null)
                    continue;
                rCmd.RemoveReactLogic.Invoke(userMsg.Value, msgCh.Value, reaction, rCmd, this);
            }
        }
        return Task.CompletedTask;
    }

     public virtual Task ReactionClearHandler(Cacheable<IUserMessage, ulong> userMsg, Cacheable<IMessageChannel, ulong> msgCh)
    {
        foreach (ReactionCommand rCmd in ReactCommands.Where(x => x.ClearReactLogic != null))
        {
            if(rCmd.ClearReactLogic == null)
                continue;
            rCmd.ClearReactLogic.Invoke(userMsg.Value, msgCh.Value, rCmd, this);
        }
        return Task.CompletedTask;
    }
}
