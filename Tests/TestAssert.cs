using System;
using System.Collections.Generic;

public sealed class TestAssert
{
    private readonly List<string> _failures = new();

    public int FailureCount => _failures.Count;
    public IReadOnlyList<string> Failures => _failures;

    public void True(bool condition, string message)
    {
        if (!condition)
        {
            _failures.Add(message);
        }
    }

    public void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            _failures.Add($"{message} (expected={expected}, actual={actual})");
        }
    }

    public void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string message)
    {
        if (expected.Count != actual.Count)
        {
            _failures.Add($"{message} (different count: {expected.Count} vs {actual.Count})");
            return;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i]))
            {
                _failures.Add($"{message} (index {i}: expected={expected[i]}, actual={actual[i]})");
                return;
            }
        }
    }
}
