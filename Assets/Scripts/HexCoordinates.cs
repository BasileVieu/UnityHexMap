using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct HexCoordinates
{
    [SerializeField] private int x, z;

    public int X => x;

    public int Z => z;

    public int Y => -X - Z;

    public HexCoordinates(int _x, int _z)
    {
        if (HexMetrics.Wrapping)
        {
            int oX = _x + _z / 2;

            if (oX < 0)
            {
                _x += HexMetrics.wrapSize;
            }
            else if (oX >= HexMetrics.wrapSize)
            {
                _x -= HexMetrics.wrapSize;
            }
        }
        
        x = _x;
        z = _z;
    }

    public static HexCoordinates FromOffsetCoordinates(int _x, int _z) => new HexCoordinates(_x - _z / 2, _z);

    public static HexCoordinates FromPosition(Vector3 _position)
    {
        float x = _position.x / HexMetrics.innerDiameter;
        float y = -x;

        float offset = _position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY
                && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates _other)
    {
        int xy = (x < _other.x ? _other.x - x : x - _other.x) +
                 (Y < _other.Y ? _other.Y - Y : Y - _other.Y);

        if (HexMetrics.Wrapping)
        {
            _other.x += HexMetrics.wrapSize;

            int xyWrapped = (x < _other.x ? _other.x - x : x - _other.x) +
                            (Y < _other.Y ? _other.Y - Y : Y - _other.Y);

            if (xyWrapped < xy)
            {
                xy = xyWrapped;
            }
        }

        return (xy + (z < _other.z ? _other.z - z : z - _other.z)) / 2;
    }

    public override string ToString() => "(" + X + ", " + Y + ", " + Z + ")";

    public string ToStringOnSeparateLines() => X + "\n" + Y + "\n" + Z;

    public void Save(BinaryWriter _writer)
    {
        _writer.Write(x);
        _writer.Write(z);
    }

    public static HexCoordinates Load(BinaryReader _reader)
    {
        HexCoordinates c;

        c.x = _reader.ReadInt32();
        c.z = _reader.ReadInt32();

        return c;
    }
}