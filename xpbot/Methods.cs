using Telegram.Bot;
using Telegram.Bot.Types;

/// <summary>
/// Methods for doing some work
/// </summary>
public class Methods
{
    // /// <summary>
    // /// Database class for working with database
    // /// </summary>
    // public Database Database { get; set; }

    /// <summary>
    /// Random class for getting random values
    /// </summary>
    public Random Rnd { get; set; }

    /// <summary>
    /// Translations class for gettins message translations
    /// </summary>
    public Translations Translations { get; set; }

    public Methods()
    {
        // Database = new Database(System.IO.File.ReadAllText("access.txt"));
        Rnd = new Random();
        Translations = new Translations();
    }

    /// <summary>
    /// Method for getting a token from environment
    /// </summary>
    public string? GetToken()
    {
        string? token = Environment.GetEnvironmentVariable("TOKEN");

        if (token == null)
        {
            return null;
        }

        return token;
    }

    /// <summary>
    /// Get random value between 1 and 5
    /// </summary>
    public int GetRandom()
    {
        return Rnd.Next(1, 5);
    }

    /// <summary>
    /// Function for getting chat language
    /// </summary>
    public string GetLang(long chatId)
    {
        using var db = new BotDbContext();
        db.Database.EnsureCreated();

        var group = db.Groups
            .Where(x => x.id == chatId)
            .FirstOrDefault();

        // TODO: Set EN as default value
        if (group == null)
        {
            SetLang(chatId, "en");
            return "en";
        }

        return group.lang;
    }

    /// <summary>
    /// Function for setting chat language
    /// </summary>
    public void SetLang(long chatId, string lang)
    {
        using var db = new BotDbContext();
        db.Database.EnsureCreated();

        var group = db.Groups
            .Where(x => x.id == chatId)
            .FirstOrDefault();

        if (group == null)
        {
            db.Groups.Add(
                new Group { id = chatId, lang = lang }
            );

            db.SaveChanges();

            return;
        }

        group.lang = lang;
        db.Groups.Update(group);

        db.SaveChanges();
    }

    /// <summary>
    /// Function for getting sum of user xp in all groups
    /// </summary>
    public int GetSumXp(long userId)
    {
        using var db = new BotDbContext();
        db.Database.EnsureCreated();

        var xp = db.Users
            .Where(x => x.id == userId)
            .Sum(x => x.xp);

        return xp;
    }

    /// <summary>
    /// Function for getting user xp in specified group
    /// </summary>
    public int GetXp(long userId, long chatId)
    {
        using var db = new BotDbContext();
        db.Database.EnsureCreated();

        var user = db.Users
            .Where(x => x.id == userId)
            .Where(x => x.chatId == chatId)
            .FirstOrDefault();


        if (user == null) 
        {
            SetXp(userId, chatId, 0);
            return 0;
        }

        return user.xp;
    }

    /// <summary>
    /// Function for setting user xp
    /// </summary>
    public void SetXp(long userId, long chatId, int xp)
    {
        using var db = new BotDbContext();
        db.Database.EnsureCreated();

        var user = db.Users
            .Where(x => x.id == userId)
            .Where(x => x.chatId == chatId)
            .FirstOrDefault();

        if (user == null)
        {
            int sumXp = db.Users.Where(x => x.id == userId).Sum(x => x.xp);

            db.Users.Add(
                new User { id = userId, chatId = chatId, xp = xp, level = (int)(xp / 100) }
            );

            db.SaveChanges();

            return;
        }

        user.xp = xp;
        user.level = (int)(xp / 100);

        db.Users.Update(user);

        db.SaveChanges();
    }

    /// <summary>
    /// Function for increment user xp
    /// </summary>
    public void AddXp(long userId, long chatId, int addedxp)
    {
        int oldxp = GetXp(userId, chatId);

        int newxp = oldxp + addedxp;
        SetXp(userId, chatId, newxp);
    }

    /// <summary>
    /// Function for getting users top by xp in group
    /// </summary>
    public async Task<string> GetTop(ITelegramBotClient bot, long chatId, string lang, CancellationToken cts)
    {
        using var db = new BotDbContext();
        await db.Database.EnsureCreatedAsync();

        var users = db.Users
            .Where(x => x.chatId == chatId)
            .Select(x => new {
                Id = x.id
            })
            .ToList();

        List<UserXp> userXp = new List<UserXp>();
        foreach (var user in users) 
        {
            userXp.Add(db.Users
                .Where(x => x.id == user.Id)
                .Select(x => new UserXp {
                    Id = x.id,
                    ChatId = x.chatId,
                    SumXp = db.Users
                        .Where(x => x.id == user.Id)
                        .Sum(x => x.xp)
                })
                .FirstOrDefault());
        }

        userXp = userXp.OrderByDescending(x => x.SumXp).ToList();

        int i = 0;

        ChatMember chatMember;

        string top = "";
        foreach (var user in userXp)
        {
            chatMember = await bot.GetChatMemberAsync(chatId, user.Id, cts);

            top += 
            @$"*{chatMember.User.FirstName} {chatMember.User.LastName}*:
                *{Translations.Level[lang]}*: {(int)(user.SumXp / 100)}
                *{Translations.XP[lang]}*: {user.SumXp}
";
            if (i > 5)
                break;

            i++;
        }

        if (top == "")
            return Translations.NoXp[lang];

        return top;
    }
}

public class UserXp
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public int SumXp { get; set; }
}