
public static class PlayerSession
{
    public static string PlayerName { get; private set; } = "";
    public static string PlayerId { get; private set; } = "";

    public static void SetPlayer(string name, string id)
    {
        PlayerName = name ?? "";
        PlayerId = id ?? "";
    }

    public static void Clear()
    {
        PlayerName = "";
        PlayerId = "";
    }
}
