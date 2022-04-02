using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using NAudio.Wave;

public sealed class Bot
{
    readonly ICast _cast;
    readonly CeVIOAIClient _cevio;

    readonly DiscordSocketClient _client = new(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates
    });

    readonly CommandService _commands = new();
    readonly Random _random;

    public Bot(ICast cast, CeVIOAIClient cevio, Random random)
    {
        _cast = cast;
        _cevio = cevio;
        _random = random;
    }

    IAudioClient? AudioClient { get; set; }
    AudioOutStream? AudioOutStream { get; set; }
    public IVoiceChannel? ParticipateChannel { get; private set; }
    public Action<SocketUserMessage>? MessageReceivedTask { private get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += Log;
        _client.MessageReceived += ReceiveMessage;

        var token = File.ReadAllText(_cast.TokenFileName()).Trim('\n');

        try
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetGameAsync(_cast.GameName());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(50, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (AudioClient != null)
            {
                await AudioClient.StopAsync();
                AudioClient.Dispose();
                AudioClient = null;
            }

            await _client.SetStatusAsync(UserStatus.Idle);

            await _client.LogoutAsync();
            await _client.StopAsync();
        }
    }

    public async Task Yomiage(string message, CancellationToken cancellationToken)
    {
        if (AudioClient is null || AudioOutStream is null)
        {
            return;
        }

        var messageReplaced = Regex.Replace(message, @"https?://[\w!?/+\-_~;.,*&@#$%()'[\]]+", "URL");

        var pathArray = await _cevio.OutputWavFiles(_cast.GetName(), messageReplaced, cancellationToken);
        foreach (var path in pathArray)
        {
            try
            {
                using var reader = new WaveFileReader(path);
                using var waveStream = WaveFormatConversionStream.CreatePcmStream(reader);
                using var waveFormatConversionSteam =
                    new WaveFormatConversionStream(new WaveFormat(48000, 16, 2), waveStream);

                try { await waveFormatConversionSteam.CopyToAsync(AudioOutStream); }
                finally { await AudioOutStream.FlushAsync(cancellationToken); }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    public string CommandName()
    {
        return _cast.CommandName();
    }

    Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    async Task ReceiveMessage(SocketMessage msg)
    {
        if (msg is not SocketUserMessage message)
        {
            return;
        }

        var argPos = 0;
        if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
        {
            await ReplyMessage(message);
            return;
        }

        var content = message.CleanContent;
        var splitMessage = content.Split(new[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
        var command = splitMessage.Length > 0 ? splitMessage[0] : null;
        var subCommand = splitMessage.Length > 1 ? splitMessage[1]! : null;

        if (command == CommandName())
        {
            switch (subCommand)
            {
                case "help":
                case "h":
                    _ = Task.Run(() => HelpMessage(message));
                    break;
                case "start":
                    _ = Task.Run(() => StartYomiage(message));
                    break;
                case "stop":
                    _ = Task.Run(() => StopYomiage(message));
                    break;
                case null:
                    _ = Task.Run(() => RandomMessage(message));
                    break;
                default:
                    if (subCommand == GetRouletteCommandName())
                    {
                        _ = Task.Run(() => Roulette(message));
                    }

                    break;
            }
        }

        if (MessageReceivedTask != null)
        {
            MessageReceivedTask.Invoke(message);
        }
    }

    async Task ReplyMessage(SocketUserMessage msg)
    {
        var texts = _cast.GetReplyTexts();
        var index = _random.Next(texts.Length);
        await msg.Channel.SendMessageAsync($"{msg.Author.Mention} {texts[index]}");
    }

    public async Task HelpMessage(SocketUserMessage msg)
    {
        var ctx = new SocketCommandContext(_client, msg);
        var embed = _cast.GetHelpEmbedMessage();
        await ctx.Channel.SendMessageAsync(embed: embed);
    }

    public async Task RandomMessage(SocketUserMessage msg)
    {
        var ctx = new SocketCommandContext(_client, msg);
        var texts = _cast.GetRandomMessages();
        var index = _random.Next(texts.Length);
        await ctx.Channel.SendMessageAsync(texts[index]);
    }

    public string GetRouletteCommandName()
    {
        return _cast.GetRouletteCommandName();
    }

    public async Task Roulette(SocketUserMessage msg)
    {
        var ctx = new SocketCommandContext(_client, msg);
        var embed = _cast.GetRouletteMessage();
        await ctx.Channel.SendMessageAsync(embed: embed);
    }

    public async Task StartYomiage(SocketUserMessage msg)
    {
        var ctx = new SocketCommandContext(_client, msg);
        var channel = (msg.Author as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await ctx.Channel.SendMessageAsync(_cast.StartBeforeParticipateMessage());
            return;
        }

        AudioClient = await channel.ConnectAsync();
        AudioOutStream = AudioClient.CreatePCMStream(AudioApplication.Mixed, 48000);
        ParticipateChannel = channel;
        await msg.Channel.SendMessageAsync(_cast.StartMessage());
    }

    public async Task StopYomiage(SocketUserMessage msg)
    {
        var ctx = new SocketCommandContext(_client, msg);
        if (ParticipateChannel == null)
        {
            await ctx.Channel.SendMessageAsync(_cast.StopBeforeParticipateMessage());
            return;
        }

        ParticipateChannel = null;
        await ctx.Channel.SendMessageAsync(_cast.StopMessage());
        if (AudioClient != null)
        {
            await AudioClient.StopAsync();
            AudioClient.Dispose();
            AudioClient = null;
        }
    }

    public void Dispose()
    {
        AudioOutStream?.Dispose();
        AudioClient?.Dispose();
        _client.Dispose();
    }
}
