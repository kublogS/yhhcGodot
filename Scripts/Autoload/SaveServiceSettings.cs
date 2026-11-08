using System.Text.Json;

public static class SaveServiceSettings
{
    public const int SaveVersion = 1;
    public static readonly int[] SlotIds = { 1, 2, 3 };
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
