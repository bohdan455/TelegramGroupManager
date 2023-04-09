using bot.Handlers;
using bot.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics.Metrics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

//add config file
var builder = new ConfigurationBuilder()
    .AddJsonFile($"appconfig.json", false, true);

var config = builder.Build();

// get access to bot
var botClient = new TelegramBotClient(config["Token"]);

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = new UpdateType[]
    {
        UpdateType.Message
    }
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { })
        return;
    var chatId = update.Message.Chat.Id;
    var commandHandler = new CommandsHandler(update);
    var UserPostHandler = new UserPostHandler(update);

    commandHandler.Add("/start", async () =>
    {
        await botClient.SendTextMessageAsync(chatId,
            "Привіт, якщо хочеш щоб твій прікол або текст опинився в групі просто пришли його сюда",
            cancellationToken: cancellationToken);
    });
    UserPostHandler.AddMessageType(MessageType.Text,async () =>
    {
        await botClient.SendTextMessageAsync(config["AdminId"],
                "Новий пост від юзера: " + $"<a href=\"tg://user?id={update.Message.Chat.Id}\">{update.Message.Chat.FirstName}</a>:",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(config["AdminId"],
                "Текстове повідомлення",
                cancellationToken: cancellationToken);
    });

    UserPostHandler.AddMessageType(MessageType.Photo, async () =>
    {
        await botClient.SendTextMessageAsync(config["AdminId"],
                "Новий пост від юзера: " + $"<a href=\"tg://user?id={update.Message.Chat.Id}\">{update.Message.Chat.FirstName}</a>:",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        await botClient.SendPhotoAsync(
            chatId: config["AdminId"],
            photo: "AgACAgIAAxkBAAEffYtkMzGmMlxqWahRJCm48bNnZsLZkwAC2scxG7VGmEkqUtuXwrUN9wEAAwIAA3kAAy8E",
            cancellationToken: cancellationToken);
    });
    await UserPostHandler.SendMessage();
    await commandHandler.SendMessage();

    await botClient.SendTextMessageAsync(chatId,
            "Після перегляду посту адміністраторами він опиниться в групі❤️",
             cancellationToken: cancellationToken);


}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}