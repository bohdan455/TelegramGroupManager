using bot.Handlers;
using bot.Middleware;
using Microsoft.Extensions.Configuration;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

//add config file
var configBuilder = new ConfigurationBuilder()
    .AddJsonFile($"appconfig.json", false, true);

var config = configBuilder.Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

// get access to bot
var botClient = new TelegramBotClient(config["Token"]);

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = new UpdateType[]
    {
        UpdateType.Message,
        UpdateType.CallbackQuery
    }
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Log.Information($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
Log.CloseAndFlush();
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    string adminId = config["AdminId"];
    string channelId = config["ChannelId"];
    if (update.CallbackQuery is not null)
    {
        if (update.CallbackQuery.Message.Chat.Id == long.Parse(adminId))
        {
            switch (update.CallbackQuery.Data)
            {
                case "sendTextToGroup":
                    await botClient.SendTextMessageAsync(channelId,
                        update.CallbackQuery.Message.Text,
                        cancellationToken: cancellationToken);
                    break;
                case "sendPhotoToGroup":
                    await botClient.SendPhotoAsync(
                        chatId: channelId,
                        photo: update.CallbackQuery.Message.Photo[0].FileId,
                        cancellationToken: cancellationToken);
                    break;
                //case "sendVideoToGroup":
                //    await botClient.SendVideoAsync(
                //        chatId: channelId,
                //        video: update.CallbackQuery.Message.Video.FileId,
                //        cancellationToken: cancellationToken);
                //    break;
                case "declinePost":
                    break;
                default:
                    await botClient.SendTextMessageAsync(adminId,
                        "Щось пішло не так",
                        cancellationToken: cancellationToken);
                    break;
            }
            await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id,
                update.CallbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
            await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id,
                update.CallbackQuery.Message.MessageId-4,
                cancellationToken: cancellationToken);

        }
    }
    if (update.Message is not { })
        return;
    Log.Information("New message from {user}, message body: {body}",
        update.Message.Chat.Username,
        update.Message.Text ?? update.Message.Photo?[0]?.FileId /*?? update.Message.Video?.FileId */?? "Undefined type");

    var chatId = update.Message.Chat.Id;
    var commandHandler = new CommandsHandler(update);
    var UserPostHandler = new UserPostHandler(update);

    commandHandler.Add("/start", async () =>
    {
        await botClient.SendTextMessageAsync(chatId,
            "Привіт, якщо хочеш щоб твій прікол або текст опинився в групі просто пришли його сюда",
            cancellationToken: cancellationToken);
    });
    UserPostHandler.AddUndefinedTypeHandler(async () =>
    {
        await botClient.SendTextMessageAsync(chatId, "Неправильний формат файлу", cancellationToken: cancellationToken);
    });
    UserPostHandler.AddResponceToUser(async () =>
    {
        await botClient.SendTextMessageAsync(adminId,
        "Новий пост від юзера: " + $"<a href=\"tg://user?id={update.Message.Chat.Id}\">{update.Message.Chat.FirstName}</a>:",
        parseMode: ParseMode.Html,
        cancellationToken: cancellationToken);

        await botClient.SendTextMessageAsync(chatId,
        "Після перегляду посту адміністраторами він опиниться в групі❤️",
         cancellationToken: cancellationToken);
    });
    UserPostHandler.AddMessageType(MessageType.Text, async () =>
    {
        InlineKeyboardMarkup inlineKeyboard = new(new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Відправити✅", callbackData: $"sendTextToGroup"),
            InlineKeyboardButton.WithCallbackData(text: "Видалити❌", callbackData: "declinePost")
            
        });

        await botClient.SendTextMessageAsync(adminId,
                update.Message.Text,
                cancellationToken: cancellationToken,
                replyMarkup:inlineKeyboard);
    });

    UserPostHandler.AddMessageType(MessageType.Photo, async () =>
    {
        InlineKeyboardMarkup inlineKeyboard = new(new[]
{
            InlineKeyboardButton.WithCallbackData(text: "Відправити✅", callbackData: $"sendPhotoToGroup"),
            InlineKeyboardButton.WithCallbackData(text: "Видалити❌", callbackData: "declinePost")

        });
        await botClient.SendPhotoAsync(
            chatId: adminId,
            photo: update.Message.Photo[0].FileId,
            cancellationToken: cancellationToken,
            replyMarkup:inlineKeyboard);
    });
//    UserPostHandler.AddMessageType(MessageType.Video, async () =>
//    {
//        InlineKeyboardMarkup inlineKeyboard = new(new[]
//{
//            InlineKeyboardButton.WithCallbackData(text: "Відправити✅", callbackData: $"sendVideoToGroup"),
//            InlineKeyboardButton.WithCallbackData(text: "Видалити❌", callbackData: "declinePost")

//        });
//        await botClient.SendVideoAsync(
//            chatId: adminId,
//            video: update.CallbackQuery.Message.Video.FileId,
//            cancellationToken: cancellationToken,
//            replyMarkup: inlineKeyboard);
//    });
    if (chatId != long.Parse(adminId))
        await UserPostHandler.SendMessage();

    await commandHandler.SendMessage();
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Log.Information(ErrorMessage);
    return Task.CompletedTask;
}