using Godot;

// Builds a rounded voxel bust from a single flat sprite: a chamfer distance
// transform over the sprite's silhouette drives a per-column "how deep is
// this point inside the shape" profile, which is used to stack a
// symmetric front/back slab of voxel layers - thin at the silhouette edge,
// thick at the interior - so the result reads as an actual volume from the
// side instead of a uniformly-thick cutout.
public static class SpritePixelMeshFactory
{
    private const int TargetGridHeight = 28;
    private const int MinGridWidth = 4;
    private const int MaxGridWidth = TargetGridHeight * 2;
    private const int MaxDepthLayers = 10;
    private const float TargetHeight = 1.5f;
    private const float AlphaThreshold = 0.45f;

    public static ArrayMesh? Build(Image? sourceImage)
    {
        var grid = BuildGrid(sourceImage, out var gridW, out var gridH);
        if (grid is null)
        {
            return null;
        }

        var solid = new bool[gridW, gridH];
        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                solid[x, y] = grid[x, y].A > 0f;
            }
        }

        var depthCount = BuildDepthProfile(solid, gridW, gridH);
        var cellSize = TargetHeight / gridH;
        return BuildMesh(grid, solid, depthCount, gridW, gridH, cellSize);
    }

    private static Color[,]? BuildGrid(Image? sourceImage, out int gridW, out int gridH)
    {
        gridW = 0;
        gridH = 0;
        if (sourceImage is null || sourceImage.IsEmpty())
        {
            return null;
        }

        var image = (Image)sourceImage.Duplicate();
        if (image.GetFormat() != Image.Format.Rgba8)
        {
            image.Convert(Image.Format.Rgba8);
        }

        var srcW = image.GetWidth();
        var srcH = image.GetHeight();
        if (srcW <= 0 || srcH <= 0)
        {
            return null;
        }

        var aspect = srcW / (float)srcH;
        gridH = TargetGridHeight;
        gridW = Mathf.Clamp((int)Mathf.Round(gridH * aspect), MinGridWidth, MaxGridWidth);
        image.Resize(gridW, gridH, Image.Interpolation.Lanczos);

        var grid = new Color[gridW, gridH];
        var any = false;
        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                var pixel = image.GetPixel(x, y);
                if (pixel.A >= AlphaThreshold)
                {
                    grid[x, y] = new Color(pixel.R, pixel.G, pixel.B, 1f);
                    any = true;
                }
            }
        }

        return any ? grid : null;
    }

    // How many voxel layers deep (front-to-back) each solid column gets,
    // scaled from 1 (right at the silhouette edge) up to MaxDepthLayers
    // (deepest interior point), via a sqrt falloff for a rounded bulge
    // rather than a hard-edged pillar.
    private static int[,] BuildDepthProfile(bool[,] solid, int gridW, int gridH)
    {
        var dist = ComputeChamferDistance(solid, gridW, gridH);
        var maxDist = 0f;
        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                if (solid[x, y])
                {
                    maxDist = Mathf.Max(maxDist, dist[x, y]);
                }
            }
        }

        if (maxDist <= 0f)
        {
            maxDist = 1f;
        }

        var depthCount = new int[gridW, gridH];
        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                if (!solid[x, y])
                {
                    continue;
                }

                var profile = Mathf.Sqrt(Mathf.Clamp(dist[x, y] / maxDist, 0f, 1f));
                depthCount[x, y] = Mathf.Clamp((int)Mathf.Round(MaxDepthLayers * profile), 1, MaxDepthLayers);
            }
        }

        return depthCount;
    }

    private static float[,] ComputeChamferDistance(bool[,] solid, int gridW, int gridH)
    {
        const float Big = 1e6f;
        const float Ortho = 1f;
        const float Diag = 1.41421356f;

        var dist = new float[gridW, gridH];
        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                dist[x, y] = solid[x, y] ? Big : 0f;
            }
        }

        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                if (!solid[x, y])
                {
                    continue;
                }

                var best = dist[x, y];
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x - 1, y) + Ortho);
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x, y - 1) + Ortho);
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x - 1, y - 1) + Diag);
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x + 1, y - 1) + Diag);
                dist[x, y] = best;
            }
        }

        for (var y = gridH - 1; y >= 0; y--)
        {
            for (var x = gridW - 1; x >= 0; x--)
            {
                if (!solid[x, y])
                {
                    continue;
                }

                var best = dist[x, y];
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x + 1, y) + Ortho);
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x, y + 1) + Ortho);
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x + 1, y + 1) + Diag);
                best = Mathf.Min(best, SampleDist(dist, gridW, gridH, x - 1, y + 1) + Diag);
                dist[x, y] = best;
            }
        }

        return dist;
    }

    private static float SampleDist(float[,] dist, int gridW, int gridH, int x, int y)
    {
        if (x < 0 || x >= gridW || y < 0 || y >= gridH)
        {
            return 0f;
        }

        return dist[x, y];
    }

    private static bool IsSolid3D(bool[,] solid, int[,] depthCount, int gridW, int gridH, int centerZ, int x, int y, int z)
    {
        if (x < 0 || x >= gridW || y < 0 || y >= gridH || !solid[x, y])
        {
            return false;
        }

        var half = depthCount[x, y];
        var zStart = centerZ - (half / 2);
        var zEnd = zStart + half;
        return z >= zStart && z < zEnd;
    }

    private static ArrayMesh? BuildMesh(Color[,] grid, bool[,] solid, int[,] depthCount, int gridW, int gridH, float cellSize)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var built = false;
        var centerZ = MaxDepthLayers / 2;

        for (var y = 0; y < gridH; y++)
        {
            for (var x = 0; x < gridW; x++)
            {
                if (!solid[x, y])
                {
                    continue;
                }

                var half = depthCount[x, y];
                var zStart = centerZ - (half / 2);
                var zEnd = zStart + half;
                var color = grid[x, y];
                var minX = (x - (gridW / 2f)) * cellSize;
                var maxX = minX + cellSize;
                var minY = (gridH - 1 - y) * cellSize;
                var maxY = minY + cellSize;

                for (var z = zStart; z < zEnd; z++)
                {
                    var minZ = (z - centerZ) * cellSize;
                    var maxZ = minZ + cellSize;

                    if (!IsSolid3D(solid, depthCount, gridW, gridH, centerZ, x, y, z - 1))
                    {
                        AddFace(st, color,
                            new Vector3(minX, minY, minZ), new Vector3(maxX, minY, minZ),
                            new Vector3(maxX, maxY, minZ), new Vector3(minX, maxY, minZ),
                            new Vector3(0f, 0f, -1f));
                    }

                    if (!IsSolid3D(solid, depthCount, gridW, gridH, centerZ, x, y, z + 1))
                    {
                        AddFace(st, color,
                            new Vector3(maxX, minY, maxZ), new Vector3(minX, minY, maxZ),
                            new Vector3(minX, maxY, maxZ), new Vector3(maxX, maxY, maxZ),
                            new Vector3(0f, 0f, 1f));
                    }

                    if (!IsSolid3D(solid, depthCount, gridW, gridH, centerZ, x - 1, y, z))
                    {
                        AddFace(st, color,
                            new Vector3(minX, minY, maxZ), new Vector3(minX, minY, minZ),
                            new Vector3(minX, maxY, minZ), new Vector3(minX, maxY, maxZ),
                            new Vector3(-1f, 0f, 0f));
                    }

                    if (!IsSolid3D(solid, depthCount, gridW, gridH, centerZ, x + 1, y, z))
                    {
                        AddFace(st, color,
                            new Vector3(maxX, minY, minZ), new Vector3(maxX, minY, maxZ),
                            new Vector3(maxX, maxY, maxZ), new Vector3(maxX, maxY, minZ),
                            new Vector3(1f, 0f, 0f));
                    }

                    if (!IsSolid3D(solid, depthCount, gridW, gridH, centerZ, x, y - 1, z))
                    {
                        AddFace(st, color,
                            new Vector3(minX, maxY, minZ), new Vector3(maxX, maxY, minZ),
                            new Vector3(maxX, maxY, maxZ), new Vector3(minX, maxY, maxZ),
                            new Vector3(0f, 1f, 0f));
                    }

                    if (!IsSolid3D(solid, depthCount, gridW, gridH, centerZ, x, y + 1, z))
                    {
                        AddFace(st, color,
                            new Vector3(minX, minY, maxZ), new Vector3(maxX, minY, maxZ),
                            new Vector3(maxX, minY, minZ), new Vector3(minX, minY, minZ),
                            new Vector3(0f, -1f, 0f));
                    }

                    built = true;
                }
            }
        }

        if (!built)
        {
            return null;
        }

        st.Index();
        return st.Commit();
    }

    private static void AddFace(SurfaceTool st, Color color, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        st.SetNormal(normal);
        st.SetColor(color);

        st.SetUV(new Vector2(0f, 0f));
        st.AddVertex(a);
        st.SetUV(new Vector2(1f, 0f));
        st.AddVertex(b);
        st.SetUV(new Vector2(1f, 1f));
        st.AddVertex(c);

        st.SetUV(new Vector2(0f, 0f));
        st.AddVertex(a);
        st.SetUV(new Vector2(1f, 1f));
        st.AddVertex(c);
        st.SetUV(new Vector2(0f, 1f));
        st.AddVertex(d);
    }
}
