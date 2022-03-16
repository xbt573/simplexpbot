using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

/// <summary>
/// Class for a update handling
/// </summary>
public class Handlers
{
    /// <summary>
    /// Methods class property
    /// </summary>
    public Methods Methods { get; set; }

    /// <summary>
    /// Translations class property
    /// </summary>
    public Translations Translations { get; set; }

    public Handlers()
    {
        // Init logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        Methods = new Methods();
        Translations = new Translations();
    }


    /// <summary>
    /// Method for updates handling
    /// </summary>
    public async Task HandleUpdatesAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
        if (update.Type == UpdateType.Message)
        {
            await ProcessMessage(bot, update, cts);
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            await ProcessCallback(bot, update, cts);
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// Process message from user
    /// </summary>
    public async Task ProcessMessage(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
        // Check if message is null
        if (update.Message == null)
            return;

        if (update.Message.From == null)
            return;

        if (update.Message.Text == null)
            return;

        string? messageText = update.Message.Text;

        // Command is null if message text is null
        string[] command = messageText.Split(" ");

        // if (command == null) 
        // {
        //     Methods.AddXp(update);
        //     return;
        // }


        // TODO: Fix @
        // Processing command
        if (command[0].Contains("@"))
            // Delete bot tag from command
            command[0] = command[0].Split("@")[0];

        long chatId = update.Message.Chat.Id;
        long userId = update.Message.From.Id;
        int messageId = update.Message.MessageId;

        string lang = Methods.GetLang(chatId);

        switch (command[0])
        {
            // TODO: Add manual
            case "/help": goto case "/start";
            case "/start":
                await Start(chatId, messageId, lang, cts, bot); break;

            case "/xp":
                await Xp(chatId, userId, messageId, lang, cts, bot); break;

            case "/lang":
                await Lang(chatId, userId, messageId, update.Message.Chat.Type, lang, command, cts, bot); break;

            case "/top":
                await Top(chatId, messageId, lang, cts, bot); break;

            default:
                Methods.AddXp(userId, chatId, Methods.GetRandom());
                break;
        }
    }

    public async Task ProcessCallback(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
        if (update.CallbackQuery == null)
            return;

        if (update.CallbackQuery.Data == null)
            return;

        if (update.CallbackQuery.Message == null)
            return;

        bool russian = update.CallbackQuery.Data == "ru";
        bool english = update.CallbackQuery.Data == "en";

        if (russian || english)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;
            long userId = update.CallbackQuery.From.Id;

            if (update.CallbackQuery.Message.Chat.Type == ChatType.Private)
            {
                Methods.SetLang(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Data);

                await bot.AnswerCallbackQueryAsync(
                    callbackQueryId: update.CallbackQuery.Id,
                    text: Translations.LanguageChanged[update.CallbackQuery.Data],
                    cancellationToken: cts
                );

                return;
            }

            ChatMember chatMember = await bot.GetChatMemberAsync(chatId, userId);

            if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator)
            {
                Methods.SetLang(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Data);

                await bot.AnswerCallbackQueryAsync(
                    callbackQueryId: update.CallbackQuery.Id,
                    text: Translations.LanguageChanged[update.CallbackQuery.Data],
                    cancellationToken: cts
                );
            }
            else
            {
                await bot.AnswerCallbackQueryAsync(
                    callbackQueryId: update.CallbackQuery.Id,
                    text: Translations.LangChangeNoAccess[update.CallbackQuery.Data],
                    cancellationToken: cts
                );
            }
        }
    }

    /// <summary>
    /// Method for errors handling
    /// </summary>
    public Task HandleErrorsAsync(ITelegramBotClient bot, Exception exception, CancellationToken cts)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Log.Warning(ErrorMessage);
        Log.Warning(exception.StackTrace);
        return Task.CompletedTask;
    }

    public async Task Help(long chatId, int messageId, string lang, CancellationToken cts, ITelegramBotClient bot)
    {
        await Start(chatId, messageId, lang, cts, bot);
    }

    public async Task Start(long chatId, int messageId, string lang, CancellationToken cts, ITelegramBotClient bot)
    {
        await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: Translations.HelpCommand[lang],
                    cancellationToken: cts,
                    replyToMessageId: messageId
        );
    }

    public async Task Xp(long chatId, long userId, int messageId, string lang, CancellationToken cts, ITelegramBotClient bot)
    {
        int xp = Methods.GetSumXp(userId);
        int level = (int)(xp / 100);

        await bot.SendTextMessageAsync(
            chatId: chatId,
            text: $"*{Translations.Level[lang]}*: {level}\n*{Translations.XP[lang]}*: {xp}",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cts,
            replyToMessageId: messageId
        );
    }

    public async Task Lang(long chatId, long userId, int messageId, ChatType chatType, string lang, string[] command, CancellationToken cts, ITelegramBotClient bot)
    {
        if (command.Length == 1)
        {
            await bot.SendTextMessageAsync(
                chatId: chatId,
                text: Translations.KeyboardLangCommand[lang],
                replyToMessageId: messageId,
                replyMarkup: Translations.LangChangeMarkup
            );

            return;
        }

        bool en = command[1].Contains("en");
        bool ru = command[1].Contains("ru");

        bool commandlang = en || ru;

        ChatMember chatMember = await bot.GetChatMemberAsync(chatId, userId);

        if (chatType == ChatType.Private)
        {
            Methods.SetLang(chatId, command[1]);

            await bot.SendTextMessageAsync(
                chatId: chatId,
                text: Translations.LanguageChanged[command[1]],
                cancellationToken: cts,

                replyToMessageId: messageId
            );

            return;
        }

        if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator)
        {
            Methods.SetLang(chatId, command[1]);
            await bot.SendTextMessageAsync(
                chatId: chatId,
                text: Translations.LanguageChanged[command[1]],
                cancellationToken: cts,

                replyToMessageId: messageId
            );
        }

    }

    public async Task Top(long chatId, int messageId, string lang, CancellationToken cts, ITelegramBotClient bot)
    {
        await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: await Methods.GetTop(bot, chatId, lang, cts),
                    parseMode: ParseMode.MarkdownV2,
                    replyToMessageId: messageId
        );
    }
}