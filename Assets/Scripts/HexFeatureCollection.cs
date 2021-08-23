using UnityEngine;

[System.Serializable]
public struct HexFeatureCollection
{
    public Transform[] prefabs;

    public Transform Pick(float _choice) => prefabs[(int) (_choice * prefabs.Length)];
}