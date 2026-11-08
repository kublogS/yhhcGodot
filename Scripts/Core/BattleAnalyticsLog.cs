using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;
using FileAccess = Godot.FileAccess;

// Writes one JSON file per battle plus an appended CSV summary row under
// user://battle_logs, so battle balance can be inspected/aggregated after
// the fact instead of only observed live.
public static class BattleAnalyticsLog
{
    private const string LogDir = "user://battle_logs";
    private const string SummaryPath = "user://battle_logs/battles_summary.csv";
    private const int MaxDetailedRecords = 200;
    private const string SummaryHeader =
        "startedAtUnix,context,enemyCount,enemyNames,outcome,totalTurns,playerMaxHp,playerHpStart,playerHpEnd,damageDealt,damageTaken";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static BattleLogRecord? _current;
    private static int _turnCounter;
    private static int _totalDamageDealt;
    private static int _totalDamageTaken;

    public static void BeginBattle(GameState state, string context)
    {
        DirAccess.MakeDirRecursiveAbsolute(LogDir);
        _turnCounter = 0;
        _totalDamageDealt = 0;
        _totalDamageTaken = 0;
        _current = new BattleLogRecord
        {
            BattleId = Guid.NewGuid().ToString("N")[..8],
            StartedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Context = context,
            PlayerName = state.Player.Name,
            PlayerLevel = state.Player.Level,
            PlayerMaxHpStart = state.Player.MaxHp,
            PlayerHpStart = state.Player.Hp,
            PlayerStats = ToStats(state.Player),
            Enemies = state.Enemies.Select(enemy => new BattleLogParticipant
            {
                Name = enemy.Name,
                MaxHp = enemy.MaxHp,
                Stats = ToStats(enemy),
            }).ToList(),
        };
    }

    private static BattleLogStats ToStats(CharacterModel character)
    {
        return new BattleLogStats
        {
            Forza = character.Forza,
            Magia = character.Magia,
            Difesa = character.Difesa,
            Agilita = character.Agilita,
            Fortuna = character.Fortuna,
            Intelligence = character.Intelligence,
            Fede = character.Fede,
        };
    }

    public static void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        if (_current is null)
        {
            BeginBattle(state, "sconosciuto");
        }

        _turnCounter++;
        var turn = new BattleLogTurn
        {
            TurnNumber = _turnCounter,
            PlayerAction = request.ActionType.ToString(),
            PlayerHpAfter = state.Player.Hp,
            EnemyHpAfter = state.Enemies.Select(enemy => enemy.Hp).ToList(),
        };

        foreach (var evt in outcome.Events)
        {
            turn.Events.Add(new BattleLogTurnEvent
            {
                EventType = evt.EventType.ToString(),
                Message = evt.Message,
                SourceId = evt.SourceId,
                TargetId = evt.TargetId,
                Amount = evt.Amount,
                Metadata = new Dictionary<string, string>(evt.Metadata),
            });

            if (evt.EventType != CombatEventType.DamageDealt || evt.Amount is not int amount)
            {
                continue;
            }

            if (evt.SourceId == CombatService.PlayerActorId)
            {
                _totalDamageDealt += amount;
            }
            else
            {
                _totalDamageTaken += amount;
            }
        }

        _current!.Turns.Add(turn);
    }

    public static void EndBattle(GameState state, string outcome)
    {
        if (_current is null)
        {
            return;
        }

        _current.Outcome = outcome;
        _current.EndedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _current.PlayerHpEnd = state.Player.Hp;
        _current.TotalTurns = _turnCounter;

        WriteRecord(_current);
        AppendSummary(_current, _totalDamageDealt, _totalDamageTaken);
        PruneOldRecords();
        _current = null;
    }

    // Per-battle JSON files would otherwise accumulate forever in user://.
    // The CSV summary keeps one row per battle, so older detail can go.
    private static void PruneOldRecords()
    {
        using var dir = DirAccess.Open(LogDir);
        if (dir is null)
        {
            return;
        }

        var records = dir.GetFiles()
            .Where(name => name.StartsWith("battle_", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        for (var i = 0; i < records.Count - MaxDetailedRecords; i++)
        {
            dir.Remove(records[i]);
        }
    }

    private static void WriteRecord(BattleLogRecord record)
    {
        var path = $"{LogDir}/battle_{record.StartedAtUnix}_{record.BattleId}.json";
        var json = JsonSerializer.Serialize(record, JsonOptions);
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }

    private static void AppendSummary(BattleLogRecord record, int damageDealt, int damageTaken)
    {
        var exists = FileAccess.FileExists(SummaryPath);
        var mode = exists ? FileAccess.ModeFlags.ReadWrite : FileAccess.ModeFlags.Write;
        using var file = FileAccess.Open(SummaryPath, mode);
        if (file is null)
        {
            return;
        }

        if (exists)
        {
            file.SeekEnd();
        }
        else
        {
            file.StoreLine(SummaryHeader);
        }

        var enemyNames = string.Join("|", record.Enemies.Select(enemy => enemy.Name));
        var row = string.Join(",",
            record.StartedAtUnix.ToString(CultureInfo.InvariantCulture),
            record.Context,
            record.Enemies.Count.ToString(CultureInfo.InvariantCulture),
            $"\"{enemyNames}\"",
            record.Outcome,
            record.TotalTurns.ToString(CultureInfo.InvariantCulture),
            record.PlayerMaxHpStart.ToString(CultureInfo.InvariantCulture),
            record.PlayerHpStart.ToString(CultureInfo.InvariantCulture),
            record.PlayerHpEnd.ToString(CultureInfo.InvariantCulture),
            damageDealt.ToString(CultureInfo.InvariantCulture),
            damageTaken.ToString(CultureInfo.InvariantCulture));
        file.StoreLine(row);
    }
}
