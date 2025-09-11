using System.Collections;

namespace Nexora.utils.pooled
{
    public class PooledDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
        where TKey : notnull
    {
        private readonly ThreadLocal<PooledArray<Dictionary<TKey, TValue>>> _pool = new(() => new PooledArray<Dictionary<TKey, TValue>>(16));
        
        private readonly Dictionary<TKey, TValue> inner;
        private bool disposed = false;

        public PooledDictionary(int capacity = 64)
        {
            inner = _pool.Value.Count > 0 ? _pool.Value.Pop()! : new Dictionary<TKey, TValue>(capacity);
        }
        
        public PooledDictionary(IEqualityComparer<TKey> comparer, int capacity = 64)
        {
            inner = _pool.Value.Count > 0 ? _pool.Value.Pop()! : new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public ICollection<TKey> Keys => inner.Keys;
        public ICollection<TValue> Values => inner.Values;
        public int Count => inner.Count;
        public bool IsReadOnly => false;
        
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => inner.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => inner.Values;

        public TValue this[TKey key]
        {
            get => inner[key];
            set => inner[key] = value;
        }

        public void Release()
        {
            inner.Clear();
            _pool.Value.Push(inner);
        }

        public void Add(TKey key, TValue value)
        {
            inner.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return inner.Remove(key);
        }

        public bool ContainsKey(TKey key)
        {
            return inner.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return inner.TryGetValue(key, out value);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)inner).Add(item);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)inner).Remove(item);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)inner).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)inner).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Release();
        }
    }
}
