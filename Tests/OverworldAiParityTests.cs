using System.Collections.Generic;

public static class OverworldAiParityTests
{
    public static void SuspiciousAlertPersistence(TestAssert assert)
    {
        var enemy = new OverworldEnemyModel
        {
            EnemyId = "susp_1",
            BehaviorKind = OverworldBehavior.Sospetto,
            Intelligence = 2,
            Speed = 1.5f,
            X = 1.5f,
            Y = 1.5f,
            Alerted = false,
        };

        var openGrid = new[,]
        {
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 },
        };
        var first = OverworldAI.ComputeVelocity(enemy, playerX: 2.5f, playerY: 1.5f, openGrid, alertTimer: 0f);
        assert.True(first.Chasing, "sospetto should chase when player is visible");
        assert.True(enemy.Alerted, "sospetto should persist alert flag after seeing player");

        var blockedGrid = new[,]
        {
            { 0, 1, 0 },
            { 0, 1, 0 },
            { 0, 1, 0 },
        };
        var second = OverworldAI.ComputeVelocity(enemy, playerX: 2.5f, playerY: 1.5f, blockedGrid, alertTimer: 0f);
        assert.True(second.Chasing, "sospetto should keep chasing after LOS loss once alerted");
    }

    public static void WakeStuckAlly(TestAssert assert)
    {
        var smart = new OverworldEnemyModel
        {
            EnemyId = "smart",
            Active = true,
            Intelligence = 7,
            X = 1f,
            Y = 1f,
        };
        var stuck = new OverworldEnemyModel
        {
            EnemyId = "stuck",
            Active = true,
            Intelligence = 2,
            IsStuck = true,
            StuckTimer = OverworldAI.StuckTimeSec,
            X = 2f,
            Y = 1f,
        };
        var enemies = new List<OverworldEnemyModel> { smart, stuck };
        var rng = new GameRng(12345);

        OverworldAI.MaybeWakeStuckAlly(smart, enemies, rng);

        assert.True(!stuck.IsStuck, "stuck ally should be released by smart enemy");
        assert.True(stuck.WakeTimer > 0f, "woken ally should receive wake timer");
        assert.True(!string.IsNullOrWhiteSpace(stuck.SpeechText), "woken ally should emit speech");
        assert.True(smart.WakeTimer > 0f, "smart enemy should receive wake cooldown");
    }

    public static void BreakableBlocksLineOfSight(TestAssert assert)
    {
        var blockedGrid = new[,]
        {
            { 0, 0, 0 },
            { 0, (int)TileType.Breakable, 0 },
            { 0, 0, 0 },
        };

        var visible = OverworldAI.HasLineOfSight(blockedGrid, startX: 0f, startY: 2f, endX: 4f, endY: 2f);
        assert.True(!visible, "intact breakable should block line of sight");
    }

    public static void ContactBehaviorGate(TestAssert assert)
    {
        var baseEnemy = new OverworldEnemyModel { EnemyId = "gate", Active = true };

        baseEnemy.BehaviorKind = OverworldBehavior.Pacifista;
        assert.True(!OverworldAI.ShouldStartEncounterOnContact(baseEnemy, chasing: true, alertTimer: 5f), "pacifista must never auto-start encounter");

        baseEnemy.BehaviorKind = OverworldBehavior.Tranquillo;
        assert.True(!OverworldAI.ShouldStartEncounterOnContact(baseEnemy, chasing: false, alertTimer: 0f), "tranquillo should not engage without alert/chase");
        assert.True(OverworldAI.ShouldStartEncounterOnContact(baseEnemy, chasing: false, alertTimer: 3f), "tranquillo should engage with valid alert");

        baseEnemy.BehaviorKind = OverworldBehavior.Sospetto;
        baseEnemy.Alerted = false;
        assert.True(!OverworldAI.ShouldStartEncounterOnContact(baseEnemy, chasing: false, alertTimer: 0f), "sospetto should not engage if not alerted/chasing");
        baseEnemy.Alerted = true;
        assert.True(OverworldAI.ShouldStartEncounterOnContact(baseEnemy, chasing: false, alertTimer: 0f), "sospetto should engage if alerted");

        baseEnemy.BehaviorKind = OverworldBehavior.Ansioso;
        assert.True(OverworldAI.ShouldStartEncounterOnContact(baseEnemy, chasing: false, alertTimer: 0f), "ansioso should engage on contact");
    }

    public static void TranquilloVisibilityArmsLocalAlert(TestAssert assert)
    {
        var enemy = new OverworldEnemyModel
        {
            EnemyId = "calm_1",
            BehaviorKind = OverworldBehavior.Tranquillo,
            Intelligence = 4,
            Speed = 1.2f,
            X = 1.5f,
            Y = 1.5f,
        };

        var openGrid = new[,]
        {
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 },
        };
        var visible = OverworldAI.ComputeVelocity(enemy, playerX: 2.5f, playerY: 1.5f, openGrid, alertTimer: 0f);
        assert.True(visible.Chasing, "tranquillo should chase while player is visible");
        assert.True(enemy.BehaviorAlertTimer > 0f, "tranquillo should arm a local alert timer after seeing player");
        assert.True(OverworldAI.ShouldStartEncounterOnContact(enemy, chasing: false, alertTimer: 0f), "tranquillo should permit contact during local alert");

        OverworldAI.TickEnemyRuntime(enemy, OverworldAI.BehaviorAlertDurationSec + 0.1f);
        assert.True(!OverworldAI.ShouldStartEncounterOnContact(enemy, chasing: false, alertTimer: 0f), "tranquillo local alert should expire");
    }
}
