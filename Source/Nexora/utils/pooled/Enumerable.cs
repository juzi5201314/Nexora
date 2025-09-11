namespace Nexora.utils.pooled;

public static class Enumerable
{
    public static PooledList<T> ToPooledList<T>(this IEnumerable<T> source)
    {
        var pooled = new PooledList<T>();
        foreach (var item in source)
            pooled.Add(item);
        return pooled;
    }
}