using System.Dynamic;
using System.Runtime.InteropServices;
using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

public class KLib : DynamicObject, IDisposable
{

    readonly IntPtr libHandle;

    readonly IntPtr symbolsHandle;

    public readonly KHeader Header;

    public readonly KStruct Symbols;

    public readonly KStruct Thiz;

    bool disposed = false;

    public KLib(IntPtr libHandle, IntPtr symbolsHandle, KHeader header, KStruct symbols, KStruct thiz)
    {
        this.libHandle = libHandle;
        this.symbolsHandle = symbolsHandle;
        Header = header;
        Symbols = symbols;
        Thiz = thiz;
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

        return new KLib(libHandle, symbolsHandle, header, symbols, symbols);
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

    public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        object? go(KFunc func)
        {
            object? tmpResult = null;

            if (args is not null && args.Length == func.Params.Length)
            {
                if (func.Params.IsEmpty && func.RetVal.Type.Contains("_kref_") && 0 == args.Length)
                {
                    GetFunc<Void_Ptr>(func).Do(f =>
                    {
                        IntPtr kPtr = f();
                        tmpResult = new KObj(kPtr, Thiz, this);
                    });
                }
                else if (func.Params.All(x => x.Type.EndsWith("KInt")) && func.RetVal.Type.Contains("_kref_") && 2 == args.Length)
                {
                    GetFunc<IntInt_Ptr>(func).Do(f =>
                    {
                        IntPtr kPtr = f((int)args[0], (int)args[1]);
                        tmpResult = new KObj(kPtr, Thiz, this);
                    });
                }
            }
            return tmpResult;
        }

        Option<KFunc> func = Symbols.FindFunc(binder.Name);
        result = func.Map(go).IfNoneUnsafe(() => null);
        return result is not null;
    }

    public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object? result)
    {
        Option<KLib> tmpResult = Symbols.FindChild(binder.Name)
            .Map(c => new KLib(libHandle, symbolsHandle, Header, Symbols, c));
        result = tmpResult.IfNoneUnsafe(() => null);
        return null != result;
    }
}
