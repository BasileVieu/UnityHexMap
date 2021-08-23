using System.Collections.Generic;
using System.IO;
using System.Collections;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;

    private HexCell location;
    private HexCell currentTravelLocation;

    private List<HexCell> pathToTravel;

    private float orientation;

    private const float travelSpeed = 4.0f;

    private const float rotationSpeed = 180.0f;

    public int VisionRange => 3;

    public HexCell Location
    {
        get => location;
        set
        {
            if (location)
            {
                Grid.DecreaseVisibility(location, VisionRange);
                location.Unit = null;
            }
            
            location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(value, VisionRange);
            transform.localPosition = value.Position;

            Grid.MakeChildOfColumn(transform, value.ColumnIndex);
        }
    }

    public float Orientation
    {
        get => orientation;
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0.0f, value, 0.0f);
        }
    }
    
    public HexGrid Grid { get; set; }

    public int Speed => 24;

    void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;

            if (currentTravelLocation)
            {
                Grid.IncreaseVisibility(location, VisionRange);
                Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                currentTravelLocation = null;
            }
        }
    }

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public void Die()
    {
        if (location)
        {
            Grid.DecreaseVisibility(location, VisionRange);
        }
        
        location.Unit = null;
        Destroy(gameObject);
    }

    public bool IsValidDestination(HexCell _cell) => _cell.IsExplored && !_cell.IsUnderwater && !_cell.Unit;

    IEnumerator LookAt(Vector3 _point)
    {
        if (HexMetrics.Wrapping)
        {
            float xDistance = _point.x - transform.localPosition.x;

            if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                _point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
            }
            else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                _point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
            }
        }
        
        _point.y = transform.localPosition.y;

        Quaternion fromRotation = transform.localRotation;

        Quaternion toRotation = Quaternion.LookRotation(_point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);

        if (angle > 0.0f)
        {
            float speed = rotationSpeed / angle;

            for (float t = Time.deltaTime * speed; t < 1.0f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);

                yield return null;
            }

            transform.LookAt(_point);
            orientation = transform.localRotation.eulerAngles.y;
        }
    }

    public void Travel(List<HexCell> _path)
    {
        location.Unit = null;
        location = _path[_path.Count - 1];
        location.Unit = this;
        pathToTravel = _path;

        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    IEnumerator TravelPath()
    {
        Vector3 a;
        Vector3 b;
        Vector3 c = pathToTravel[0].Position;

        yield return LookAt(pathToTravel[1].Position);

        if (!currentTravelLocation)
        {
            currentTravelLocation = pathToTravel[0];
        }

        Grid.DecreaseVisibility(currentTravelLocation, VisionRange);

        int currentColumn = currentTravelLocation.ColumnIndex;

        float t = Time.deltaTime * travelSpeed;

        for (var i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocation = pathToTravel[i];
            
            a = c;
            b = pathToTravel[i - 1].Position;

            int nextColumn = currentTravelLocation.ColumnIndex;

            if (currentColumn != nextColumn)
            {
                if (nextColumn < currentColumn - 1)
                {
                    a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                else if (nextColumn > currentColumn + 1)
                {
                    a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                
                Grid.MakeChildOfColumn(transform, nextColumn);

                currentColumn = nextColumn;
            }

            c = (b + currentTravelLocation.Position) * 0.5f;

            Grid.IncreaseVisibility(pathToTravel[i], VisionRange);
            
            for (; t < 1.0f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);

                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0.0f;
                transform.localRotation = Quaternion.LookRotation(d);

                yield return null;
            }

            Grid.DecreaseVisibility(pathToTravel[i], VisionRange);

            t -= 1.0f;
        }

        currentTravelLocation = null;

        a = c;
        b = location.Position;
        c = b;

        Grid.IncreaseVisibility(location, VisionRange);

        for (; t < 1.0f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);

            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0.0f;
            transform.localRotation = Quaternion.LookRotation(d);

            yield return null;
        }

        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    public int GetMoveCost(HexCell _fromCell, HexCell _toCell, HexDirection _direction)
    {
        HexEdgeType edgeType = _fromCell.GetEdgeType(_toCell);

        if (edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }

        int moveCost;

        if (_fromCell.HasRoadThroughEdge(_direction))
        {
            moveCost = 1;
        }
        else if (_fromCell.Walled != _toCell.Walled)
        {
            return -1;
        }
        else
        {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += _toCell.UrbanLevel + _toCell.FarmLevel + _toCell.PlantLevel;
        }

        return moveCost;
    }

    public void Save(BinaryWriter _writer)
    {
        location.coordinates.Save(_writer);
        _writer.Write(orientation);
    }

    public static void Load(BinaryReader _reader, HexGrid _grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(_reader);
        float orientation = _reader.ReadSingle();

        _grid.AddUnit(Instantiate(unitPrefab), _grid.GetCell(coordinates), orientation);
    }
}