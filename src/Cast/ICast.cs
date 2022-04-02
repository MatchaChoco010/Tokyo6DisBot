using Discord;

public interface ICast
{
    string GetName();
    string TokenFileName();
    string GameName();
    string CommandName();
    string[] GetReplyTexts();
    Embed GetHelpEmbedMessage();
    string[] GetRandomMessages();
    string GetRouletteCommandName();
    Embed GetRouletteMessage();
    string StartBeforeParticipateMessage();
    string StartMessage();
    string StopBeforeParticipateMessage();
    string StopMessage();
}
