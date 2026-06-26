using System.Collections;

namespace Lodsman.Model;

internal class LinkedHashSet<T>(IEqualityComparer<T>? comparer = null) : IEnumerable<T> where T : notnull
{
    private readonly Dictionary<T, LinkedListNode<T>> _dictionary = new(comparer);
    private readonly LinkedList<T> _linkedList = [];

    public int Count => _linkedList.Count;

    public bool Add(T item)
    {
        if (_dictionary.ContainsKey(item))
            return false;

        var node = _linkedList.AddLast(item);
        _dictionary[item] = node;
        return true;
    }

    public bool Remove(T item)
    {
        if (!_dictionary.Remove(item, out var node))
            return false;

        _linkedList.Remove(node);
        return true;
    }

    public bool Replace(T oldItem, T newItem)
    {
        if (_dictionary.ContainsKey(newItem) ||
            !_dictionary.Remove(oldItem, out var node))
            return false;

        node.Value = newItem;
        _dictionary[newItem] = node;
        return true;
    }

    public void Clear()
    {
        _dictionary.Clear();
        _linkedList.Clear();
    }

    public IEnumerator<T> GetEnumerator() => _linkedList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
