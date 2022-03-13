using Serilog;

using System.Data.SQLite;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace XPBot;

public class Program
{
	public static string botname = "simplexpbot";

	public static string cs = "Data Source=database.db";

	public static Dictionary<string, string> helpmessages = new Dictionary<string, string> 
	{
		// TODO: Сделай использование, псина
		{"ru", "Привет!\nДанный бот помогает вам собирать \"опыт\", что бы узнать свой опыт и уровень напишите /xp, что бы сменить язык напишите /lang"},
		{"en", "Hi!\nThis bot helps you to collect \"experience\", that would know your experience and level write /xp, that would change the language write /lang"}
	};

	public static Dictionary<string, string> chooselangmessages = new Dictionary<string, string>
	{
		{"ru", "Выберите язык:"},
		{"en", "Choose language"}
	};

	public static Dictionary<string, string> languagechanged = new Dictionary<string, string>
	{
		{"ru", "Язык изменён!"},
		{"en", "Language changed!"}
	};

	public static Dictionary<string, string> languagenoaccess = new Dictionary<string, string> 
	{
		{"ru", "Изменять язык может только администратор!"},
		{"en", "Only administrators can change language!"}
	};

	public static Dictionary<string, string[]> getxpmessages = new Dictionary<string, string[]>
	{
		{"ru", new string[] {"*Опыт*: ", "*Уровень*: "}},
		{"en", new string[] {"*XP*: ", "*Level*: "}}
	};

	public static Dictionary<string, string> noxp = new Dictionary<string, string>
	{
		{"ru", "Нет пользователей с опытом в базе :/"},
		{"en", "No users with experience in database :/"}
	};

	// public static ReplyKeyboardMarkup languagekeyboard = new(new[]
	// {
	// 	new KeyboardButton[] {"Русский 🇷🇺"},
	// 	new KeyboardButton[] {"English 🇬🇧"},
	// });

	public static InlineKeyboardMarkup languagekeyboard = new(new []
    {
        // first row
        new []
        {
            InlineKeyboardButton.WithCallbackData(text: "Russian 🇷🇺", callbackData: "ru")
        },
        // second row
        new []
        {
            InlineKeyboardButton.WithCallbackData(text: "English 🇬🇧", callbackData: "en")
        },
    });

	public static Random rnd = new Random();

	public static void Main(string[] args)
	{
		InitLogger();
		
		Log.Information("Starting program");
		Log.Information("Connecting to a database...");

		CreateDbIfNotExists("database.db");

		var conn = InitConnection(cs);
		CreateTablesIfNotExist(conn);		

		Log.Information("Initializing bot...");

		string token = GetToken();
		InitBot(token);		

	}


    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
    	if (await ProcessCallback(bot, update, cts) == true) 
    	{
    		return;
    	}

        if (update.Type != UpdateType.Message)
        	return;

    	if (update.Message!.Type != MessageType.Text)
	        return;

	   	if (update.Message == null) 
	   		return;

	   	if (update.Message.Text == null) 
	   		return;

	   	if (update.Message.Chat == null)
	   		return;

	    string[] command = update.Message.Text.Split(" ");
	    string? lang = GetLang(update.Message.Chat.Id);

