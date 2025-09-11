using System.Collections;

namespace Nexora.utils.pooled
{
    public class PooledHashSet<T> : ISet<T>, IDisposable
    {
        private readonly ThreadLocal<PooledArray<HashSet<T>>> _pool = new(() => new PooledArray<HashSet<T>>(16));

        private readonly HashSet<T> inner;
        private bool disposed = false;

        public PooledHashSet(int capacity = 64)
        {
            inner = _pool.Value.Count > 0 ? _pool.Value.Pop()! : new HashSet<T>(capacity);
        }

        public int Count => inner.Count;
        public bool IsReadOnly => false;

        public void Release()
        {
            inner.Clear();
            _pool.Value.Push(inner);
        }

        public bool Remove(T item)
        {
            return inner.Remove(item);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(T item)
        {
            return inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Add(T item)
        {
            return inner.Add(item);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            inner.UnionWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            inner.IntersectWith(other);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            inner.ExceptWith(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            inner.SymmetricExceptWith(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return inner.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return inner.IsSupersetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return inner.IsProperSupersetOf(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return inner.IsProperSubsetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return inner.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return inner.SetEquals(other);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Release();
        }
    }
}