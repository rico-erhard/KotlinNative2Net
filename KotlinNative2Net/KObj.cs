using System.Dynamic;
using LanguageExt;

namespace KotlinNative2Net;

class KObj : DynamicObject
{

    readonly IntPtr handle;

    readonly KStruct kStruct;

    readonly KLib kLib;

    internal KObj(IntPtr handle, KStruct kStruct, KLib kLib)
    {
        this.handle = handle;
        this.kStruct = kStruct;
        this.kLib = kLib;
    }

    public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        (object? result, bool success) InvokeKLib(KFunc f, object?[] args)
        => kLib.Invokers
            .Filter(x => x.IsMatch(f, args))
            .HeadOrNone()
            .Match(
                x => (x.Invoke(kLib, f, handle, args), true),
                () => (null, false));

        Option<KFunc> kFunc = binder.Name switch
        {
            "Dispose" => kLib.Symbols.FindFunc("DisposeStablePointer"),
            _ => kStruct.FindFunc(binder.Name)
        };

        (object? tmpResult, bool success) = kFunc
            .Map(f => InvokeKLib(f, args ?? new object?[0]))
            .IfNoneUnsafe(() => (null, false));

        result = tmpResult;
        return success;
    }
}

