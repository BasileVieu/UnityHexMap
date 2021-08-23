using System.Collections.Generic;

public static class ListPool<T>
{
    private static Stack<List<T>> stack = new Stack<List<T>>();

    public static List<T> Get() => stack.Count > 0 ? stack.Pop() : new List<T>();

    public static void Add(List<T> _list)
    {
        _list.Clear();
        stack.Push(_list);
    }
}