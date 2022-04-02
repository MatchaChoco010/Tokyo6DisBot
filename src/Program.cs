using var bot = new BotManager(new KoharuRikka(), new NatsukiKarin());
bot.StartAsync().Wait();
