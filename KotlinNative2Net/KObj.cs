using System.Dynamic;
using LanguageExt;
using static LanguageExt.Prelude;

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

public class PtrIntInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib
        .GetFunc<Ptr_Int>(func)
        .Map<int?>(d => d(kObj))
        .IfNoneUnsafe(() => null);

    public override bool IsMatch(KFunc func, object[] args)
    => 1 == func.Params.Count && func.RetVal.Type.EndsWith("KInt");
}

public class PtrVoidInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib.GetFunc<Ptr_Void>(func)
        .IfSome(d => d(kObj));

    public override bool IsMatch(KFunc func, object[] args)
    => 1 == func.Params.Count && "void" == func.RetVal.Type;
}

public class PtrIntIntIntInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib
        .GetFunc<PtrPtrIntInt_Int>(func)
        .Map<int?>(d => d(kObj, (IntPtr)args[0], (int)args[1], (int)args[2]))
        .IfNoneUnsafe(() => null);

    public override bool IsMatch(KFunc func, object[] args)
    => 4 == func.Params.Count
    && func.Params[1].Type.EndsWith("void*")
    && func.Params[2].Type.EndsWith("KInt")
    && func.Params[3].Type.EndsWith("KInt")
    && func.RetVal.Type.EndsWith("KInt");
}
