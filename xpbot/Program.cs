using Serilog;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

/// <summary>
/// Main bot class
/// </summary>
public class Program
{
	/// <summary>
	/// Main function
	/// </summary>
	public static void Main(string[] args)
	{
		// Create handlers class
		Handlers handlers = new Handlers();

		// Create methods class
		Methods methods = new Methods();

		// Init logger
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.Console()
			.CreateLogger();

		Log.Information("Starting program...");

		string? token = methods.GetToken();

		if (token == null) 
		{
			Log.Fatal("TOKEN variable not found in environment.");
			throw new InvalidDataException("TOKEN variable not found in environment.");
		}

		// Init bot
		TelegramBotClient bot = new TelegramBotClient(token);

		ReceiverOptions receiverOptions = new ReceiverOptions
		{
			AllowedUpdates = { }
		};

		CancellationTokenSource cts = new CancellationTokenSource();

		bot.StartReceiving(
			handlers.HandleUpdatesAsync,
			handlers.HandleErrorsAsync,
			receiverOptions,
			cts.Token
		);

		Log.Information("Bot started!");

		// Infinite loop
		while (true) 
		{
			Thread.Sleep(100);
		}
	}
}