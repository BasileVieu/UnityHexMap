using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class HexGrid : MonoBehaviour
{
    private int chunkCountX;
    private int chunkCountZ;

    private int currentCenterColumnIndex = -1;

    public int cellCountX = 20;
    public int cellCountZ = 15;

    public bool wrapping;

    public HexCell cellPrefab;
    private HexCell currentPathFrom;
    private HexCell currentPathTo;

    public Text cellLabelPrefab;

    public HexGridChunk chunkPrefab;

    public HexUnit unitPrefab;

    public Texture2D noiseSource;

    private HexCell[] cells;

    private HexGridChunk[] chunks;

    private HexCellPriorityQueue searchFrontier;

    public int seed;

    private int searchFrontierPhase;

    private const int visionRange = 3;

    private bool currentPathExists;

    List<HexUnit> units = new List<HexUnit>();

    private HexCellShaderData cellShaderData;

    private Transform[] columns;

    public bool HasPath => currentPathExists;

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        cellShaderData.Grid = this;

        CreateMap(cellCountX, cellCountZ, wrapping);
    }

    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;

            ResetVisibility();
        }
    }

    public bool CreateMap(int _x, int _z, bool _wrapping)
    {
        if (_x <= 0
            || _x % HexMetrics.chunkSizeX != 0
            || _z <= 0
            || _z % HexMetrics.chunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size.");

            return false;
        }

        ClearPath();
        ClearUnits();

        if (columns != null)
        {
            foreach (Transform column in columns)
            {
                Destroy(column.gameObject);
            }
        }

        cellCountX = _x;
        cellCountZ = _z;

        wrapping = _wrapping;

        currentCenterColumnIndex = -1;

        HexMetrics.wrapSize = wrapping ? cellCountX : 0;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        return true;
    }

    private void CreateChunks()
    {
        columns = new Transform[chunkCountX];

        for (var x = 0; x < chunkCountX; x++)
        {
            columns[x] = new GameObject("Column").transform;
            columns[x].SetParent(transform, false);
        }
        
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (var x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(columns[x], false);
            }
        }
    }

    private void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (var x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    public HexCell GetCell(Vector3 _position)
    {
        _position = transform.InverseTransformPoint(_position);

        HexCoordinates coordinates = HexCoordinates.FromPosition(_position);

        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;

        return cells[index];
    }

    public HexCell GetCell(HexCoordinates _coordinates)
    {
        int z = _coordinates.Z;

        if (z < 0
            || z >= cellCountZ)
        {
            return null;
        }

        int x = _coordinates.X + z / 2;

        if (x < 0
            || x >= cellCountX)
        {
            return null;
        }

        return cells[x + z * cellCountX];
    }

    private void CreateCell(int _x, int _z, int _i)
    {
        Vector3 position;
        position.x = (_x + _z * 0.5f - _z / 2) * HexMetrics.innerDiameter;
        position.y = 0f;
        position.z = _z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[_i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(_x, _z);
        cell.Index = _i;
        cell.ColumnIndex = _x / HexMetrics.chunkSizeX;
        cell.ShaderData = cellShaderData;

        if (wrapping)
        {
            cell.Explorable = _z > 0 && _z < cellCountZ - 1;
        }
        else
        {
            cell.Explorable = _x > 0 && _z > 0 && _x < cellCountX - 1 && _z < cellCountZ - 1;
        }

        if (_x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[_i - 1]);

            if (wrapping
                && _x == cellCountX - 1)
            {
                cell.SetNeighbor(HexDirection.E, cells[_i - _x]);
            }
        }

        if (_z > 0)
        {
            if ((_z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[_i - cellCountX]);

                if (_x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[_i - cellCountX - 1]);
                }
                else if (wrapping)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[_i - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[_i - cellCountX]);

                if (_x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[_i - cellCountX + 1]);
                }
                else if (wrapping)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[_i - cellCountX * 2 + 1]);
                }
            }
        }

        Text label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(_x, _z, cell);
    }

    private void AddCellToChunk(int _x, int _z, HexCell _cell)
    {
        int chunkX = _x / HexMetrics.chunkSizeX;
        int chunkZ = _z / HexMetrics.chunkSizeZ;

        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = _x - chunkX * HexMetrics.chunkSizeX;
        int localZ = _z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, _cell);
    }

    public void ShowUI(bool _visible)
    {
        foreach (HexGridChunk chunk in chunks)
        {
            chunk.ShowUI(_visible);
        }
    }

    public HexCell GetCell(Ray _ray) => Physics.Raycast(_ray, out RaycastHit hit) ? GetCell(hit.point) : null;

    public HexCell GetCell(int _xOffset, int _zOffset) => cells[_xOffset + _zOffset * cellCountX];

    public HexCell GetCell(int _cellIndex) => cells[_cellIndex];

    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }

        List<HexCell> path = ListPool<HexCell>.Get();

        for (HexCell cell = currentPathTo; cell != currentPathFrom; cell = cell.PathFrom)
        {
            path.Add(cell);
        }

        path.Add(currentPathFrom);

        path.Reverse();

        return path;
    }

    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;

            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }

            current.DisableHighlight();
            currentPathExists = false;
        }
        else if (currentPathFrom)
        {
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }

        currentPathFrom = currentPathTo = null;
    }

    void ShowPath(int _speed)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;

            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / _speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }

        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    public void FindPath(HexCell _fromCell, HexCell _toCell, HexUnit _unit)
    {
        ClearPath();

        currentPathFrom = _fromCell;
        currentPathTo = _toCell;
        currentPathExists = Search(_fromCell, _toCell, _unit);

        ShowPath(_unit.Speed);
    }

    bool Search(HexCell _fromCell, HexCell _toCell, HexUnit _unit)
    {
        int speed = _unit.Speed;

        searchFrontierPhase += 2;

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        _fromCell.SearchPhase = searchFrontierPhase;
        _fromCell.Distance = 0;

        searchFrontier.Enqueue(_fromCell);

        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == _toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor == null
                    || neighbor.SearchPhase > searchFrontierPhase)
                {
                    continue;
                }

                if (!_unit.IsValidDestination(neighbor))
                {
                    continue;
                }

                int moveCost = _unit.GetMoveCost(current, neighbor, d);

                if (moveCost < 0)
                {
                    continue;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;

                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(_toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return false;
    }

    List<HexCell> GetVisibleCells(HexCell _fromCell, int _range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        _range += _fromCell.ViewElevation;

        _fromCell.SearchPhase = searchFrontierPhase;
        _fromCell.Distance = 0;

        searchFrontier.Enqueue(_fromCell);

        HexCoordinates fromCoordinates = _fromCell.coordinates;

        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            visibleCells.Add(current);

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor == null
                    || neighbor.SearchPhase > searchFrontierPhase
                    || !neighbor.Explorable)
                {
                    continue;
                }

                int distance = current.Distance + 1;

                if (distance + neighbor.ViewElevation > _range
                    || distance > fromCoordinates.DistanceTo(neighbor.coordinates))
                {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return visibleCells;
    }

    public void MakeChildOfColumn(Transform _child, int _columnIndex)
    {
        _child.SetParent(columns[_columnIndex], false);
    }

    public void AddUnit(HexUnit _unit, HexCell _location, float _orientation)
    {
        units.Add(_unit);
        _unit.Grid = this;
        _unit.Location = _location;
        _unit.Orientation = _orientation;
    }

    public void RemoveUnit(HexUnit _unit)
    {
        units.Remove(_unit);
        _unit.Die();
    }

    void ClearUnits()
    {
        foreach (HexUnit unit in units)
        {
            unit.Die();
        }

        units.Clear();
    }

    public void IncreaseVisibility(HexCell _fromCell, int _range)
    {
        List<HexCell> tempCells = GetVisibleCells(_fromCell, _range);

        foreach (HexCell cell in tempCells)
        {
            cell.IncreaseVisibility();
        }

        ListPool<HexCell>.Add(tempCells);
    }

    public void DecreaseVisibility(HexCell _fromCell, int _range)
    {
        List<HexCell> tempCells = GetVisibleCells(_fromCell, _range);

        foreach (HexCell cell in tempCells)
        {
            cell.DecreaseVisibility();
        }

        ListPool<HexCell>.Add(tempCells);
    }

    public void ResetVisibility()
    {
        foreach (HexCell cell in cells)
        {
            cell.ResetVisibility();
        }

        foreach (HexUnit unit in units)
        {
            HexUnit tempUnit = unit;

            IncreaseVisibility(tempUnit.Location, tempUnit.VisionRange);
        }
    }

    public void CenterMap(float _xPosition)
    {
        var centerColumnIndex = (int) (_xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));

        if (centerColumnIndex == currentCenterColumnIndex)
        {
            return;
        }

        currentCenterColumnIndex = centerColumnIndex;

        int minColumnIndex = centerColumnIndex - chunkCountX / 2;
        int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

        Vector3 position;
        position.y = position.z = 0.0f;

        for (var i = 0; i < columns.Length; i++)
        {
            if (i < minColumnIndex)
            {
                position.x = chunkCountX * (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else if (i > maxColumnIndex)
            {
                position.x = chunkCountX * -(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else
            {
                position.x = 0.0f;
            }

            columns[i].localPosition = position;
        }
    }

    public void Save(BinaryWriter _writer)
    {
        _writer.Write(cellCountX);
        _writer.Write(cellCountZ);
        _writer.Write(wrapping);

        foreach (HexCell cell in cells)
        {
            cell.Save(_writer);
        }

        _writer.Write(units.Count);

        foreach (HexUnit unit in units)
        {
            unit.Save(_writer);
        }
    }

    public void Load(BinaryReader _reader, int _header)
    {
        ClearPath();
        ClearUnits();

        var x = 20;
        var z = 15;

        if (_header >= 1)
        {
            x = _reader.ReadInt32();
            z = _reader.ReadInt32();
        }

        bool tempWrapping = _header >= 5 && _reader.ReadBoolean();

        if (x != cellCountX
            || z != cellCountZ
            || wrapping != tempWrapping)
        {
            if (!CreateMap(x, z, wrapping))
            {
                return;
            }
        }

        bool originalImmediateMode = cellShaderData.ImmediateMode;

        cellShaderData.ImmediateMode = true;

        foreach (HexCell cell in cells)
        {
            cell.Load(_reader, _header);
        }

        foreach (HexGridChunk chunk in chunks)
        {
            chunk.Refresh();
        }

        if (_header >= 2)
        {
            int unitCount = _reader.ReadInt32();

            for (var i = 0; i < unitCount; i++)
            {
                HexUnit.Load(_reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
    }
}