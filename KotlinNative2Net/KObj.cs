namespace KotlinNative2Net;

public class KObj : IDisposable
{

    readonly IntPtr handle;

    readonly Action<IntPtr> dispose;

    bool disposed = false;

    public KObj(IntPtr handle, Action<IntPtr> dispose)
    {
        this.handle = handle;
        this.dispose = dispose;
    }

    // Public implementation of Dispose pattern callable by consumers.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            dispose(handle);
        }
        disposed = true;
    }

    public static implicit operator IntPtr(KObj x) => x.handle;
}
