using System.Collections;

namespace Nexora.utils.pooled;

public class PooledList<T> : IList<T>, IDisposable
{
    private readonly ThreadLocal<PooledArray<List<T>>> _pool = new(() => new PooledArray<List<T>>(16));

    private bool disposed = false;

    public List<T> Inner { get; }

    public PooledList(int cap)
    {
        Inner = _pool.Value.Count > 0 ? _pool.Value.Pop()! : new List<T>(cap);
    }

    public PooledList() : this(64)
    {
    }

    public int Count => Inner.Count;
    public bool IsReadOnly => false;

    public void Release()
    {
        Inner.Clear();
        _pool.Value.Push(Inner);
    }

    public IEnumerator<T> GetEnumerator() => Inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item) => Inner.Add(item);

    public void Clear() => Inner.Clear();

    public bool Contains(T item) => Inner.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);

    public bool Remove(T item) => Inner.Remove(item);

    public int IndexOf(T item) => Inner.IndexOf(item);

    public void Insert(int index, T item) => Inner.Insert(index, item);

    public void RemoveAt(int index) => Inner.RemoveAt(index);

    public T this[int index]
    {
        get => Inner[index];
        set => Inner[index] = value;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Release();
    }
}