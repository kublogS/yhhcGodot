using System;
using Godot;

public sealed class PlayerNavigationResolver
{
    private readonly DungeonData _dungeon;
    private readonly float _tileSize;
    private readonly float _playerRadius;
    private readonly float _solidInset;

    public PlayerNavigationResolver(DungeonData dungeon, float tileSize, float playerRadius, float solidInset = 0.05f)
    {
        _dungeon = dungeon;
        _tileSize = MathF.Max(0.1f, tileSize);
        _playerRadius = MathF.Max(0.05f, playerRadius);
        _solidInset = Mathf.Clamp(solidInset, 0f, _tileSize * 0.25f);
    }

    public Vector3 ResolveMotion(Vector3 currentPosition, Vector3 intendedMotion)
    {
        if (CanOccupy(currentPosition + intendedMotion))
        {
            return intendedMotion;
        }

        var resolvedX = ResolveAxis(currentPosition, intendedMotion.X, moveOnX: true);
        currentPosition.X += resolvedX;
        var resolvedZ = ResolveAxis(currentPosition, intendedMotion.Z, moveOnX: false);
        return new Vector3(resolvedX, 0f, resolvedZ);
    }

    private float ResolveAxis(Vector3 origin, float delta, bool moveOnX)
    {
        if (Mathf.IsZeroApprox(delta))
        {
            return 0f;
        }

        var directTarget = moveOnX ? origin + new Vector3(delta, 0f, 0f) : origin + new Vector3(0f, 0f, delta);
        if (CanOccupy(directTarget))
        {
            return delta;
        }

        var low = 0f;
        var high = 1f;
        for (var i = 0; i < 6; i++)
        {
            var mid = (low + high) * 0.5f;
            var probeDelta = delta * mid;
            var probe = moveOnX ? origin + new Vector3(probeDelta, 0f, 0f) : origin + new Vector3(0f, 0f, probeDelta);
            if (CanOccupy(probe))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return delta * low;
    }

    private bool CanOccupy(Vector3 worldPosition)
    {
        var center = WorldToGrid(worldPosition);
        var range = (int)MathF.Ceiling((_playerRadius + (_tileSize * 0.5f)) / _tileSize) + 1;

        for (var y = center.Y - range; y <= center.Y + range; y++)
        {
            for (var x = center.X - range; x <= center.X + range; x++)
            {
                if (_dungeon.IsWalkable(x, y))
                {
                    continue;
                }

                if (IntersectsSolidCell(worldPosition, x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IntersectsSolidCell(Vector3 worldPosition, int gridX, int gridY)
    {
        var center = DungeonGenerator.GridToWorld(gridX, gridY, _tileSize);
        var halfExtent = MathF.Max(0.1f, (_tileSize * 0.5f) - _solidInset);
        var nearestX = Mathf.Clamp(worldPosition.X, center.X - halfExtent, center.X + halfExtent);
        var nearestZ = Mathf.Clamp(worldPosition.Z, center.Z - halfExtent, center.Z + halfExtent);
        var dx = worldPosition.X - nearestX;
        var dz = worldPosition.Z - nearestZ;
        return (dx * dx) + (dz * dz) < (_playerRadius * _playerRadius);
    }

    private Vector2I WorldToGrid(Vector3 worldPosition)
    {
        var x = (int)MathF.Round(worldPosition.X / _tileSize);
        var y = (int)MathF.Round(worldPosition.Z / _tileSize);
        return new Vector2I(x, y);
    }
}
