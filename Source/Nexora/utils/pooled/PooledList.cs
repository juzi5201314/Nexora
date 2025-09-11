using System.Collections;

namespace Nexora.utils.pooled;

public class PooledList<T> : IList<T>, IDisposable
{
    private readonly ThreadLocal<PooledArray<List<T>>> _pool = new(() => new PooledArray<List<T>>(16));

    private readonly List<T> inner;
    private bool disposed = false;

    public PooledList(int cap)
    {
        inner = _pool.Value.Count > 0 ? _pool.Value.Pop()! : new List<T>(cap);
    }

    public PooledList() : this(64)
    {
    }

    public int Count => inner.Count;
    public bool IsReadOnly => false;

    public void Release()
    {
        inner.Clear();
        _pool.Value.Push(inner);
    }

    public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item) => inner.Add(item);

    public void Clear() => inner.Clear();

    public bool Contains(T item) => inner.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => inner.CopyTo(array, arrayIndex);

    public bool Remove(T item) => inner.Remove(item);

    public int IndexOf(T item) => inner.IndexOf(item);

    public void Insert(int index, T item) => inner.Insert(index, item);

    public void RemoveAt(int index) => inner.RemoveAt(index);

    public T this[int index]
    {
        get => inner[index];
        set => inner[index] = value;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Release();
    }
}