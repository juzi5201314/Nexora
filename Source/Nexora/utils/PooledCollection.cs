using UnityEngine.Pool;

namespace Nexora.utils;

public static class PooledCollection
{
    public static DictPool<TK, TV> Dict<TK, TV>() => new(DictionaryPool<TK, TV>.Get());
    public static ListPool<T> List<T>() => new(UnityEngine.Pool.ListPool<T>.Get());
    
    public class ListPool<T>(List<T> inner) : IDisposable
    {
        public List<T> Inner = inner;
        public void Dispose()
        {
            UnityEngine.Pool.ListPool<T>.Release(Inner);
        }
    }

    public class DictPool<TK, TV>(Dictionary<TK, TV> inner) : IDisposable
    {
        public Dictionary<TK, TV> Inner = inner;
        public void Dispose()
        {
            DictionaryPool<TK, TV>.Release(Inner);
        }
    }
}