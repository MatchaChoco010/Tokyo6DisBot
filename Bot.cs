using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

public class Bot: IDisposable
{
     readonly DiscordSocketClient _client = new(new DiscordSocketConfig()
     {
         GatewayIntents = GatewayIntents.All
     });
     readonly CommandService _commands = new();
     readonly KoharuRikka _koharu = new();
     readonly Random _rand = new();

     IServiceProvider BuildServiceProvider() => new ServiceCollection()
         .AddSingleton(_client)
         .AddSingleton(_commands)
         .AddSingleton(_koharu)
         .AddSingleton(_rand)
         .AddSingleton(this)
         .BuildServiceProvider();

     public IAudioClient? AudioClient { get; set; }
     public AudioOutStream? AudioOutStream { get; set; }

     Task Log(LogMessage msg)
     {
          Console.WriteLine(msg.ToString());
          return Task.CompletedTask;
     }

     async Task ReplyMessage(SocketUserMessage msg)
     {
         var texts = new[]
         {
             "どうしたの？",
             "んー？",
             "なになにー？",
             "ばかっ",
             "おっはよ～",
             "よろしくね！",
             "ありがとう！",
             "そうそう！",
             "そうなんですよ！！",
             "うん",
             "うんうん",
             "うんうんうん",
             "そうなの！！",
             "エエエエエエー！！！",
             "六花よし！",
             "へくし",
             "六花ちゃんの意見が正しいね",
             "お前、うまそうだな・・・",
             "ラキストンもお腹減ったよね？",
         };
         var index = _rand.Next(texts.Length);
         await msg.Channel.SendMessageAsync($"{msg.Author.Mention} {texts[index]}");
     }

     void Yomiage(string message)
     {
         if (AudioClient is null || AudioOutStream is null) return;

         _ = Task.Run(async () =>
         {
             var messageReplaced = Regex.Replace(message, @"https?://[\w!?/+\-_~;.,*&@#$%()'[\]]+", "URL");
             await _koharu.Speak(messageReplaced, AudioOutStream);
         });
     }

     async Task ReceiveMessage(SocketMessage msg)
     {
          if (msg is not SocketUserMessage message) return;

          int argPos = 0;
          if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
          {
               await ReplyMessage(message);
          }

          if (message.CleanContent.StartsWith("rikka-chan"))
          {
              var context = new SocketCommandContext(_client, message);
              await _commands.ExecuteAsync(context: context, argPos: argPos, services: BuildServiceProvider());
          }
          else
          {
              Yomiage(message.CleanContent);
          }
     }

     public async Task Start()
     {
          await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: BuildServiceProvider());
          _client.Log += Log;
          _client.MessageReceived += ReceiveMessage;

          var token = File.ReadAllText("token.txt").Trim('\n');
          await _client.LoginAsync(TokenType.Bot, token);
          await _client.StartAsync();

          await _client.SetStatusAsync(UserStatus.Online);
          await _client.SetGameAsync("ヨーグルト探しの旅");

          _koharu.ComponentHappy = 100;
          _koharu.ComponentNormal = 0;

          while (true)
          {
               if (await Console.In.ReadLineAsync() == "quit")
               {
                    break;
               }
          }

          await _client.SetStatusAsync(UserStatus.Idle);

          await _client.LogoutAsync();
          await _client.StopAsync();
     }

     public void Dispose()
     {
         AudioOutStream?.Dispose();
         AudioClient?.Dispose();
         _koharu.Dispose();
         _client.Dispose();
     }
}
