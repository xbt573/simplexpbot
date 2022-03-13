using Telegram.Bot.Types.ReplyMarkups;

/// <summary>
/// Class contains translations of text given to user
/// </summary>
public class Translations
{
	/// <summary>
	/// Translations for /help command
	/// </summary>
	public Dictionary<string, string> HelpCommand { get; set; }

	/// <summary>
	/// Translations for /lang command (keyboard ver.)
	/// </summary>
	public Dictionary<string, string> KeyboardLangCommand { get; set; }

	/// <summary>
	/// Translations for /lang command (keyboard markup)
	/// </summary>
	public InlineKeyboardMarkup LangChangeMarkup { get; set; }

	/// <summary>
	/// Translations for /lang command (no access)
	/// </summary>
	public Dictionary<string, string> LangChangeNoAccess { get; set; }

	/// <summary>
	/// Translations for /lang command (inline ver.)
	/// </summary>
	public Dictionary<string, string> LanguageChanged { get; set; }

	/// <summary>
	/// Translations for /xp and /top commands
	/// </summary>
	public Dictionary<string, string> Level { get; set; }

	/// <summary>
	/// Translations for /top command (no xp)
	/// </summary>
	public Dictionary<string, string> NoXp { get; set; }

	/// <summary>
	/// Translations for /xp and /top commands
	/// </summary>
	public Dictionary<string, string> XP { get; set; }

	public Translations()
	{
		HelpCommand = new Dictionary<string, string>
		{
			{"ru", "Привет!\nДанный бот помогает вам собирать \"опыт\", что бы узнать свой опыт и уровень напишите /xp, что бы сменить язык напишите /lang"},
			{"en", "Hi!\nThis bot helps you to collect \"experience\", that would know your experience and level write /xp, that would change the language write /lang"}
		};

		KeyboardLangCommand = new Dictionary<string, string>
		{
			{"ru", "Выберите язык"},
			{"en", "Choose language"}
		};

		LangChangeNoAccess = new Dictionary<string, string>
		{
			{"ru", "Менять язык могут только администраторы!"},
			{"en", "Only chat administrators can change language!"}
		};

		LanguageChanged = new Dictionary<string, string>
		{
			{"ru", "Язык изменён!"},
			{"en", "Language changed!"}
		};

		Level = new Dictionary<string, string>
		{
			{"ru", "Уровень"},
			{"en", "Level"}
		};

		NoXp = new Dictionary<string, string>
		{
			{"ru", "Нет участников с опытом в базе :/"},
			{"en", "No users with experience in database :/"}
		};

		XP = new Dictionary<string, string>
		{
			{"ru", "Опыт"},
			{"en", "XP"}
		};

		LangChangeMarkup = new(new []
		{
			new []
			{
				InlineKeyboardButton.WithCallbackData(text: "Russian 🇷🇺", callbackData: "ru")
			},

			new []
			{
				InlineKeyboardButton.WithCallbackData(text: "English 🇬🇧", callbackData: "en")
			}
		});
	}
}