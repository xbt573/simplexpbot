using System.Data.SQLite;
using Telegram.Bot;
using Telegram.Bot.Types;

/// <summary>
/// Class for working with database
/// </summary>
public class Database
{
    /// <summary>
    /// Access string for database
    /// </summary>
    public string AccessString { get; set; }

    /// <summary>
    /// Localization for GetTop method
    /// </summary>
    public Translations Translations { get; set; }

    public Database(string accessString)
    {
        AccessString = accessString;
        Translations = new Translations();

        using var conn = GetSQLiteConnection(AccessString);
        using var cmd = GetSQLiteCommand(conn);

        cmd.CommandText =
        @"CREATE TABLE IF NOT EXISTS groups (
            id BIGINT,
            lang TEXT
        );

        CREATE TABLE IF NOT EXISTS users (
            id BIGINT,
            chatid BIGINT,
            level INT,
            xp INT
        );";

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Get user xp from database
    /// </summary>
    public int GetXp(long userId, long chatId)
    {
        using var conn = GetSQLiteConnection(AccessString);
        using var cmd = GetSQLiteCommand(conn);

        cmd.CommandText = @"SELECT xp FROM users WHERE id=@userId AND chatid=@chatId";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@chatId", chatId);

        var output = cmd.ExecuteScalar();

        if (output == null) 
        {
            return 0;
        }
        else 
        {
            string? parsed = output.ToString();

            if (parsed == null) 
            {
                return 0;
            }
            else 
            {
                return int.Parse(parsed);   
            }

            // return int.Parse(output.ToString());
        }
    }

    /// <summary>
    /// Set user xp to database
    /// </summary>
    public void SetXp(long userId, long chatId, int xp)
    {
        using var conn = GetSQLiteConnection(AccessString);
        using var cmd = GetSQLiteCommand(conn);

        cmd.CommandText = @"SELECT xp FROM users WHERE id=@userId AND chatid=@chatId";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@chatId", chatId);

        var output = cmd.ExecuteScalar();

        if (output == null) 
        {
            cmd.CommandText = @"INSERT INTO users VALUES(@userId, @chatId, 0, 0)";
        }
        else 
        {
            cmd.CommandText = @"UPDATE users SET xp=@xp WHERE id=@userId AND chatid=@chatId";   
        }

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@chatId", chatId);
        cmd.Parameters.AddWithValue("@xp", xp);

        cmd.ExecuteNonQuery();
    }

    public void AddXp(long userId, long chatId, int addedXp)
    {
        int oldXp = GetXp(userId, chatId);
        int newXp = oldXp+addedXp;

        SetXp(userId, chatId, newXp);
    }

    /// <summary>
    /// Get chat language from database
    /// </summary>
    public string GetLang(long chatId)
    {
        using var conn = GetSQLiteConnection(AccessString);
        using var cmd = GetSQLiteCommand(conn);

        cmd.CommandText = @"SELECT lang FROM groups WHERE id=@chatId";
        cmd.Parameters.AddWithValue("@chatId", chatId);

        var output = cmd.ExecuteScalar();

        if (output == null) 
        {
            SetLang("en", chatId);
            return "en";
        }
        else 
        {
            string? parsed = output.ToString();

            if (parsed == null) 
            {
                SetLang("en", chatId);
                return "en";
            }

            return parsed;
        
        }
    }

    /// <summary>
    /// Set chat language to database
    /// </summary>
    public void SetLang(string lang, long chatId)
    {
        using var conn = GetSQLiteConnection(AccessString);
        using var cmd = GetSQLiteCommand(conn);

        cmd.CommandText = @"SELECT lang FROM groups WHERE id=@chatId";
        cmd.Parameters.AddWithValue("@chatId", chatId);

        var output = cmd.ExecuteScalar();

        if (output == null) 
        {
            cmd.CommandText = @"INSERT INTO groups VALUES(@chatId, @lang)";
        }
        else
        {
            cmd.CommandText = @"UPDATE groups SET lang=@lang WHERE id=@chatId";
        }

        cmd.Parameters.AddWithValue("@chatId", chatId);
        cmd.Parameters.AddWithValue("@lang", lang);

        cmd.ExecuteNonQuery();
    }

    public async Task<string> GetTop(ITelegramBotClient bot, long chatId, string lang, CancellationToken cts)
    {
        using var conn = GetSQLiteConnection(AccessString);
        using var cmd = GetSQLiteCommand(conn);

        cmd.CommandText = @"SELECT * FROM users WHERE chatid=@chatId ORDER BY xp DESC LIMIT 5";
        cmd.Parameters.AddWithValue("@chatId", chatId);

        using var rdr = cmd.ExecuteReader();
        string top = "";

        if (lang == null)
            lang = "en";

        while (rdr.Read()) 
        {
            ChatMember chatMember = await bot.GetChatMemberAsync(chatId, rdr.GetInt64(0), cts);

            top += 
            @$"*{chatMember.User.FirstName} {chatMember.User.LastName}*:
                *{Translations.Level[lang]}*: {rdr.GetInt32(2)}
                *{Translations.XP[lang]}*: {rdr.GetInt32(3)}
";
        }

        if (top == "")
            return Translations.NoXp[lang];

        return top;
    }


    /// <summary>
    /// Get SQLite connection
    /// </summary>
    public SQLiteConnection GetSQLiteConnection(string cs)
    {
        SQLiteConnection conn = new SQLiteConnection(cs);
        conn.Open();

        return conn;
    }

    /// <summary>
    /// Get SQLite command
    /// </summary>
    public SQLiteCommand GetSQLiteCommand(SQLiteConnection conn)
    {   
        SQLiteCommand cmd = new SQLiteCommand(conn);
        return cmd;
    }
    
}