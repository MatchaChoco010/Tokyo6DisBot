using System;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

public class Commands : ModuleBase
{
    readonly DiscordSocketClient _client;
    readonly CommandService _commands;
    readonly KoharuRikka _koharu;
    readonly Random _random;

    readonly Bot _bot;

    public Commands(
        DiscordSocketClient client,
        CommandService commands,
        KoharuRikka koharu,
        Random random,
        Bot bot)
    {
        _client = client;
        _commands = commands;
        _koharu = koharu;
        _random = random;
        _bot = bot;
    }

    async Task HelpMessage()
    {
        var embed = new EmbedBuilder
        {
            Title = "小春六花の呼び出し方",
            Fields =
            {
                new EmbedFieldBuilder { Name = "rikka-chan help", Value = "このヘルプメッセージを出します", IsInline = false, },
                new EmbedFieldBuilder { Name = "rikka-chan", Value = "小春六花ちゃんがランダムにメッセージを投稿します", IsInline = false, },
                new EmbedFieldBuilder { Name = "rikka-chan yogurt", Value = "小春六花ちゃんがヨーグルトを手に入れます", IsInline = false, },
                new EmbedFieldBuilder
                {
                    Name = "rikka-chan start", Value = "小春六花ちゃんがメッセージの読み上げを開始します", IsInline = false,
                },
                new EmbedFieldBuilder
                {
                    Name = "rikka-chan stop", Value = "小春六花ちゃんがメッセージの読み上げを停止します", IsInline = false,
                },
            },
            Color = new Color(175, 238, 238),
        };
        await Context.Channel.SendMessageAsync(embed: embed.Build());
    }

    async Task RandomYogurt()
    {
        string GetMessage()
        {
            var w = _random.NextDouble();
            return w switch
            {
                < 0.5 => $"小春六花ちゃんはヨーグルトを{_random.Next(1, 6)}個手に入れました。",
                < 0.55 => $"小春六花ちゃんはヨーグルトを{_random.Next()}個手に入れました。",
                < 0.7 => $"小春六花ちゃんはずんだ餅を{_random.Next(10, 15)}個手に入れました。",
                < 0.9 => $"小春六花ちゃんはヨーグルトを{_random.Next(1, 6)}個勝手に食べられました。",
                _ => $"小春六花ちゃんはヨーグルトの空容器を{_random.Next(1, 6)}個発見しました。"
            };
        }

        var embed = new EmbedBuilder
        {
            Title = "ヨーグルトちゃれんじ", Description = GetMessage(), Color = new Color(175, 238, 238),
        };
        await Context.Channel.SendMessageAsync(embed: embed.Build());
    }

    async Task StartYomiage(SocketUserMessage msg)
    {
        var channel = (msg.Author as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await msg.Channel.SendMessageAsync("ボイスちゃんねるに入ってから呼んでね！");
            return;
        }

        _bot.AudioClient = await channel.ConnectAsync();
        _bot.AudioOutStream = _bot.AudioClient.CreatePCMStream(AudioApplication.Mixed, 48000);
        await msg.Channel.SendMessageAsync(" 読み上げを開始します！");
    }

    async Task StopYomiage()
    {
        if (_bot.AudioClient is null)
        {
            await Context.Channel.SendMessageAsync(" まだ読み上げをしていないよ。");
            return;
        }

        await Context.Channel.SendMessageAsync(" 読み上げをしゅーりょーします！");
        await _bot.AudioClient.StopAsync();
        _bot.AudioClient.Dispose();
        _bot.AudioClient = null;
    }

    async Task RandomMessage()
    {
        var texts = new[]
        {
            "小春六花です", "よろしくおねがいしますっ", "またお会いしましたね！", "おっはよ～", "元気にしてた？", "小春六花です。今日も～　よろしくおねがいします！", "ねえ、ねぇねぇねぇねぇ",
            "また会おうね！", "そうなんですよ！！", "えぇえぇえぇえええええ", "違うのに・・・", "ちっ(リアル目の舌打ち)", "了解！", "こらー！！！！", "っはああああああ", "痛っ",
            "ばか！", "うえーん、へたくそ～！", "は”あ”あ”あ”あ”あ”あ”？", "おめでとう！", "最低・・・", "ごめんね！", "今日はどこに連れて行ってくれるんですか？",
            "私がナビゲートしますね！", "安全運転でお願いします！", "この先、しばらく道なりです", "それじゃあ再びしゅっぱーつ！", "皆さん、こんにちはー！", "皆さん、こんにちは・・・",
            "あ、ありがと。。うれし。。", "ひどい・・・。泣きそうです。。", "ひっどーい。じゃあもう帰ります！", "しょうがないなぁ。。と言うとでも思ったか・・・", "ぴんぽんぱんぽーん",
            "前回のおさらい", "次回予告ー！", "まったみってね～！", "ばいばい！", "小春六花がお送りしました", "正面よし！", "後方よし！", "六花よし！", "安全確認よし！",
            "シートベルト、よし！", "今回の目的地はどこですか？", "空を飛んでください", "ぐう・・・ぐう・・・", "ちゃんと計画立ててきたんですか？", "そして、私と一緒に、地球を守って！",
            "地図が間違ってる、これは。", "なんか歌いたくなっちゃった・・・", "ストーップ！はい、オッケー！", "撃てー！！！", "お願い、当たってー！！", "突撃－！！", "うふふ、ワンキル",
            "私、こういうの得意なんですけど！", "ついに目覚めし、我が能力・・・　ククク", "エターナル・フォース・ブリザード！", "撃～た～な～い～で～！", "もう、私の言う事聞いてよー！",
            "わたし、あなたのことすきじゃないいいいい", "お願い、生き返って！", "すごいすごい！！", "私にかして？", "ミッションコンプリート！", "諦めるのはまだはやいですよ！", "あいたっ",
            "ぶぇえ", "うわあああああやーらーれーたああああ", "ぐえええええ", "いってーなこのやろー！", "えーん、いーじーめーるー", "とりゃあああああああああ", "ご飯がたけたよ～！",
            "ごはん♪ごはん♪", "おーいしー！！", "これは・・大物の予感！！", "今日は大漁でしたね！楽しかった～！", "存在感無いので、空気かと思いました・・・", "え？いたんですか？",
            "もう一度おねがいします", "ちゃんとやって？", "了解であります！！", "助けて！！ラキストーン！！", "ねぇねぇラキストン", "私の言うことが正しいよね？", "ラキストンもうんって言った",
            "ラキストンもお腹減ったって", "六花ちゃんの言うことが圧倒的に正しい", "じゃじゃーん", "またヨーグルトが食べられた～！", "よおぐるとぉ～",
        };
        var index = _random.Next(texts.Length);
        await Context.Channel.SendMessageAsync(texts[index]);
    }

    [Command("rikka-chan", RunMode = RunMode.Async)]
    public async Task RikkaChanCommand(string subCommand)
    {
        var message = Context.Message as SocketUserMessage;
        if (message is null) return;
        switch (subCommand)
        {
             case "help":
             case "h":
                  await HelpMessage();
                  break;
             case "yogurt":
                  await RandomYogurt();
                  break;
             case "start":
                  await StartYomiage(message);
                  break;
             case "stop":
                  await StopYomiage();
                  break;
             default:
                  break;
        }
    }

    [Command("rikka-chan", RunMode = RunMode.Async)]
    public async Task RikkaChanCommand()
    {
        await RandomMessage();
    }
}
