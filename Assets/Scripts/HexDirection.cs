public enum HexDirection
{
    NE,
    E,
    SE,
    SW,
    W,
    NW
}

public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection _direction)
    {
        return (int) _direction < 3 ? _direction + 3 : _direction - 3;
    }

    public static HexDirection Previous(this HexDirection _direction)
    {
        return _direction == HexDirection.NE ? HexDirection.NW : _direction - 1;
    }

    public static HexDirection Previous2(this HexDirection _direction)
    {
        _direction -= 2;

        return _direction >= HexDirection.NE ? _direction : _direction + 6;
    }

    public static HexDirection Next(this HexDirection _direction)
    {
        return _direction == HexDirection.NW ? HexDirection.NE : _direction + 1;
    }

    public static HexDirection Next2(this HexDirection _direction)
    {
        _direction += 2;

        return _direction <= HexDirection.NW ? _direction : _direction - 6;
    }
}