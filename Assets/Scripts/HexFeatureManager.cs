using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections;
    public HexFeatureCollection[] farmCollections;
    public HexFeatureCollection[] plantCollections;

    public HexMesh walls;

    public Transform wallTower;
    public Transform bridge;

    public Transform[] special;

    private Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }

        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);

        walls.Clear();
    }

    public void Apply()
    {
        walls.Apply();
    }

    public void AddFeature(HexCell _cell, Vector3 _position)
    {
        if (_cell.IsSpecial)
        {
            return;
        }

        HexHash hash = HexMetrics.SampleHashGrid(_position);

        Transform prefab = PickPrefab(urbanCollections, _cell.UrbanLevel, hash.a, hash.d);

        Transform otherPrefab = PickPrefab(farmCollections, _cell.FarmLevel, hash.b, hash.d);

        float usedHash = hash.a;

        if (prefab)
        {
            if (otherPrefab
                && hash.b < hash.a)
            {
                prefab = otherPrefab;

                usedHash = hash.b;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;

            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(plantCollections, _cell.PlantLevel, hash.c, hash.d);

        if (prefab)
        {
            if (otherPrefab
                && hash.c < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }

        Transform instance = Instantiate(prefab, container, false);
        _position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(_position);
        instance.localRotation = Quaternion.Euler(0.0f, 360.0f * hash.e, 0.0f);
    }

    Transform PickPrefab(HexFeatureCollection[] _collection, int _level, float _hash, float _choice)
    {
        if (_level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(_level - 1);

            for (var i = 0; i < thresholds.Length; i++)
            {
                if (_hash < thresholds[i])
                {
                    return _collection[i].Pick(_choice);
                }
            }
        }

        return null;
    }

    public void AddWall(EdgeVertices _near, HexCell _nearCell,
                        EdgeVertices _far, HexCell _farCell,
                        bool _hasRiver, bool _hasRoad)
    {
        if (_nearCell.Walled != _farCell.Walled
            && !_nearCell.IsUnderwater
            && !_farCell.IsUnderwater
            && _nearCell.GetEdgeType(_farCell) != HexEdgeType.Cliff)
        {
            AddWallSegment(_near.v1, _far.v1, _near.v2, _far.v2);

            if (_hasRiver
                || _hasRoad)
            {
                AddWallCap(_near.v2, _far.v2);
                AddWallCap(_far.v4, _near.v4);
            }
            else
            {
                AddWallSegment(_near.v2, _far.v2, _near.v3, _far.v3);
                AddWallSegment(_near.v3, _far.v3, _near.v4, _far.v4);
            }

            AddWallSegment(_near.v4, _far.v4, _near.v5, _far.v5);
        }
    }

    void AddWallSegment(Vector3 _nearLeft, Vector3 _farLeft,
                        Vector3 _nearRight, Vector3 _farRight,
                        bool _addTower = false)
    {
        _nearLeft = HexMetrics.Perturb(_nearLeft);
        _farLeft = HexMetrics.Perturb(_farLeft);
        _nearRight = HexMetrics.Perturb(_nearRight);
        _farRight = HexMetrics.Perturb(_farRight);

        Vector3 left = HexMetrics.WallLerp(_nearLeft, _farLeft);
        Vector3 right = HexMetrics.WallLerp(_nearRight, _farRight);

        Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(_nearLeft, _farLeft);
        Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(_nearRight, _farRight);

        float leftTop = left.y + HexMetrics.wallHeight;
        float rightTop = right.y + HexMetrics.wallHeight;

        Vector3 v3;
        Vector3 v4;

        Vector3 v1 = v3 = left - leftThicknessOffset;
        Vector3 v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3;
        Vector3 t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v2, v1, v4, v3);

        walls.AddQuadUnperturbed(t1, t2, v3, v4);

        if (_addTower)
        {
            Transform towerInstance = Instantiate(wallTower, container, false);
            Transform towerInstanceTransform = towerInstance.transform;
            towerInstanceTransform.localPosition = (left + right) * 0.5f;
            Vector3 rightDirection = right - left;
            rightDirection.y = 0.0f;
            towerInstanceTransform.right = rightDirection;
        }
    }

    void AddWallSegment(Vector3 _pivot, HexCell _pivotCell,
                        Vector3 _left, HexCell _leftCell,
                        Vector3 _right, HexCell _rightCell)
    {
        if (_pivotCell.IsUnderwater)
        {
            return;
        }

        bool hasLeftWall = !_leftCell.IsUnderwater && _pivotCell.GetEdgeType(_leftCell) != HexEdgeType.Cliff;
        bool hasRightWall = !_rightCell.IsUnderwater && _pivotCell.GetEdgeType(_rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRightWall)
            {
                var hasTower = false;

                if (_leftCell.Elevation == _rightCell.Elevation)
                {
                    HexHash hash = HexMetrics.SampleHashGrid((_pivot + _left + _right) * (1.0f / 3.0f));

                    hasTower = hash.e < HexMetrics.wallTowerThreshold;
                }

                AddWallSegment(_pivot, _left, _pivot, _right, hasTower);
            }
            else if (_leftCell.Elevation < _rightCell.Elevation)
            {
                AddWallWedge(_pivot, _left, _right);
            }
            else
            {
                AddWallCap(_pivot, _left);
            }
        }
        else if (hasRightWall)
        {
            if (_rightCell.Elevation < _leftCell.Elevation)
            {
                AddWallWedge(_right, _pivot, _left);
            }
            else
            {
                AddWallCap(_right, _pivot);
            }
        }
    }

    public void AddWall(Vector3 _c1, HexCell _cell1,
                        Vector3 _c2, HexCell _cell2,
                        Vector3 _c3, HexCell _cell3)
    {
        if (_cell1.Walled)
        {
            if (_cell2.Walled)
            {
                if (!_cell3.Walled)
                {
                    AddWallSegment(_c3, _cell3, _c1, _cell1, _c2, _cell2);
                }
            }
            else if (_cell3.Walled)
            {
                AddWallSegment(_c2, _cell2, _c3, _cell3, _c1, _cell1);
            }
            else
            {
                AddWallSegment(_c1, _cell1, _c2, _cell2, _c3, _cell3);
            }
        }
        else if (_cell2.Walled)
        {
            if (_cell3.Walled)
            {
                AddWallSegment(_c1, _cell1, _c2, _cell2, _c3, _cell3);
            }
            else
            {
                AddWallSegment(_c2, _cell2, _c3, _cell3, _c1, _cell1);
            }
        }
        else if (_cell3.Walled)
        {
            AddWallSegment(_c3, _cell3, _c1, _cell1, _c2, _cell2);
        }
    }

    void AddWallCap(Vector3 _near, Vector3 _far)
    {
        _near = HexMetrics.Perturb(_near);
        _far = HexMetrics.Perturb(_far);

        Vector3 center = HexMetrics.WallLerp(_near, _far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(_near, _far);

        Vector3 v3;
        Vector3 v4;

        Vector3 v1 = v3 = center - thickness;
        Vector3 v2 = v4 = center + thickness;

        v3.y = v4.y = center.y + HexMetrics.wallHeight;

        walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    void AddWallWedge(Vector3 _near, Vector3 _far, Vector3 _point)
    {
        _near = HexMetrics.Perturb(_near);
        _far = HexMetrics.Perturb(_far);
        _point = HexMetrics.Perturb(_point);

        Vector3 center = HexMetrics.WallLerp(_near, _far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(_near, _far);

        Vector3 v3;
        Vector3 v4;
        Vector3 pointTop = _point;
        _point.y = center.y;

        Vector3 v1 = v3 = center - thickness;
        Vector3 v2 = v4 = center + thickness;

        v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

        walls.AddQuadUnperturbed(v1, _point, v3, pointTop);
        walls.AddQuadUnperturbed(_point, v2, pointTop, v4);
        walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }

    public void AddBridge(Vector3 _roadCenter1, Vector3 _roadCenter2)
    {
        _roadCenter1 = HexMetrics.Perturb(_roadCenter1);
        _roadCenter2 = HexMetrics.Perturb(_roadCenter2);

        Transform instance = Instantiate(bridge, container, false);
        instance.localPosition = (_roadCenter1 + _roadCenter2) * 0.5f;
        instance.forward = _roadCenter2 - _roadCenter1;

        float length = Vector3.Distance(_roadCenter1, _roadCenter2);

        instance.localScale = new Vector3(1.0f, 1.0f, length * (1.0f / HexMetrics.bridgeDesignLength));
    }

    public void AddSpecialFeature(HexCell _cell, Vector3 _position)
    {
        Transform instance = Instantiate(special[_cell.SpecialIndex - 1], container, false);
        instance.localPosition = HexMetrics.Perturb(_position);

        HexHash hash = HexMetrics.SampleHashGrid(_position);
        instance.localRotation = Quaternion.Euler(0.0f, 360.0f * hash.e, 0.0f);
    }
}