using System.Collections.Generic;

public sealed class TypeSystemConfig
{
    public List<string> Types { get; set; } = new();
    public Dictionary<string, List<string>> Families { get; set; } = new();
    public Dictionary<string, List<string>> Weakness { get; set; } = new();
    public List<List<string>> FriendGroups { get; set; } = new();
    public Dictionary<string, string> DamageModifiers { get; set; } = new();
    public Dictionary<string, string> TypeStatScaling { get; set; } = new();
    public Dictionary<string, float> Scaling { get; set; } = new();
    public Dictionary<string, string> TypeLookup { get; set; } = new();
    public Dictionary<string, string> TypeToFamily { get; set; } = new();
    public Dictionary<string, int> FamilyGroup { get; set; } = new();
}
