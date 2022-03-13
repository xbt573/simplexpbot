using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;

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

    /// <summary>
    /// Database class property
    /// </summary>
    public Database Database { get; set; }

    public Handlers()
    {
        // Init logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        Methods = new Methods();
        Translations = new Translations();

        if (!(System.IO.File.Exists("access.txt"))) 
        {
            Process.Start("cp", "../access.txt .");
        }

        Database = new Database("access.txt");
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

        // Processing command
        if (command[0].Contains("@"))
            // Delete bot tag from command
            command[0] = command[0].Split("@")[0];

        long chatId = update.Message.Chat.Id;
        long userId = update.Message.From.Id;
        int messageId = update.Message.MessageId;

        string lang = Database.GetLang(chatId);

        switch (command[0])
        {
            case "/help": goto case "/start";
            case "/start":
                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: Translations.HelpCommand[lang],
                    cancellationToken: cts,
                    replyToMessageId: messageId
                );
                break;

            case "/xp":
                int xp = Database.GetXp(userId, chatId);
                int level = (int)(xp/100);

                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"*{Translations.Level[lang]}*: {level}\n*{Translations.XP[lang]}*: {xp}",
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cts,
                    replyToMessageId: messageId
                );

                break;

            case "/lang":
                if (command.Length == 1) 
                {
                    await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: Translations.KeyboardLangCommand[lang],
                        replyToMessageId: messageId,
                        replyMarkup: Translations.LangChangeMarkup
                    );
                }
                else 
                {
                    if (command[1] == "en" || command[1] == "ru") 
                    {
                        ChatMember chatMember = await bot.GetChatMemberAsync(chatId, userId);

                        if (update.Message.Chat.Type == ChatType.Private) 
                        {
                            Database.SetLang(command[1], chatId);

                            await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: Translations.LanguageChanged[command[1]],
                                cancellationToken: cts,

                                replyToMessageId: messageId
                            );

                            break;
                        }

                        if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator) 
                        {
                            Database.SetLang(command[1], chatId);
                            await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: Translations.LanguageChanged[command[1]],
                                cancellationToken: cts,

                                replyToMessageId: messageId
                            );
                        }
                    }
                }

                break;

            case "/top":
                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: await Database.GetTop(bot, chatId, lang, cts),
                    parseMode: ParseMode.MarkdownV2,
                    replyToMessageId: messageId
                );
                break;

            default:
                Database.AddXp(userId, chatId, Methods.GetRandom());
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
                Database.SetLang(update.CallbackQuery.Data, update.CallbackQuery.Message.Chat.Id);

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
                Database.SetLang(update.CallbackQuery.Data, update.CallbackQuery.Message.Chat.Id);

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

}