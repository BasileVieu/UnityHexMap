using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    private Mesh hexMesh;

    [NonSerialized] private List<Vector3> vertices;
    [NonSerialized] private List<Vector3> cellIndices;
    [NonSerialized] private List<Vector2> uvs;
    [NonSerialized] private List<Vector2> uv2S;
    [NonSerialized] private List<Color> cellWeights;
    [NonSerialized] private List<int> triangles;

    private MeshCollider meshCollider;

    public bool useCollider;
    public bool useCellData;
    public bool useUvCoordinates;
    public bool useUv2Coordinates;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();

        if (useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        hexMesh.name = "Hex Mesh";
    }

    public void Clear()
    {
        hexMesh.Clear();

        vertices = ListPool<Vector3>.Get();

        if (useCellData)
        {
            cellWeights = ListPool<Color>.Get();
            cellIndices = ListPool<Vector3>.Get();
        }

        if (useUvCoordinates)
        {
            uvs = ListPool<Vector2>.Get();
        }

        if (useUv2Coordinates)
        {
            uv2S = ListPool<Vector2>.Get();
        }

        triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);

        if (useCellData)
        {
            hexMesh.SetColors(cellWeights);
            ListPool<Color>.Add(cellWeights);
            
            hexMesh.SetUVs(2, cellIndices);
            ListPool<Vector3>.Add(cellIndices);
        }

        if (useUvCoordinates)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }

        if (useUv2Coordinates)
        {
            hexMesh.SetUVs(1, uv2S);
            ListPool<Vector2>.Add(uv2S);
        }

        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);

        hexMesh.RecalculateNormals();

        if (useCollider)
        {
            meshCollider.sharedMesh = hexMesh;
        }
    }

    public void AddTriangle(Vector3 _v1, Vector3 _v2, Vector3 _v3)
    {
        int vertexIndex = vertices.Count;

        vertices.Add(HexMetrics.Perturb(_v1));
        vertices.Add(HexMetrics.Perturb(_v2));
        vertices.Add(HexMetrics.Perturb(_v3));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUnperturbed(Vector3 _v1, Vector3 _v2, Vector3 _v3)
    {
        int vertexIndex = vertices.Count;

        vertices.Add(_v1);
        vertices.Add(_v2);
        vertices.Add(_v3);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleCellData(Vector3 _indices, Color _weights1, Color _weights2, Color _weights3)
    {
        cellIndices.Add(_indices);
        cellIndices.Add(_indices);
        cellIndices.Add(_indices);
        cellWeights.Add(_weights1);
        cellWeights.Add(_weights2);
        cellWeights.Add(_weights3);
    }

    public void AddTriangleCellData(Vector3 _indices, Color _weights)
    {
        AddTriangleCellData(_indices, _weights, _weights, _weights);
    }

    public void AddTriangleUv(Vector2 _uv1, Vector2 _uv2, Vector2 _uv3)
    {
        uvs.Add(_uv1);
        uvs.Add(_uv2);
        uvs.Add(_uv3);
    }

    public void AddQuad(Vector3 _vector1, Vector3 _vector2, Vector3 _vector3, Vector3 _vector4)
    {
        int vertexIndex = vertices.Count;

        vertices.Add(HexMetrics.Perturb(_vector1));
        vertices.Add(HexMetrics.Perturb(_vector2));
        vertices.Add(HexMetrics.Perturb(_vector3));
        vertices.Add(HexMetrics.Perturb(_vector4));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    
    public void AddQuadCellData (Vector3 _indices, Color _weights1, Color _weights2, Color _weights3, Color _weights4)
    {
        cellIndices.Add(_indices);
        cellIndices.Add(_indices);
        cellIndices.Add(_indices);
        cellIndices.Add(_indices);
        cellWeights.Add(_weights1);
        cellWeights.Add(_weights2);
        cellWeights.Add(_weights3);
        cellWeights.Add(_weights4);
    }

    public void AddQuadCellData (Vector3 _indices, Color _weights1, Color _weights2)
    {
        AddQuadCellData(_indices, _weights1, _weights1, _weights2, _weights2);
    }

    public void AddQuadCellData (Vector3 _indices, Color _weights)
    {
        AddQuadCellData(_indices, _weights, _weights, _weights, _weights);
    }

    public void AddQuadUnperturbed(Vector3 _vector1, Vector3 _vector2, Vector3 _vector3, Vector3 _vector4)
    {
        int vertexIndex = vertices.Count;

        vertices.Add(_vector1);
        vertices.Add(_vector2);
        vertices.Add(_vector3);
        vertices.Add(_vector4);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    public void AddQuadUv(Vector2 _uv1, Vector2 _uv2, Vector2 _uv3, Vector2 _uv4)
    {
        uvs.Add(_uv1);
        uvs.Add(_uv2);
        uvs.Add(_uv3);
        uvs.Add(_uv4);
    }

    public void AddQuadUv(float _uMin, float _uMax, float _vMin, float _vMax)
    {
        uvs.Add(new Vector2(_uMin, _vMin));
        uvs.Add(new Vector2(_uMax, _vMin));
        uvs.Add(new Vector2(_uMin, _vMax));
        uvs.Add(new Vector2(_uMax, _vMax));
    }

    public void AddTriangleUv2(Vector2 _uv1, Vector2 _uv2, Vector2 _uv3)
    {
        uv2S.Add(_uv1);
        uv2S.Add(_uv2);
        uv2S.Add(_uv3);
    }

    public void AddQuadUv2(Vector2 _uv1, Vector2 _uv2, Vector2 _uv3, Vector2 _uv4)
    {
        uv2S.Add(_uv1);
        uv2S.Add(_uv2);
        uv2S.Add(_uv3);
        uv2S.Add(_uv4);
    }

    public void AddQuadUv2(float _uMin, float _uMax, float _vMin, float _vMax)
    {
        uv2S.Add(new Vector2(_uMin, _vMin));
        uv2S.Add(new Vector2(_uMax, _vMin));
        uv2S.Add(new Vector2(_uMin, _vMax));
        uv2S.Add(new Vector2(_uMax, _vMax));
    }
}