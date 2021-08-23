using UnityEngine;

public struct EdgeVertices
{
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;
    public Vector3 v4;
    public Vector3 v5;

    public EdgeVertices(Vector3 _corner1, Vector3 _corner2)
    {
        v1 = _corner1;
        v2 = Vector3.Lerp(_corner1, _corner2, 0.25f);
        v3 = Vector3.Lerp(_corner1, _corner2, 0.5f);
        v4 = Vector3.Lerp(_corner1, _corner2, 0.75f);
        v5 = _corner2;
    }

    public EdgeVertices(Vector3 _corner1, Vector4 _corner2, float _outerStep)
    {
        v1 = _corner1;
        v2 = Vector3.Lerp(_corner1, _corner2, _outerStep);
        v3 = Vector3.Lerp(_corner1, _corner2, 0.5f);
        v4 = Vector3.Lerp(_corner1, _corner2, 1.0f - _outerStep);
        v5 = _corner2;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices _a, EdgeVertices _b, int _step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(_a.v1, _b.v1, _step);
        result.v2 = HexMetrics.TerraceLerp(_a.v2, _b.v2, _step);
        result.v3 = HexMetrics.TerraceLerp(_a.v3, _b.v3, _step);
        result.v4 = HexMetrics.TerraceLerp(_a.v4, _b.v4, _step);
        result.v5 = HexMetrics.TerraceLerp(_a.v5, _b.v5, _step);

        return result;
    }
}