using System.Dynamic;
using LanguageExt;

namespace KotlinNative2Net;

class KObj : DynamicObject
{

    readonly IntPtr handle;

    readonly KStruct kStruct;

    GetFunc getFunc;

    internal KObj(IntPtr handle, KStruct kStruct, GetFunc getFunc)
    {
        this.handle = handle;
        this.kStruct = kStruct;
        this.getFunc = getFunc;
    }

    public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        static bool IsPtrInt(KFunc f)
        => 1 == f.Params.Count && f.RetVal.Type == "math_KInt";

        bool success = false;
        object? localResult = null;

        Option<KFunc> func = kStruct.FindFunc(binder.Name);
        func.Do(f =>
        {
            if (IsPtrInt(f))
            {
                getFunc.Get<Ptr_Int>(f).Do(d =>
                {
                    localResult = d(handle);
                    success = true;
                });
            }
        });

        result = localResult;
        return success;
    }
}
