using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;

    public RectTransform uiRect;

    public HexGridChunk chunk;

    [SerializeField] private HexCell[] neighbors;

    public HexCell PathFrom { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    public HexUnit Unit { get; set; }

    [SerializeField] private bool[] roads;

    private int elevation = int.MinValue;
    private int waterLevel;
    private int urbanLevel;
    private int farmLevel;
    private int plantLevel;
    private int specialIndex;
    private int terrainTypeIndex;
    private int distance;
    private int visibility;

    public int SearchHeuristic { get; set; }

    private bool walled;
    private bool explored;

    public int Elevation
    {
        get => elevation;
        set
        {
            if (elevation == value)
            {
                return;
            }

            int originalViewElevation = ViewElevation;

            elevation = value;

            if (ViewElevation != originalViewElevation)
            {
                ShaderData.ViewElevationChanged();
            }

            RefreshPosition();

            ValidateRivers();

            for (var i = 0; i < roads.Length; i++)
            {
                if (roads[i]
                    && GetElevationDifference((HexDirection) i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public int TerrainTypeIndex
    {
        get => terrainTypeIndex;
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;

                ShaderData.RefreshTerrain(this);
            }
        }
    }

    public int UrbanLevel
    {
        get => urbanLevel;
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get => farmLevel;
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get => plantLevel;
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public bool Walled
    {
        get => walled;
        set
        {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    public Vector3 Position => transform.localPosition;

    public bool HasIncomingRiver { get; private set; }

    public bool HasOutgoingRiver { get; private set; }

    public HexDirection IncomingRiver { get; private set; }

    public HexDirection OutgoingRiver { get; private set; }

    public bool HasRiver => HasIncomingRiver || HasOutgoingRiver;

    public bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;

    public bool HasRiverThroughEdge(HexDirection _direction) => HasIncomingRiver && IncomingRiver == _direction
                                                                || HasOutgoingRiver && OutgoingRiver == _direction;

    public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

    public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

    public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

    public bool HasRoads => roads.Any(_road => _road);

    public HexDirection RiverBeginOrEndDirection => HasIncomingRiver ? IncomingRiver : OutgoingRiver;
    
    public HexCellShaderData ShaderData { get; set; }

    public int WaterLevel
    {
        get => waterLevel;
        set
        {
            if (waterLevel == value)
            {
                return;
            }

            int originalViewElevation = ViewElevation;

            waterLevel = value;

            if (ViewElevation != originalViewElevation)
            {
                ShaderData.ViewElevationChanged();
            }
            
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater => waterLevel > elevation;

    public int SpecialIndex
    {
        get => specialIndex;
        set
        {
            if (specialIndex != value
                && !HasRiver)
            {
                specialIndex = value;

                RemoveRoads();

                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial => specialIndex > 0;

    public int Distance
    {
        get => distance;
        set => distance = value;
    }

    public int SearchPriority => distance + SearchHeuristic;

    public int SearchPhase { get; set; }
    
    public int Index { get; set; }

    public bool IsVisible => visibility > 0 && Explorable;

    public bool IsExplored
    {
        get => explored && Explorable;
        private set => explored = value;
    }
    
    public bool Explorable { get; set; }

    public int ViewElevation => elevation >= waterLevel ? elevation : waterLevel;
    
    public int ColumnIndex { get; set; }

    public HexCell GetNeighbor(HexDirection _direction) => neighbors[(int) _direction];

    public void SetNeighbor(HexDirection _direction, HexCell _cell)
    {
        neighbors[(int) _direction] = _cell;
        _cell.neighbors[(int) _direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection _direction) =>
            HexMetrics.GetEdgeType(elevation, neighbors[(int) _direction].elevation);

    public HexEdgeType GetEdgeType(HexCell _otherCell) => HexMetrics.GetEdgeType(elevation, _otherCell.elevation);

    public void SetOutgoingRiver(HexDirection _direction)
    {
        if (HasOutgoingRiver
            && OutgoingRiver == _direction)
        {
            return;
        }

        HexCell neighbor = GetNeighbor(_direction);

        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();

        if (HasIncomingRiver
            && IncomingRiver == _direction)
        {
            RemoveIncomingRiver();
        }

        HasOutgoingRiver = true;
        OutgoingRiver = _direction;
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.HasIncomingRiver = true;
        neighbor.IncomingRiver = _direction.Opposite();
        neighbor.specialIndex = 0;

        SetRoad((int) _direction, false);
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void RemoveOutgoingRiver()
    {
        if (!HasOutgoingRiver)
        {
            return;
        }

        HasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(OutgoingRiver);
        neighbor.HasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!HasIncomingRiver)
        {
            return;
        }

        HasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(IncomingRiver);
        neighbor.HasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    private void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();

            foreach (HexCell neighbor in neighbors)
            {
                if (neighbor != null
                    && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }

            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    private void RefreshSelfOnly()
    {
        chunk.Refresh();

        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    public bool HasRoadThroughEdge(HexDirection _direction) => roads[(int) _direction];

    public void AddRoad(HexDirection _direction)
    {
        if (!roads[(int) _direction]
            && !HasRiverThroughEdge(_direction)
            && !IsSpecial
            && !GetNeighbor(_direction).IsSpecial
            && GetElevationDifference(_direction) <= 1)
        {
            SetRoad((int) _direction, true);
        }
    }

    public void RemoveRoads()
    {
        for (var i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    void SetRoad(int _index, bool _state)
    {
        roads[_index] = _state;
        neighbors[_index].roads[(int) ((HexDirection) _index).Opposite()] = _state;
        neighbors[_index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public int GetElevationDifference(HexDirection _direction)
    {
        int difference = elevation - GetNeighbor(_direction).elevation;

        return difference >= 0 ? difference : -difference;
    }

    bool IsValidRiverDestination(HexCell _neighbor) =>
            _neighbor
            && elevation >= _neighbor.elevation
            || waterLevel == _neighbor.elevation;

    void ValidateRivers()
    {
        if (HasOutgoingRiver
            && !IsValidRiverDestination(GetNeighbor(OutgoingRiver)))
        {
            RemoveOutgoingRiver();
        }

        if (HasIncomingRiver
            && !GetNeighbor(IncomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
                (HexMetrics.SampleNoise(position).y * 2.0f - 1.0f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    public void SetLabel(string _text)
    {
        var label = uiRect.GetComponent<Text>();
        label.text = _text;
    }

    public void EnableHighlight(Color _color)
    {
        var highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = _color;
        highlight.enabled = true;
    }

    public void DisableHighlight()
    {
        var highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void IncreaseVisibility()
    {
        visibility += 1;

        if (visibility == 1)
        {
            IsExplored = true;
            
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility()
    {
        visibility -= 1;

        if (visibility == 0)
        {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void ResetVisibility()
    {
        if (visibility > 0)
        {
            visibility = 0;

            ShaderData.RefreshVisibility(this);
        }
    }

    public void SetMapData(float _data)
    {
        ShaderData.SetMapData(this, _data);
    }

    public void Save(BinaryWriter _writer)
    {
        _writer.Write((byte) terrainTypeIndex);
        _writer.Write((byte) (elevation + 127));
        _writer.Write((byte) waterLevel);
        _writer.Write((byte) urbanLevel);
        _writer.Write((byte) farmLevel);
        _writer.Write((byte) plantLevel);
        _writer.Write((byte) specialIndex);
        _writer.Write(walled);

        if (HasIncomingRiver)
        {
            _writer.Write((byte) (IncomingRiver + 128));
        }
        else
        {
            _writer.Write((byte) 0);
        }

        if (HasOutgoingRiver)
        {
            _writer.Write((byte) (OutgoingRiver + 128));
        }
        else
        {
            _writer.Write((byte) 0);
        }

        var roadFlags = 0;

        for (var i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }

        _writer.Write((byte) roadFlags);
        _writer.Write(IsExplored);
    }

    public void Load(BinaryReader _reader, int _header)
    {
        terrainTypeIndex = _reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = _reader.ReadByte();

        if (_header >= 4)
        {
            elevation -= 127;
        }

        RefreshPosition();

        waterLevel = _reader.ReadByte();
        urbanLevel = _reader.ReadByte();
        farmLevel = _reader.ReadByte();
        plantLevel = _reader.ReadByte();
        specialIndex = _reader.ReadByte();
        walled = _reader.ReadBoolean();

        byte riverData = _reader.ReadByte();

        if (riverData >= 128)
        {
            HasIncomingRiver = true;
            IncomingRiver = (HexDirection) (riverData - 128);
        }
        else
        {
            HasIncomingRiver = false;
        }

        riverData = _reader.ReadByte();

        if (riverData >= 128)
        {
            HasOutgoingRiver = true;
            OutgoingRiver = (HexDirection) (riverData - 128);
        }
        else
        {
            HasOutgoingRiver = false;
        }

        int roadFlags = _reader.ReadByte();

        for (var i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = _header >= 3 && _reader.ReadBoolean();

        ShaderData.RefreshVisibility(this);
    }
}