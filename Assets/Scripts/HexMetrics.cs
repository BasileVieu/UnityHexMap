using UnityEngine;

public static class HexMetrics
{
    public const float outerToInner = 0.866025404f;
    public const float innerToOuter = 1.0f / outerToInner;

    public const float outerRadius = 10f;

    public const float innerRadius = outerRadius * outerToInner;

    public const float innerDiameter = innerRadius * 2.0f;

    public const float solidFactor = 0.8f;

    public const float blendFactor = 1.0f - solidFactor;

    public const float elevationStep = 3.0f;

    public const int terracesPerSlope = 2;

    public const int terraceSteps = terracesPerSlope * 2 + 1;

    public const float horizontalTerraceStepSize = 1.0f / terraceSteps;

    public const float verticalTerraceStepSize = 1.0f / (terracesPerSlope + 1);

    public const float cellPerturbStrength = 4.0f;

    public const float noiseScale = 0.003f;

    public const float elevationPerturbStrength = 1.5f;

    public const int chunkSizeX = 5;
    public const int chunkSizeZ = 5;

    public const float streamBedElevationOffset = -1.75f;

    public const float waterElevationOffset = -0.5f;

    public const float waterFactor = 0.6f;

    public const float waterBlendFactor = 1.0f - waterFactor;

    public static Texture2D noiseSource;

    private static Vector3[] corners =
    {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(0f, 0f, outerRadius)
    };

    public const int hashGridSize = 256;

    public const float hashGridScale = 0.25f;

    private static HexHash[] hashGrid;

    private static float[][] featureThresholds =
    {
            new float[] {0.0f, 0.0f, 0.4f},
            new float[] {0.0f, 0.4f, 0.6f},
            new float[] {0.4f, 0.6f, 0.8f}
    };

    public const float wallHeight = 4.0f;

    public const float wallYOffset = -1.0f;

    public const float wallThickness = 0.75f;

    public static float[] GetFeatureThresholds(int _level) => featureThresholds[_level];

    public const float wallElevationOffset = verticalTerraceStepSize;

    public const float wallTowerThreshold = 0.5f;

    public const float bridgeDesignLength = 7.0f;

    public static int wrapSize;

    public static bool Wrapping => wrapSize > 0;

    public static void InitializeHashGrid(int _seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];

        Random.State currentState = Random.state;

        Random.InitState(_seed);

        for (var i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }

        Random.state = currentState;
    }

    public static HexHash SampleHashGrid(Vector3 _position)
    {
        int x = (int) (_position.x * hashGridScale) % hashGridSize;

        if (x < 0)
        {
            x += hashGridSize;
        }

        int z = (int) (_position.z * hashGridScale) % hashGridSize;

        if (z < 0)
        {
            z += hashGridSize;
        }

        return hashGrid[x + z * hashGridSize];
    }

    public static Vector3 GetFirstCorner(HexDirection _direction) => corners[(int) _direction];

    public static Vector3 GetSecondCorner(HexDirection _direction) => corners[(int) _direction + 1];

    public static Vector3 GetFirstSolidCorner(HexDirection _direction) => corners[(int) _direction] * solidFactor;

    public static Vector3 GetSecondSolidCorner(HexDirection _direction) => corners[(int) _direction + 1] * solidFactor;

    public static Vector3 GetSolidEdgeMiddle(HexDirection _direction) =>
            corners[(int) _direction] + corners[(int) _direction + 1] * (0.5f * solidFactor);

    public static Vector3 GetBridge(HexDirection _direction) =>
            (corners[(int) _direction] + corners[(int) _direction + 1]) * blendFactor;

    public static Vector3 TerraceLerp(Vector3 _a, Vector3 _b, int _step)
    {
        float h = _step * horizontalTerraceStepSize;
        _a.x += (_b.x - _a.x) * h;
        _a.z += (_b.z - _a.z) * h;

        float v = (_step + 1) / 2 * verticalTerraceStepSize;
        _a.y += (_b.y - _a.y) * v;

        return _a;
    }

    public static Color TerraceLerp(Color _a, Color _b, int _step)
    {
        float h = _step * horizontalTerraceStepSize;

        return Color.Lerp(_a, _b, h);
    }

    public static HexEdgeType GetEdgeType(int _elevation1, int _elevation2)
    {
        if (_elevation1 == _elevation2)
        {
            return HexEdgeType.Flat;
        }

        int delta = _elevation2 - _elevation1;

        if (delta == 1
            || delta == -1)
        {
            return HexEdgeType.Slope;
        }

        return HexEdgeType.Cliff;
    }

    public static Vector4 SampleNoise(Vector3 _position)
    {
        Vector4 sample = noiseSource.GetPixelBilinear(_position.x * noiseScale, _position.z * noiseScale);

        if (Wrapping
            && _position.x < innerDiameter * 1.5f)
        {
            Vector4 sample2 = noiseSource.GetPixelBilinear((_position.x + wrapSize * innerDiameter) * noiseScale,
                                                           _position.z * noiseScale);

            sample = Vector4.Lerp(sample2, sample, _position.x * (1.0f / innerDiameter) - 0.5f);
        }

        return sample;
    }

    public static Vector3 Perturb(Vector3 _position)
    {
        Vector4 sample = SampleNoise(_position);

        _position.x += (sample.x * 2.0f - 1.0f) * cellPerturbStrength;
        _position.z += (sample.z * 2.0f - 1.0f) * cellPerturbStrength;

        return _position;
    }

    public static Vector3 GetFirstWaterCorner(HexDirection _direction) => corners[(int) _direction] * waterFactor;

    public static Vector3 GetSecondWaterCorner(HexDirection _direction) => corners[(int) _direction + 1] * waterFactor;

    public static Vector3 GetWaterBridge(HexDirection _direction) =>
            (corners[(int) _direction] + corners[(int) _direction + 1]) * waterBlendFactor;

    public static Vector3 WallThicknessOffset(Vector3 _near, Vector3 _far)
    {
        Vector3 offset;

        offset.x = _far.x - _near.x;
        offset.y = 0.0f;
        offset.z = _far.z - _near.z;

        return offset.normalized * (wallThickness * 0.5f);
    }

    public static Vector3 WallLerp(Vector3 _near, Vector3 _far)
    {
        _near.x += (_far.x - _near.x) * 0.5f;
        _near.z += (_far.z - _near.z) * 0.5f;

        float v = _near.y < _far.y ? wallElevationOffset : 1.0f - wallElevationOffset;
        _near.y += (_far.y - _near.y) * v + wallYOffset;

        return _near;
    }
}