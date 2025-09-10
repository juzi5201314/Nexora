namespace Nexora.utils;

public static class LockExtensions
{
    public static ReaderWriterLockSlim NewRwLock() => new(LockRecursionPolicy.SupportsRecursion);
    public static IDisposable Read(this ReaderWriterLockSlim rwLock) => new ReadLock(rwLock);
    public static IDisposable Write(this ReaderWriterLockSlim rwLock) => new WriteLock(rwLock);
    public static IDisposable UpgradeableRead(this ReaderWriterLockSlim rwLock) => new UpgradeableReadLock(rwLock);

    private class ReadLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        public ReadLock(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock.EnterReadLock();
        }

        public void Dispose() => _rwLock.ExitReadLock();
    }

    private class WriteLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        public WriteLock(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock.EnterWriteLock();
        }

        public void Dispose() => _rwLock.ExitWriteLock();
    }

    private class UpgradeableReadLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        public UpgradeableReadLock(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock.EnterUpgradeableReadLock();
        }

        public void Dispose() => _rwLock.ExitUpgradeableReadLock();
    }
}