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
    public Random rnd { get; set; }

    public Methods()
    {
        // Database = new Database(System.IO.File.ReadAllText("access.txt"));
        rnd = new Random();
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
        return rnd.Next(1, 5);
    }

    // public void AddXp(Update update) {}
}