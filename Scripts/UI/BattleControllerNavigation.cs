using System.Collections.Generic;
using Godot;

public partial class BattleController
{
    private IEnumerable<Button> AllInteractiveButtons()
    {
        foreach (var button in _targetButtons)
        {
            yield return button;
        }

        foreach (var button in _moveButtons)
        {
            yield return button;
        }

        foreach (var button in _actionButtons)
        {
            yield return button;
        }
    }

    private static void WireRowNavigation(IReadOnlyList<Button> row)
    {
        for (var i = 0; i < row.Count; i++)
        {
            var left = i > 0 ? row[i - 1] : row[i];
            var right = i < row.Count - 1 ? row[i + 1] : row[i];
            row[i].FocusNeighborLeft = row[i].GetPathTo(left);
            row[i].FocusNeighborRight = row[i].GetPathTo(right);
        }
    }

    private static void WireVerticalNavigation(IReadOnlyList<Button> topRow, IReadOnlyList<Button> bottomRow)
    {
        for (var i = 0; i < topRow.Count; i++)
        {
            var mapped = MapRowIndex(i, topRow.Count, bottomRow.Count);
            topRow[i].FocusNeighborBottom = topRow[i].GetPathTo(bottomRow[mapped]);
        }

        for (var i = 0; i < bottomRow.Count; i++)
        {
            var mapped = MapRowIndex(i, bottomRow.Count, topRow.Count);
            bottomRow[i].FocusNeighborTop = bottomRow[i].GetPathTo(topRow[mapped]);
        }
    }

    private static int MapRowIndex(int index, int fromCount, int toCount)
    {
        if (toCount <= 1 || fromCount <= 1)
        {
            return 0;
        }

        var ratio = index / (float)(fromCount - 1);
        return Mathf.Clamp(Mathf.RoundToInt(ratio * (toCount - 1)), 0, toCount - 1);
    }
}
