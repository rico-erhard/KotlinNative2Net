using System.Runtime.InteropServices;
using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

delegate IntPtr Void_Ptr();

public class KLib : IDisposable
{

    readonly IntPtr libHandle;

    readonly IntPtr symbolsHandle;

    public readonly KHeader Header;

    public readonly KStruct Symbols;

    bool disposed = false;

    public KLib(IntPtr libHandle, IntPtr symbolsHandle, KHeader header, KStruct symbols)
    {
        this.libHandle = libHandle;
        this.symbolsHandle = symbolsHandle;
        Header = header;
        Symbols = symbols;
    }

    public static Option<KLib> Of(string apiPath, string sharedLibPath)
    => Try(() =>
    {
        string headerText = File.ReadAllText(apiPath);
        KHeader header = (KHeader)Parser.ParseHeader(headerText);
        string symbolsFuncName = header.SymbolsFunc;

        IntPtr libHandle = NativeLibrary.Load(sharedLibPath);
        IntPtr symbolsFuncAddr = NativeLibrary.GetExport(libHandle, symbolsFuncName);
        KStruct symbolsDecl = (KStruct)header.Childs.Find(x => x.Name == header.SymbolsType);

        Void_Ptr symbolsFunc = Marshal.GetDelegateForFunctionPointer<Void_Ptr>(symbolsFuncAddr);
        IntPtr symbolsHandle = symbolsFunc();
        KStruct symbols = (KStruct)header.Childs.Find(x => x.Name == header.SymbolsType);

        return new KLib(libHandle, symbolsHandle, header, symbols);
    }).ToOption();

    static Option<T> GetFuncAtOffset<T>(IntPtr symbols, int offset)
    => Try<T>(() =>
    {
        IntPtr addr = Marshal.ReadIntPtr(symbols + IntPtr.Size * offset);
        return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }).ToOption();

    static Option<T> GetFuncFromDecl<T>(IntPtr symbols, KStruct symbolsDecl, KFunc f)
    => symbolsDecl.FindOffset(f)
        .Bind(x => GetFuncAtOffset<T>(symbols, x));

    public Option<T> GetFunc<T>(KFunc f)
    => GetFuncFromDecl<T>(symbolsHandle, Symbols, f);

    public Option<T> GetFunc<T>(string fullName)
    => Symbols.FindFunc(fullName).Bind(f => GetFunc<T>(f));

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
            NativeLibrary.Free(libHandle);
        }
        disposed = true;
    }
}
