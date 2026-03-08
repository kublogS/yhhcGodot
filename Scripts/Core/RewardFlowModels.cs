public sealed class RewardSummary
{
    public int Exp { get; init; }
    public int Soli { get; init; }
    public int Items { get; init; }
    public int Tokens { get; init; }
}

public sealed class RewardActionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
