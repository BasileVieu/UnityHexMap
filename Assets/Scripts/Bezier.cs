using UnityEngine;

public static class Bezier
{
    public static Vector3 GetPoint(Vector3 _a, Vector3 _b, Vector3 _c, float _t)
    {
        float r = 1.0f - _t;

        return r * r * _a + 2.0f * r * _t * _b + _t * _t * _c;
    }

    public static Vector3 GetDerivative(Vector3 _a, Vector3 _b, Vector3 _c, float _t)
    {
        return 2.0f * ((1.0f - _t) * (_b - _a) + _t * (_c - _b));
    }
}