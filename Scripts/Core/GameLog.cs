using System;
using Godot;

// Logging for code that also runs in the standalone test runner. Any GD.* or
// Engine.* call segfaults when GodotSharp is loaded outside the engine, and
// that includes probes like Engine.GetMainLoop(), so it cannot be detected at
// runtime: the test entry point turns EngineAvailable off explicitly.
public static class GameLog
{
    public static bool EngineAvailable { get; set; } = true;

    public static void Error(string message)
    {
        if (EngineAvailable)
        {
            GD.PrintErr(message);
            return;
        }

        Console.Error.WriteLine(message);
    }
}
