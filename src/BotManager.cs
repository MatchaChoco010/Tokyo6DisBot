using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class BotManager : IDisposable
{
    readonly List<Bot> _bots = new();
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly ICast[] _casts;
    readonly CeVIOAIClient _cevio = new();
    readonly Random _random = new();

    public BotManager(params ICast[] casts)
    {
        _casts = casts;
        foreach (var cast in casts)
        {
            _bots.Add(new Bot(cast, _cevio, _random));
        }
    }

    public void Dispose()
    {
        foreach (var speaker in _bots)
        {
            speaker.Dispose();
        }

        _cevio.Dispose();
    }

    public async Task StartAsync()
    {
        if (_bots.Count > 0)
        {
            _bots.First().MessageReceivedTask = msg =>
            {
                var content = msg.CleanContent;
                var splitMessage = content.Split(new[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                var command = splitMessage.Length > 0 ? splitMessage[0] : null;

                if (_bots.Any(b => command == b.CommandName()))
                {
                    return;
                }

                var channelBotsList = _bots.Where(b => b.ParticipateChannel != null)
                    .GroupBy(b => b.ParticipateChannel?.Id);
                foreach (var botsInOneChannel in channelBotsList)
                {
                    var bots = botsInOneChannel.ToList();
                    var index = _random.Next(bots.Count);
                    _ = bots[index].Yomiage(content, _cancellationTokenSource.Token);
                }
            };
        }

        var tasks = new List<Task>();
        foreach (var speaker in _bots)
        {
            tasks.Add(speaker.StartAsync(_cancellationTokenSource.Token));
        }

        tasks.Add(EscCancel.StartAsync(_cancellationTokenSource));

        await Task.WhenAny(tasks);
        Task.WaitAll(tasks.ToArray());
    }
}