		await ProcessCommand(bot, update, cts, command, lang);
    }

    public static async Task ProcessCommand(ITelegramBotClient bot, Update update, CancellationToken cts, string[] command, string? lang)
    {
    	if (update.Message == null) 
	   		return;

	   	if (update.Message.Text == null) 
	   		return;

	   	if (update.Message.From == null)
	   		return;

    	long chatId = update.Message.Chat.Id;
	    long userId = update.Message.From.Id;
	    string messageText = update.Message.Text;
	    int messageId = update.Message.MessageId;

	    if (lang == null)
	    	lang = "en";

    	switch (command[0].Split("@")[0]) 
	    {
            // case "/start@simplexpbot": goto case "/start";
            case "/start":
	    		await bot.SendTextMessageAsync(
	    			chatId: chatId,
	    			text: helpmessages[lang],
	    			cancellationToken: cts,

	    			replyToMessageId: messageId
	    		);

	    		break;

            //case "/help@simplexpbot": goto case "/help";
	    	case "/help":
	    		await bot.SendTextMessageAsync(
	    			chatId: chatId,
	    			text: helpmessages[lang],
	    			cancellationToken: cts,

	    			replyToMessageId: messageId
	    		);

	    		break;

            // case "/lang@simplexpbot": goto case "/lang";
	    	case "/lang":
	    		bool en = command.Contains("en");
	    		bool ru = command.Contains("ru");

	    		if (en || ru) 
	    		{
	    			lang = command[1];

	    			SetLang(chatId, lang);

	    			await bot.SendTextMessageAsync(
	    				chatId: chatId,
	    				text: languagechanged[lang],
	    				cancellationToken: cts,

	    				replyToMessageId: messageId
	    			);
	    		}
	    		else
	    		{
	    			await bot.SendTextMessageAsync(
	    				chatId: chatId,
	    				text: chooselangmessages[lang],
	    				cancellationToken: cts,

	    				replyToMessageId: messageId,

	    				replyMarkup: languagekeyboard
	    			);
	    		}

	    		break;
            
            // case "/xp@simplexpbot": goto case "/xp";
	    	case "/xp":
	    		await bot.SendTextMessageAsync(
	    			chatId: chatId,
	    			parseMode: ParseMode.MarkdownV2,
	    			text: $"{getxpmessages[lang][0]}{GetXp(userId, chatId)}\n{getxpmessages[lang][1]}{GetLevel(userId, chatId)}",
	    			cancellationToken: cts,

	    			replyToMessageId: messageId
	    		);

	    		break;

	    	case "/top":
	    		string? locallang = GetLang(chatId);
	    		string top = await GetTop(bot, chatId, lang, cts);

	    		await bot.SendTextMessageAsync(
	    			chatId: chatId,
	    			text: top,
	    			cancellationToken: cts,

	    			parseMode: ParseMode.MarkdownV2,
	    			replyToMessageId: messageId
	    		);

	    		break;

	    	default:
	    		AddXp(chatId, userId, update.Message.Chat);
	    		break;
	    }
    }

    public static async Task<bool> ProcessCallback(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
    	long chatId;

    	if (update.CallbackQuery != null) 
    	{
    		if (update.CallbackQuery.Message == null)
    			return false;

    		if (update.CallbackQuery.Data == null)
    			return false;

    		chatId = update.CallbackQuery.Message.Chat.Id;

    		if (update.CallbackQuery.Message.Chat.Type == ChatType.Private) 
    		{
    			SetLang(chatId, update.CallbackQuery.Data);
    			await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, languagechanged[update.CallbackQuery.Data]);	
    		}
    		else 
    		{
    			ChatMember user = await bot.GetChatMemberAsync(chatId, update.CallbackQuery.From.Id, cts);

    			if (user.Status == ChatMemberStatus.Creator || user.Status == ChatMemberStatus.Administrator) 
    			{
    				SetLang(chatId, update.CallbackQuery.Data);
    				await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, languagechanged[update.CallbackQuery.Data]);
    			}
    			else 
    			{
    				string? lang = GetLang(chatId);
    				await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, languagenoaccess[update.CallbackQuery.Data]);
    			}
			}

    		return true;
    	}

    	return false;
    }


    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cts)
    {
        var ErrorMessage = exception switch
    	{
	        ApiRequestException apiRequestException
            	=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        	_ => exception.ToString()
    	};

    	Log.Warning(ErrorMessage);
    	return Task.CompletedTask;
    }

    public static void InitLogger()
    {
    	Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.Console()
			.CreateLogger();	
    }

    public static void CreateDbIfNotExists(string db)
    {
    	if (!System.IO.File.Exists(db)) 
			System.IO.File.Create(db);
    }

    public static SQLiteConnection InitConnection(string cs)
    {
    	var conn = new SQLiteConnection(cs);
    	conn.Open();

    	return conn;
    }

    public static SQLiteCommand InitCommand(SQLiteConnection conn)
    {
    	var cmd = new SQLiteCommand(conn);
    	return cmd;
    }

    public static void CreateTablesIfNotExist(SQLiteConnection conn)
    {
    	using var cmd = InitCommand(conn);

		cmd.CommandText = 
		@"CREATE TABLE IF NOT EXISTS groups (
			id BIGINT,
			lang DEFAULT 'en'
		);

		CREATE TABLE IF NOT EXISTS users (
			chatid BIGINT,
			id BIGINT,
			level INT,
			xp INT
		);";

		cmd.ExecuteNonQuery();
    }

    public static string GetToken()
    {
    	string? token = Environment.GetEnvironmentVariable("TOKEN");

		if (token == null) 
		{
			Log.Fatal("TOKEN variable not found in environment");
			throw new InvalidDataException("TOKEN variable not found in environment");
		}

		return token;
    }

    public static void InitBot(string token)
    {
    	var bot = new TelegramBotClient(token);

		using var cts = new CancellationTokenSource();

		var receiverOptions = new ReceiverOptions
		{
			AllowedUpdates = { }
		};

		bot.StartReceiving(
			HandleUpdateAsync,
			HandleErrorAsync,
			receiverOptions,
			cancellationToken: cts.Token
		);

		Log.Information("Bot started!");

		while (true) 
		{
			Thread.Sleep(100);
		}

		// Log.Information("Goodbye!");
		// cts.Cancel();
    }

    public static string? GetLang(long chatId)
    {
    	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"SELECT lang FROM groups WHERE id=@id";
	    cmd.Parameters.AddWithValue("@id", chatId);

	    object output = cmd.ExecuteScalar();
	    string? lang;

	    if (output == null) 
	    {
	    	lang = "en";

	    	cmd.CommandText = 
	    	@"INSERT INTO groups VALUES (
	    		@chatId,
	    		@lang
	    	)";

	    	cmd.Parameters.AddWithValue("@chatId", chatId);
	    	cmd.Parameters.AddWithValue("@lang", "en");

	   		cmd.ExecuteNonQuery();
	   	}
	   	else 
	   	{
	   		lang = output.ToString();	
   		}

   		return lang;
    }

    public static void SetLang(long chatId, string lang)
    {
    	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"UPDATE groups SET lang=@lang WHERE id=@id";
	    cmd.Parameters.AddWithValue("@lang", lang);
	    cmd.Parameters.AddWithValue("@id", chatId);

	    cmd.ExecuteNonQuery();
    }

    public static void InsertGroupIfNotExists(long chatId)
    {
    	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"SELECT * FROM groups WHERE id=@id";
	    cmd.Parameters.AddWithValue("@id", chatId);

	    object output = cmd.ExecuteScalar();

	    if (output == null) 
	    {
	    	cmd.CommandText = 
	    	@"INSERT INTO groups VALUES (
	    		@chatId,
	    		@lang
	    	)";

	    	cmd.Parameters.AddWithValue("@chatId", chatId);
	    	cmd.Parameters.AddWithValue("@lang", "en");

	   		cmd.ExecuteNonQuery();
	   	}
    }

    public static void InsertUserIfNotExists(long userId, long chatId)
    {
    	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"SELECT * FROM users WHERE id=@id";
	    cmd.Parameters.AddWithValue("@id", userId);

	    object output = cmd.ExecuteScalar();

	    if (output == null) 
	    {
	    	cmd.CommandText = 
	    	@"INSERT INTO users VALUES (
	    		@chatId,
	    		@userId,
	    		@level,
	    		@xp
	    	)";

	    	cmd.Parameters.AddWithValue("@chatId", chatId);
	    	cmd.Parameters.AddWithValue("@userId", userId);
	    	cmd.Parameters.AddWithValue("@level", 0);
	    	cmd.Parameters.AddWithValue("@xp", 0.0);

	   		cmd.ExecuteNonQuery();
	   	}
    }
    
    public static void AddXp(long chatId, long userId, Chat chat)
    {
    	if (chat.Type == ChatType.Private) 
    		return;

     	InsertGroupIfNotExists(chatId);
     	InsertUserIfNotExists(userId, chatId);

     	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

     	int? oldxp = GetXp(userId, chatId);

     	if (oldxp == null)
     		oldxp = 0;

     	int? newxp = oldxp + rnd.Next(1, 5);
     	int newlevel = (int)(newxp / 100);

     	cmd.CommandText = @"UPDATE users SET level=@level, xp=@xp WHERE id=@id";
     	cmd.Parameters.AddWithValue("@level", newlevel);
     	cmd.Parameters.AddWithValue("@xp", newxp);
     	cmd.Parameters.AddWithValue("@id", userId);

     	cmd.ExecuteNonQuery();
    }

    public static int? GetXp(long userId, long chatId)
    {
    	InsertUserIfNotExists(userId, chatId);

    	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"SELECT xp FROM users WHERE id=@id";
    	cmd.Parameters.AddWithValue("@id", userId);

    	var dbOut = cmd.ExecuteScalar().ToString();

    	if (dbOut == null)
    		return null;

    	int? xp = int.Parse(dbOut);

    	return xp;
    }

    public static int? GetLevel(long userId, long chatId)
    {
    	InsertUserIfNotExists(userId, chatId);

		using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"SELECT level FROM users WHERE id=@id";
    	cmd.Parameters.AddWithValue("@id", userId);

    	var dbOut = cmd.ExecuteScalar().ToString();

    	if (dbOut == null)
    		return null;

    	int? level = int.Parse(dbOut);

    	return level;    	
    }

    public static async Task<string> GetTop(ITelegramBotClient bot, long chatId, string lang, CancellationToken cts)
    {
    	using var conn = InitConnection(cs);
    	using var cmd = InitCommand(conn);

    	cmd.CommandText = @"SELECT * FROM users WHERE chatid=@chatId ORDER BY xp DESC LIMIT 5";
    	cmd.Parameters.AddWithValue("@chatId", chatId);

    	using var rdr = cmd.ExecuteReader();
    	string top = "";

    	if (lang == null)
    		lang = "en";

    	while (rdr.Read()) 
    	{
    		ChatMember chatMember = await bot.GetChatMemberAsync(chatId, rdr.GetInt64(1), cts);

    		top += 
    		@$"*{chatMember.User.FirstName} {chatMember.User.LastName}*:
    			{getxpmessages[lang][1]}{rdr.GetInt32(2)}
    			{getxpmessages[lang][0]}{rdr.GetInt32(3)}

";
    	}

    	if (top == "")
    		return noxp[lang];

    	return top;
    }
}
