using System.Buffers;
using System.Collections;

namespace Nexora.utils.pooled;

public class PooledArray<T>(int cap): IDisposable
{
    public T[] Buffer { get; private set; } = ArrayPool<T>.Shared.Rent(cap);

    public int Index { get; private set; } = 0;
    public int Count => Index;

    public void Push(T item)
    {
        if (Index >= Buffer.Length)
        {
            Resize();
        }

        Buffer[Index++] = item;
    }

    public bool TryPop(out T? item)
    {
        if (Index <= 0)
        {
            item = default;
            return false;
        }

        item = Buffer[--Index];
        return true;
    }

    public T? Pop()
    {
        return Index <= 0 ? default : Buffer[--Index];
    }

    private void Resize()
    {
        var newBuffer = ArrayPool<T>.Shared.Rent(Buffer.Length * 2);
        Array.Copy(Buffer, newBuffer, Buffer.Length);
        ArrayPool<T>.Shared.Return(Buffer);
        Buffer = newBuffer;
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(Buffer);
        GC.SuppressFinalize(this);
    }
}