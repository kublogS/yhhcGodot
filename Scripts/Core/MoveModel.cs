using System;

public sealed class MoveModel
{
    public string Name { get; set; } = "Attacco";
    public int MoveId { get; set; }
    public string? Weapon { get; set; }
    public string? Element { get; set; }
    public string? MoveType { get; set; }
    public int BaseDamage { get; set; } = 10;
    public bool Aoe { get; set; }
    public int Power { get; set; } = 5;
    public string? CostResource { get; set; }
    public string? CostRaw { get; set; }
    public int? CostAmount { get; set; }
    public string? CostUnit { get; set; }

    public bool IsBasicAttack => MoveId == 0 || Name.Equals("Attacco", StringComparison.OrdinalIgnoreCase);
}
