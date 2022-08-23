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
        static bool IsPtrInt(KFunc f)
        => 1 == f.Params.Count && f.RetVal.Type.EndsWith("KInt");

        static bool IsPtrVoid(KFunc f)
        => 1 == f.Params.Count && "void" == f.RetVal.Type;

        static bool IsPtrIntInt_Int(KFunc f)
        => 4 == f.Params.Count
        && f.Params[1].Type.EndsWith("void*")
        && f.Params[2].Type.EndsWith("KInt")
        && f.Params[3].Type.EndsWith("KInt")
        && f.RetVal.Type.EndsWith("KInt");

        (object? result, bool success) InvokeKLib(KFunc f, object?[] args)
        {
            if (IsPtrInt(f))
            {
                return kLib.GetFunc<Ptr_Int>(f).Match(d =>
                {
                    object localResult = d(handle);
                    return (localResult, true);
                }, (null, false));
            }
            else if (IsPtrVoid(f))
            {
                return kLib.GetFunc<Ptr_Void>(f)
                .Match<(object?, bool)>(d =>
                {
                    d(handle);
                    return (null, true);
                }, (null, false));
            }
            else if (IsPtrIntInt_Int(f))
            {
                return kLib.GetFunc<PtrPtrIntInt_Int>(f)
                .Match(d =>
                {
                    object localResult = d(handle, (IntPtr)args[0], (int)args[1], (int)args[2]);
                    return (localResult, true);
                }, (null, false));

            }
            return (null, false);
        }

        Option<KFunc> kFunc = binder.Name switch
        {
            "Dispose" => kLib.Symbols.FindFunc("DisposeStablePointer"),
            _ => kStruct.FindFunc(binder.Name)
        };

        (object? tmpResult, bool success) = kFunc
            .Map(f => InvokeKLib(f, args ?? new object?[0]))
            .IfNone((null, false));

        result = tmpResult;
        return success;
    }
}
