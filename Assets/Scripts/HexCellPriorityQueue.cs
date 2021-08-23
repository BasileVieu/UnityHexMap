using System.Collections.Generic;

public class HexCellPriorityQueue
{
    List<HexCell> list = new List<HexCell>();

    public int Count { get; private set; }

    private int minimum = int.MaxValue;

    public void Enqueue(HexCell _cell)
    {
        Count += 1;

        int priority = _cell.SearchPriority;

        if (priority < minimum)
        {
            minimum = priority;
        }

        while (priority >= list.Count)
        {
            list.Add(null);
        }

        _cell.NextWithSamePriority = list[priority];
        list[priority] = _cell;
    }

    public HexCell Dequeue()
    {
        Count -= 1;

        for (; minimum < list.Count; minimum++)
        {
            HexCell cell = list[minimum];

            if (cell != null)
            {
                list[minimum] = cell.NextWithSamePriority;

                return cell;
            }
        }

        return null;
    }

    public void Change(HexCell _cell, int _oldPriority)
    {
        HexCell current = list[_oldPriority];
        HexCell next = current.NextWithSamePriority;

        if (current == _cell)
        {
            list[_oldPriority] = next;
        }
        else
        {
            while (next != _cell)
            {
                current = next;
                next = current.NextWithSamePriority;
            }

            current.NextWithSamePriority = _cell.NextWithSamePriority;
        }

        Enqueue(_cell);

        Count -= 1;
    }

    public void Clear()
    {
        list.Clear();

        Count = 0;

        minimum = int.MaxValue;
    }
}