using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    public HexMesh terrain;
    public HexMesh rivers;
    public HexMesh roads;
    public HexMesh water;
    public HexMesh waterShore;
    public HexMesh estuaries;

    private HexCell[] cells;

    private Canvas gridCanvas;

    public HexFeatureManager features;

    private static Color weights1 = new Color(1.0f, 0.0f, 0.0f);
    private static Color weights2 = new Color(0.0f, 1.0f, 0.0f);
    private static Color weights3 = new Color(0.0f, 0.0f, 1.0f);

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    public void AddCell(int _index, HexCell _cell)
    {
        cells[_index] = _cell;
        _cell.chunk = this;
        _cell.transform.SetParent(transform, false);
        _cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh()
    {
        enabled = true;
    }

    public void ShowUI(bool _visible)
    {
        gridCanvas.gameObject.SetActive(_visible);
    }

    void LateUpdate()
    {
        Triangulate();

        enabled = false;
    }

    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();

        foreach (HexCell cell in cells)
        {
            Triangulate(cell);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    void Triangulate(HexCell _cell)
    {
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, _cell);
        }

        if (!_cell.IsUnderwater)
        {
            if (!_cell.HasRiver
                && !_cell.HasRoads)
            {
                features.AddFeature(_cell, _cell.Position);
            }

            if (_cell.IsSpecial)
            {
                features.AddSpecialFeature(_cell, _cell.Position);
            }
        }
    }

    void Triangulate(HexDirection _direction, HexCell _cell)
    {
        Vector3 center = _cell.Position;

        var e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(_direction),
                                 center + HexMetrics.GetSecondSolidCorner(_direction));

        if (_cell.HasRiver)
        {
            if (_cell.HasRiverThroughEdge(_direction))
            {
                e.v3.y = _cell.StreamBedY;

                if (_cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(_direction, _cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(_direction, _cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(_direction, _cell, center, e);
            }
        }
        else
        {
            TriangulateWithoutRiver(_direction, _cell, center, e);

            if (!_cell.IsUnderwater
                && !_cell.HasRoadThroughEdge(_direction))
            {
                features.AddFeature(_cell, (center + e.v1 + e.v5) * (1.0f / 3.0f));
            }
        }

        if (_direction <= HexDirection.SE)
        {
            TriangulateConnection(_direction, _cell, e);
        }

        if (_cell.IsUnderwater)
        {
            TriangulateWater(_direction, _cell, center);
        }
    }

    void TriangulateWater(HexDirection _direction, HexCell _cell, Vector3 _center)
    {
        _center.y = _cell.WaterSurfaceY;

        HexCell neighbor = _cell.GetNeighbor(_direction);

        if (neighbor != null
            && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(_direction, _cell, neighbor, _center);
        }
        else
        {
            TriangulateOpenWater(_direction, _cell, neighbor, _center);
        }
    }

    void TriangulateWaterShore(HexDirection _direction, HexCell _cell, HexCell _neighbor, Vector3 _center)
    {
        var e1 = new EdgeVertices(_center + HexMetrics.GetFirstWaterCorner(_direction),
                                  _center + HexMetrics.GetSecondWaterCorner(_direction));

        water.AddTriangle(_center, e1.v1, e1.v2);
        water.AddTriangle(_center, e1.v2, e1.v3);
        water.AddTriangle(_center, e1.v3, e1.v4);
        water.AddTriangle(_center, e1.v4, e1.v5);

        Vector3 indices;
        indices.x = indices.z = _cell.Index;
        indices.y = _neighbor.Index;
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);

        Vector3 center2 = _neighbor.Position;

        if (_neighbor.ColumnIndex < _cell.ColumnIndex - 1)
        {
            center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        else if (_neighbor.ColumnIndex > _cell.ColumnIndex + 1)
        {
            center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        
        center2.y = _center.y;

        var e2 = new EdgeVertices(center2 + HexMetrics.GetSecondSolidCorner(_direction.Opposite()),
                                  center2 + HexMetrics.GetFirstSolidCorner(_direction.Opposite()));

        if (_cell.HasRiverThroughEdge(_direction))
        {
            TriangulateEstuary(e1, e2, _cell.HasIncomingRiver && _cell.IncomingRiver == _direction, indices);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

            waterShore.AddQuadUv(0.0f, 0.0f, 0.0f, 1.0f);
            waterShore.AddQuadUv(0.0f, 0.0f, 0.0f, 1.0f);
            waterShore.AddQuadUv(0.0f, 0.0f, 0.0f, 1.0f);
            waterShore.AddQuadUv(0.0f, 0.0f, 0.0f, 1.0f);

            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
        }

        HexCell nextNeighbor = _cell.GetNeighbor(_direction.Next());

        if (nextNeighbor != null)
        {
            Vector3 center3 = nextNeighbor.Position;

            if (nextNeighbor.ColumnIndex < _cell.ColumnIndex - 1)
            {
                center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            else if (nextNeighbor.ColumnIndex > _cell.ColumnIndex + 1)
            {
                center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            
            Vector3 v3 = center3 + (nextNeighbor.IsUnderwater
                                                          ? HexMetrics.GetFirstWaterCorner(_direction.Previous())
                                                          : HexMetrics.GetFirstSolidCorner(_direction.Previous()));
            v3.y = _center.y;

            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUv(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f),
                                     new Vector2(0.0f, nextNeighbor.IsUnderwater ? 0.0f : 1.0f));

            indices.z = nextNeighbor.Index;

            waterShore.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    }

    void TriangulateEstuary(EdgeVertices _e1, EdgeVertices _e2, bool _incomingRiver, Vector3 _indices)
    {
        waterShore.AddTriangle(_e2.v1, _e1.v2, _e1.v1);
        waterShore.AddTriangle(_e2.v5, _e1.v5, _e1.v4);

        waterShore.AddTriangleUv(new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f),
                                 new Vector2(0.0f, 0.0f));
        waterShore.AddTriangleUv(new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f),
                                 new Vector2(0.0f, 0.0f));

        waterShore.AddTriangleCellData(_indices, weights2, weights1, weights1);
        waterShore.AddTriangleCellData(_indices, weights2, weights1, weights1);

        estuaries.AddQuad(_e2.v1, _e1.v2, _e2.v2, _e1.v3);
        estuaries.AddTriangle(_e1.v3, _e2.v2, _e2.v4);
        estuaries.AddQuad(_e1.v3, _e1.v4, _e2.v4, _e2.v5);

        estuaries.AddQuadUv(new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f),
                            new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        estuaries.AddTriangleUv(new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f),
                                new Vector2(1.0f, 1.0f));
        estuaries.AddQuadUv(new Vector2(0f, 0f), new Vector2(0f, 0f),
                            new Vector2(1f, 1f), new Vector2(0f, 1f));

        estuaries.AddQuadCellData(_indices, weights2, weights1, weights2, weights1);
        estuaries.AddTriangleCellData(_indices, weights1, weights2, weights2);
        estuaries.AddQuadCellData(_indices, weights1, weights2);

        if (_incomingRiver)
        {
            estuaries.AddQuadUv2(new Vector2(1.5f, 1.0f), new Vector2(0.7f, 1.15f),
                                 new Vector2(1.0f, 0.8f), new Vector2(0.5f, 1.1f));
            estuaries.AddTriangleUv2(new Vector2(0.5f, 1.1f), new Vector2(1.0f, 0.8f),
                                     new Vector2(0.0f, 0.8f));
            estuaries.AddQuadUv2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                                 new Vector2(0.0f, 0.8f), new Vector2(-0.5f, 1.0f));
        }
        else
        {
            estuaries.AddQuadUv2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                                 new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            estuaries.AddTriangleUv2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f),
                                     new Vector2(1f, 0f));
            estuaries.AddQuadUv2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                                 new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
        }
    }

    void TriangulateOpenWater(HexDirection _direction, HexCell _cell, HexCell _neighbor, Vector3 _center)
    {
        Vector3 c1 = _center + HexMetrics.GetFirstWaterCorner(_direction);
        Vector3 c2 = _center + HexMetrics.GetSecondWaterCorner(_direction);

        water.AddTriangle(_center, c1, c2);

        Vector3 indices;
        indices.x = indices.y = indices.z = _cell.Index;
        
        water.AddTriangleCellData(indices, weights1);

        if (_direction <= HexDirection.SE
            && _neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(_direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            indices.y = _neighbor.Index;

            water.AddQuadCellData(indices, weights1, weights2);

            if (_direction <= HexDirection.E)
            {
                HexCell nextNeighbor = _cell.GetNeighbor(_direction.Next());

                if (nextNeighbor == null
                    || !nextNeighbor.IsUnderwater)
                {
                    return;
                }

                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(_direction.Next()));

                indices.z = nextNeighbor.Index;

                water.AddTriangleCellData(indices, weights1, weights2, weights3);
            }
        }
    }

    void TriangulateWithoutRiver(HexDirection _direction, HexCell _cell, Vector3 _center, EdgeVertices _e)
    {
        TriangulateEdgeFan(_center, _e, _cell.Index);

        if (_cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(_direction, _cell);

            TriangulateRoad(_center,
                            Vector3.Lerp(_center, _e.v1, interpolators.x),
                            Vector3.Lerp(_center, _e.v5, interpolators.y),
                            _e, _cell.HasRoadThroughEdge(_direction), _cell.Index);
        }
    }

    Vector2 GetRoadInterpolators(HexDirection _direction, HexCell _cell)
    {
        Vector2 interpolators;

        if (_cell.HasRoadThroughEdge(_direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x = _cell.HasRoadThroughEdge(_direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y = _cell.HasRoadThroughEdge(_direction.Next()) ? 0.5f : 0.25f;
        }

        return interpolators;
    }

    void TriangulateAdjacentToRiver(HexDirection _direction, HexCell _cell, Vector3 _center, EdgeVertices _e)
    {
        if (_cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(_direction, _cell, _center, _e);
        }

        if (_cell.HasRiverThroughEdge(_direction.Next()))
        {
            if (_cell.HasRiverThroughEdge(_direction.Previous()))
            {
                _center += HexMetrics.GetSolidEdgeMiddle(_direction) * (HexMetrics.innerToOuter * 0.5f);
            }
            else if (_cell.HasRiverThroughEdge(_direction.Previous2()))
            {
                _center += HexMetrics.GetFirstSolidCorner(_direction) * 0.25f;
            }
        }
        else if (_cell.HasRiverThroughEdge(_direction.Previous())
                 && _cell.HasRiverThroughEdge(_direction.Next2()))
        {
            _center += HexMetrics.GetSecondSolidCorner(_direction) * 0.25f;
        }

        var m = new EdgeVertices(Vector3.Lerp(_center, _e.v1, 0.5f), Vector3.Lerp(_center, _e.v5, 0.5f));

        TriangulateEdgeStrip(m, weights1, _cell.Index, _e, weights1, _cell.Index);
        TriangulateEdgeFan(_center, m, _cell.Index);

        if (!_cell.IsUnderwater
            && !_cell.HasRoadThroughEdge(_direction))
        {
            features.AddFeature(_cell, (_center + _e.v1 + _e.v5) * (1.0f / 3.0f));
        }
    }

    void TriangulateRoadAdjacentToRiver(HexDirection _direction, HexCell _cell, Vector3 _center, EdgeVertices _e)
    {
        bool hasRoadThroughEdge = _cell.HasRoadThroughEdge(_direction);
        bool previousHasRiver = _cell.HasRiverThroughEdge(_direction.Previous());
        bool nextHasRiver = _cell.HasRiverThroughEdge(_direction.Next());

        Vector2 interpolators = GetRoadInterpolators(_direction, _cell);

        Vector3 roadCenter = _center;

        if (_cell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(_cell.RiverBeginOrEndDirection.Opposite()) * (1.0f / 3.0f);
        }
        else if (_cell.IncomingRiver == _cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;

            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge
                    && !_cell.HasRoadThroughEdge(_direction.Next()))
                {
                    return;
                }

                corner = HexMetrics.GetSecondSolidCorner(_direction);
            }
            else
            {
                if (!hasRoadThroughEdge
                    && !_cell.HasRoadThroughEdge(_direction.Next()))
                {
                    return;
                }

                corner = HexMetrics.GetFirstSolidCorner(_direction);
            }

            roadCenter += corner * 0.5f;

            if (_cell.IncomingRiver == _direction.Next()
                && (_cell.HasRoadThroughEdge(_direction.Next2())
                    || _cell.HasRoadThroughEdge(_direction.Opposite())))
            {
                features.AddBridge(roadCenter, _center - corner * 0.5f);
            }

            _center += corner * 0.25f;
        }
        else if (_cell.IncomingRiver == _cell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(_cell.IncomingRiver) * 0.2f;
        }
        else if (_cell.IncomingRiver == _cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(_cell.IncomingRiver) * 0.2f;
        }
        else if (previousHasRiver
                 && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
            {
                return;
            }

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(_direction) * HexMetrics.innerToOuter;

            roadCenter += offset * 0.7f;

            _center += offset * 0.5f;
        }
        else
        {
            HexDirection middle;

            if (previousHasRiver)
            {
                middle = _direction.Next();
            }
            else if (nextHasRiver)
            {
                middle = _direction.Previous();
            }
            else
            {
                middle = _direction;
            }

            if (!_cell.HasRoadThroughEdge(middle)
                && !_cell.HasRoadThroughEdge(middle.Previous())
                && !_cell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);

            roadCenter += offset * 0.25f;

            if (_direction == middle
                && _cell.HasRoadThroughEdge(_direction.Opposite()))
            {
                features.AddBridge(roadCenter, _center - offset * (HexMetrics.innerToOuter * 0.7f));
            }
        }

        Vector3 mL = Vector3.Lerp(roadCenter, _e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, _e.v5, interpolators.y);

        TriangulateRoad(roadCenter, mL, mR, _e, hasRoadThroughEdge, _cell.Index);

        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, _center, mL, _cell.Index);
        }

        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, _center, _cell.Index);
        }
    }

    void TriangulateWithRiverBeginOrEnd(HexDirection _direction, HexCell _cell, Vector3 _center, EdgeVertices _e)
    {
        var m = new EdgeVertices(Vector3.Lerp(_center, _e.v1, 0.5f), Vector3.Lerp(_center, _e.v5, 0.5f))
        {
                v3 = {y = _e.v3.y}
        };

        TriangulateEdgeStrip(m, weights1, _cell.Index, _e, weights1, _cell.Index);
        TriangulateEdgeFan(_center, m, _cell.Index);

        if (!_cell.IsUnderwater)
        {
            bool reversed = _cell.HasIncomingRiver;

            Vector3 indices;
            indices.x = indices.y = indices.z = _cell.Index;

            TriangulateRiverQuad(m.v2, m.v4, _e.v2, _e.v4, _cell.RiverSurfaceY, 0.6f, reversed, indices);

            _center.y = m.v2.y = m.v4.y = _cell.RiverSurfaceY;

            rivers.AddTriangle(_center, m.v2, m.v4);

            if (reversed)
            {
                rivers.AddTriangleUv(new Vector2(0.5f, 0.4f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.2f));
            }
            else
            {
                rivers.AddTriangleUv(new Vector2(0.5f, 0.4f), new Vector2(0.0f, 0.6f), new Vector2(1.0f, 0.6f));
            }

            rivers.AddTriangleCellData(indices, weights1);
        }
    }

    void TriangulateWithRiver(HexDirection _direction, HexCell _cell, Vector3 _center, EdgeVertices _e)
    {
        Vector3 centerL;
        Vector3 centerR;

        if (_cell.HasRiverThroughEdge(_direction.Opposite()))
        {
            centerL = _center + HexMetrics.GetFirstSolidCorner(_direction.Previous()) * 0.25f;
            centerR = _center + HexMetrics.GetSecondSolidCorner(_direction.Next()) * 0.25f;
        }
        else if (_cell.HasRiverThroughEdge(_direction.Next()))
        {
            centerL = _center;
            centerR = Vector3.Lerp(_center, _e.v5, 2.0f / 3.0f);
        }
        else if (_cell.HasRiverThroughEdge(_direction.Previous()))
        {
            centerL = Vector3.Lerp(_center, _e.v1, 2.0f / 3.0f);
            centerR = _center;
        }
        else if (_cell.HasRiverThroughEdge(_direction.Next2()))
        {
            centerL = _center;
            centerR = _center + HexMetrics.GetSolidEdgeMiddle(_direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else
        {
            centerL = _center + HexMetrics.GetSolidEdgeMiddle(_direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = _center;
        }

        _center = Vector3.Lerp(centerL, centerR, 0.5f);

        var m = new EdgeVertices(Vector3.Lerp(centerL, _e.v1, 0.5f),
                                 Vector3.Lerp(centerR, _e.v5, 0.5f),
                                 1.0f / 6.0f)
        {
                v3 = {y = _center.y = _e.v3.y}
        };

        TriangulateEdgeStrip(m, weights1, _cell.Index, _e, weights1, _cell.Index);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, _center, m.v2, m.v3);
        terrain.AddQuad(_center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);

        Vector3 indices;
        indices.x = indices.y = indices.z = _cell.Index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);

        if (!_cell.IsUnderwater)
        {
            bool reversed = _cell.IncomingRiver == _direction;

            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, _cell.RiverSurfaceY, 0.4f, reversed, indices);
            TriangulateRiverQuad(m.v2, m.v4, _e.v2, _e.v4, _cell.RiverSurfaceY, 0.6f, reversed, indices);
        }
    }

    void TriangulateWaterfallInWater(Vector3 _v1, Vector3 _v2, Vector3 _v3, Vector3 _v4,
                                     float _y1, float _y2, float _waterY, Vector3 _indices)
    {
        _v1.y = _v2.y = _y1;
        _v3.y = _v4.y = _y2;

        _v1 = HexMetrics.Perturb(_v1);
        _v2 = HexMetrics.Perturb(_v2);
        _v3 = HexMetrics.Perturb(_v3);
        _v4 = HexMetrics.Perturb(_v4);

        float t = (_waterY - _y2) / (_y1 - _y2);
        _v3 = Vector3.Lerp(_v3, _v1, t);
        _v4 = Vector3.Lerp(_v4, _v2, t);

        rivers.AddQuadUnperturbed(_v1, _v2, _v3, _v4);
        rivers.AddQuadUv(0.0f, 1.0f, 0.8f, 1.0f);

        rivers.AddQuadCellData(_indices, weights1, weights2);
    }

    void TriangulateConnection(HexDirection _direction, HexCell _cell, EdgeVertices _e1)
    {
        HexCell neighbor = _cell.GetNeighbor(_direction);

        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(_direction);
        bridge.y = neighbor.Position.y - _cell.Position.y;

        var e2 = new EdgeVertices(_e1.v1 + bridge, _e1.v5 + bridge);

        bool hasRiver = _cell.HasRiverThroughEdge(_direction);
        bool hasRoad = _cell.HasRoadThroughEdge(_direction);

        if (hasRiver)
        {
            e2.v3.y = neighbor.StreamBedY;

            Vector3 indices;
            indices.x = indices.z = _cell.Index;
            indices.y = neighbor.Index;

            if (!_cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(_e1.v2, _e1.v4, e2.v2, e2.v4,
                                         _cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                                         _cell.HasIncomingRiver && _cell.IncomingRiver == _direction, indices);
                }
                else if (_cell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(_e1.v2, _e1.v4, e2.v2, e2.v4,
                                                _cell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY, indices);
                }
            }
            else if (!neighbor.IsUnderwater
                     && neighbor.Elevation > _cell.WaterLevel)
            {
                TriangulateWaterfallInWater(e2.v4, e2.v2, _e1.v4, _e1.v2,
                                            neighbor.RiverSurfaceY, _cell.RiverSurfaceY, _cell.WaterSurfaceY, indices);
            }
        }

        if (_cell.GetEdgeType(_direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(_e1, _cell, e2, neighbor, hasRoad);
        }
        else
        {
            TriangulateEdgeStrip(_e1, weights1, _cell.Index, e2, weights2, neighbor.Index, hasRoad);
        }

        features.AddWall(_e1, _cell, e2, neighbor, hasRiver, hasRoad);

        HexCell nextNeighbor = _cell.GetNeighbor(_direction.Next());

        if (_direction <= HexDirection.E
            && nextNeighbor != null)
        {
            Vector3 v5 = _e1.v5 + HexMetrics.GetBridge(_direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (_cell.Elevation <= neighbor.Elevation)
            {
                if (_cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(_e1.v5, _cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, _e1.v5, _cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, _e1.v5, _cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, _e1.v5, _cell, e2.v5, neighbor);
            }
        }
    }

    void TriangulateCorner(Vector3 _bottom, HexCell _bottomCell,
                           Vector3 _left, HexCell _leftCell,
                           Vector3 _right, HexCell _rightCell)
    {
        HexEdgeType leftEdgeType = _bottomCell.GetEdgeType(_leftCell);
        HexEdgeType rightEdgeType = _bottomCell.GetEdgeType(_rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            switch (rightEdgeType)
            {
                case HexEdgeType.Slope:
                    TriangulateCornerTerraces(_bottom, _bottomCell, _left, _leftCell, _right, _rightCell);

                    break;

                case HexEdgeType.Flat:
                    TriangulateCornerTerraces(_left, _leftCell, _right, _rightCell, _bottom, _bottomCell);

                    break;

                default:
                    TriangulateCornerTerracesCliff(_bottom, _bottomCell, _left, _leftCell, _right, _rightCell);

                    break;
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(_right, _rightCell, _bottom, _bottomCell, _left, _leftCell);
            }
            else
            {
                TriangulateCornerCliffTerraces(_bottom, _bottomCell, _left, _leftCell, _right, _rightCell);
            }
        }
        else if (_leftCell.GetEdgeType(_rightCell) == HexEdgeType.Slope)
        {
            if (_leftCell.Elevation < _rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(_right, _rightCell, _bottom, _bottomCell, _left, _leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(_left, _leftCell, _right, _rightCell, _bottom, _bottomCell);
            }
        }
        else
        {
            terrain.AddTriangle(_bottom, _left, _right);

            Vector3 indices;
            indices.x = _bottomCell.Index;
            indices.y = _leftCell.Index;
            indices.z = _rightCell.Index;
            terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
        }

        features.AddWall(_bottom, _bottomCell, _left, _leftCell, _right, _rightCell);
    }

    void TriangulateEdgeTerraces(EdgeVertices _begin, HexCell _beginCell,
                                 EdgeVertices _end, HexCell _endCell,
                                 bool _hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(_begin, _end, 1);

        Color w2 = HexMetrics.TerraceLerp(weights1, weights2, 1);

        float i1 = _beginCell.Index;
        float i2 = _endCell.Index;

        TriangulateEdgeStrip(_begin, weights1, i1, e2, w2, i2, _hasRoad);

        for (var i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;

            Color w1 = w2;

            e2 = EdgeVertices.TerraceLerp(_begin, _end, i);
           w2 = HexMetrics.TerraceLerp(weights1, weights2, i);

            TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, _hasRoad);
        }

        TriangulateEdgeStrip(e2, w2, i1, _end, weights2, i2, _hasRoad);
    }

    void TriangulateCornerTerraces(Vector3 _begin, HexCell _beginCell,
                                   Vector3 _left, HexCell _leftCell,
                                   Vector3 _right, HexCell _rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(_begin, _left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(_begin, _right, 1);

        Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
        Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);

        Vector3 indices;
        indices.x = _beginCell.Index;
        indices.y = _leftCell.Index;
        indices.z = _rightCell.Index;

        terrain.AddTriangle(_begin, v3, v4);
        terrain.AddTriangleCellData(indices, weights1, w3, w4);

        for (var i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;

            Color w1 = w3;
            Color w2 = w4;

            v3 = HexMetrics.TerraceLerp(_begin, _left, i);
            v4 = HexMetrics.TerraceLerp(_begin, _right, i);
            w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
            w4 = HexMetrics.TerraceLerp(weights1, weights3, i);

            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadCellData(indices, w1, w2, w3, w4);
        }

        terrain.AddQuad(v3, v4, _left, _right);
        terrain.AddQuadCellData(indices, weights1, weights2, w3, w4);
    }

    void TriangulateCornerTerracesCliff(Vector3 _begin, HexCell _beginCell,
                                        Vector3 _left, HexCell _leftCell,
                                        Vector3 _right, HexCell _rightCell)
    {
        float b = 1.0f / (_rightCell.Elevation - _beginCell.Elevation);

        if (b < 0.0f)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(_begin), HexMetrics.Perturb(_right), b);

        Color boundaryWeights = Color.Lerp(weights1, weights3, b);

        Vector3 indices;
        indices.x = _beginCell.Index;
        indices.y = _leftCell.Index;
        indices.z = _rightCell.Index;

        TriangulateBoundaryTriangle(_begin, weights1, _left, weights2, boundary, boundaryWeights, indices);

        if (_leftCell.GetEdgeType(_rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(_left, weights2, _right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(_left), HexMetrics.Perturb(_right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
        }
    }

    void TriangulateCornerCliffTerraces(Vector3 _begin, HexCell _beginCell,
                                        Vector3 _left, HexCell _leftCell,
                                        Vector3 _right, HexCell _rightCell)
    {
        float b = 1.0f / (_leftCell.Elevation - _beginCell.Elevation);

        if (b < 0.0f)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(_begin), HexMetrics.Perturb(_left), b);

        Color boundaryWeights = Color.Lerp(weights1, weights3, b);

        Vector3 indices;
        indices.x = _beginCell.Index;
        indices.y = _leftCell.Index;
        indices.z = _rightCell.Index;

        TriangulateBoundaryTriangle(_right, weights1, _begin, weights2, boundary, boundaryWeights, indices);

        if (_leftCell.GetEdgeType(_rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(_left, weights2, _right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(_left), HexMetrics.Perturb(_right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
        }
    }

    void TriangulateBoundaryTriangle(Vector3 _begin, Color _beginWeights,
                                     Vector3 _left, Color _leftWeights,
                                     Vector3 _boundary, Color _boundaryWeights,
                                     Vector3 _indices)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(_begin, _left, 1));

        Color w2 = HexMetrics.TerraceLerp(_beginWeights, _leftWeights, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(_begin), v2, _boundary);
        terrain.AddTriangleCellData(_indices, _beginWeights, w2, _boundaryWeights);

        for (var i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;

            Color w1 = w2;

            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(_begin, _left, i));
            w2 = HexMetrics.TerraceLerp(_beginWeights, _leftWeights, i);

            terrain.AddTriangleUnperturbed(v1, v2, _boundary);
            terrain.AddTriangleCellData(_indices, w1, w2, _boundaryWeights);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(_left), _boundary);
        terrain.AddTriangleCellData(_indices, w2, _leftWeights, _boundaryWeights);
    }

    void TriangulateEdgeFan(Vector3 _center, EdgeVertices _edge, float _index)
    {
        terrain.AddTriangle(_center, _edge.v1, _edge.v2);
        terrain.AddTriangle(_center, _edge.v2, _edge.v3);
        terrain.AddTriangle(_center, _edge.v3, _edge.v4);
        terrain.AddTriangle(_center, _edge.v4, _edge.v5);

        Vector3 indices;
        indices.x = indices.y = indices.z = _index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    }

    void TriangulateEdgeStrip(EdgeVertices _e1, Color _w1, float _index1,
                              EdgeVertices _e2, Color _w2, float _index2,
                              bool _hasRoad = false)
    {
        terrain.AddQuad(_e1.v1, _e1.v2, _e2.v1, _e2.v2);
        terrain.AddQuad(_e1.v2, _e1.v3, _e2.v2, _e2.v3);
        terrain.AddQuad(_e1.v3, _e1.v4, _e2.v3, _e2.v4);
        terrain.AddQuad(_e1.v4, _e1.v5, _e2.v4, _e2.v5);

        Vector3 indices;
        indices.x = indices.z = _index1;
        indices.y = _index2;
        terrain.AddQuadCellData(indices, weights1, weights2);
        terrain.AddQuadCellData(indices, weights1, weights2);
        terrain.AddQuadCellData(indices, weights1, weights2);
        terrain.AddQuadCellData(indices, weights1, weights2);

        if (_hasRoad)
        {
            TriangulateRoadSegment(_e1.v2, _e1.v3, _e1.v4, _e2.v2, _e2.v3, _e2.v4, _w1, _w2, indices);
        }
    }

    void TriangulateRiverQuad(Vector3 _v1, Vector3 _v2, Vector3 _v3, Vector3 _v4,
                              float _y, float _v, bool _reversed, Vector3 _indices)
    {
        TriangulateRiverQuad(_v1, _v2, _v3, _v4, _y, _y, _v, _reversed, _indices);
    }

    void TriangulateRiverQuad(Vector3 _v1, Vector3 _v2, Vector3 _v3, Vector3 _v4,
                              float _y1, float _y2, float _v, bool _reversed, Vector3 _indices)
    {
        _v1.y = _v2.y = _y1;
        _v3.y = _v4.y = _y2;

        rivers.AddQuad(_v1, _v2, _v3, _v4);

        if (_reversed)
        {
            rivers.AddQuadUv(1.0f, 0.0f, 0.8f - _v, 0.6f - _v);
        }
        else
        {
            rivers.AddQuadUv(0.0f, 1.0f, _v, _v + 0.2f);
        }

        rivers.AddQuadCellData(_indices, weights1, weights2);
    }

    void TriangulateRoad(Vector3 _center, Vector3 _mL, Vector3 _mR, EdgeVertices _e, bool _hasRoadThroughCellEdge, float _index)
    {
        if (_hasRoadThroughCellEdge)
        {
            Vector3 indices;
            indices.x = indices.y = indices.z = _index;
            
            Vector3 mC = Vector3.Lerp(_mL, _mR, 0.5f);

            TriangulateRoadSegment(_mL, mC, _mR, _e.v2, _e.v3, _e.v4, weights1, weights1, indices);

            roads.AddTriangle(_center, _mL, mC);
            roads.AddTriangle(_center, mC, _mR);

            roads.AddTriangleUv(new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f));
            roads.AddTriangleUv(new Vector2(1.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f));

            roads.AddTriangleCellData(indices, weights1);
            roads.AddTriangleCellData(indices, weights1);
        }
        else
        {
            TriangulateRoadEdge(_center, _mL, _mR, _index);
        }
    }

    void TriangulateRoadEdge(Vector3 _center, Vector3 _mL, Vector3 _mR, float _index)
    {
        roads.AddTriangle(_center, _mL, _mR);
        roads.AddTriangleUv(new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f));

        Vector3 indices;
        indices.x = indices.y = indices.z = _index;
        roads.AddTriangleCellData(indices, weights1);
    }

    void TriangulateRoadSegment(Vector3 _v1, Vector3 _v2, Vector3 _v3,
                                Vector3 _v4, Vector3 _v5, Vector3 _v6,
                                Color _w1, Color _w2, Vector3 _indices)
    {
        roads.AddQuad(_v1, _v2, _v4, _v5);
        roads.AddQuad(_v2, _v3, _v5, _v6);

        roads.AddQuadUv(0.0f, 1.0f, 0.0f, 0.0f);
        roads.AddQuadUv(1.0f, 0.0f, 0.0f, 0.0f);

        roads.AddQuadCellData(_indices, _w1, _w2);
        roads.AddQuadCellData(_indices, _w1, _w2);
    }
}