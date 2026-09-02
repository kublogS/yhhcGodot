using System;

public static class EnemyPlayerIntelTests
{
    // The Tests console app never runs the Database autoload (no live Godot
    // engine), so TypeSystem.GetConfig() would otherwise fall back to an
    // empty config with no types/weakness table. Load a minimal but faithful
    // slice of the real type table for the duration of each test, and
    // restore whatever was there before so this doesn't leak into other tests.
    private const string MinimalTypeSystemJson = """
    {
      "types": ["Marziale", "Fulmine", "Freddo", "Acqua", "Fuoco", "Sacro", "Profano"],
      "families": {
        "Bruta": ["Marziale", "Fuoco", "Fulmine"],
        "Ultraterreno": ["Freddo", "Sacro", "Profano"]
      },
      "weakness": {
        "Acqua": ["Fulmine"],
        "Freddo": ["Acqua"]
      },
      "friend_groups": [["Bruta"], ["Ultraterreno"]]
    }
    """;

    private static void WithMinimalTypeSystem(Action body)
    {
        var previous = TypeSystem.GetConfig();
        TypeSystem.SetConfig(TypeSystem.BuildConfig(MinimalTypeSystemJson));
        try
        {
            body();
        }
        finally
        {
            TypeSystem.SetConfig(previous);
        }
    }

    public static void ConvergesOnRepeatedWeaknessSignal(TestAssert assert)
    {
        WithMinimalTypeSystem(() =>
        {
            var intel = new EnemyPlayerIntel();
            assert.True(intel.GuessDefenderType() is null, "should have no guess before any observation");

            // Only "Acqua" is weak to a "Fulmine" attack in the type table, so
            // this uniquely (if gradually) identifies the defender's type.
            var tags = new DamageTags { AttackType = "Fulmine", Weak = true };
            intel.RecordObservation(tags);
            assert.True(intel.GuessDefenderType() is null, "a single observation should not be enough to commit yet");

            intel.RecordObservation(tags);
            assert.Equal("Acqua", intel.GuessDefenderType(), "repeated consistent weakness signals should converge on the implicated type");
        });
    }

    public static void NullifyIsAnInstantReveal(TestAssert assert)
    {
        var intel = new EnemyPlayerIntel();
        intel.RecordObservation(new DamageTags { AttackType = "Sacro", Nullified = true });
        assert.Equal("Sacro", intel.GuessDefenderType(), "a nullified hit directly reveals the defender's type");
    }

    public static void NeutralHitsDoNotFalselyConverge(TestAssert assert)
    {
        WithMinimalTypeSystem(() =>
        {
            var intel = new EnemyPlayerIntel();
            var tags = new DamageTags { AttackType = "Marziale" };
            intel.RecordObservation(tags);
            intel.RecordObservation(tags);
            intel.RecordObservation(tags);
            assert.True(intel.GuessDefenderType() is null, "plain neutral hits should never converge on a guess");
        });
    }
}
