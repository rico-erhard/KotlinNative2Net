using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

public static class Invokers
{
    public static Seq<Invoker> Default = Seq<Invoker>()
        .Add(new VoidCtorInvoker())
        .Add(new IntIntCtorInvoker())
        .Add(new Ptr_IntInvoker())
        .Add(new Ptr_VoidInvoker())
        .Add(new PtrIntInt_IntInvoker());
}

public abstract class Invoker
{
    public abstract bool IsMatch(KFunc func, object[] args);
    public abstract object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args);
}

public class VoidCtorInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib
        .GetFunc<Void_Ptr>(func)
        .Map(f => new KObj(f(), kLib.Thiz, kLib))
        .IfNoneUnsafe(() => null);

    public override bool IsMatch(KFunc func, object[] args)
    => func.Params.IsEmpty && func.RetVal.Type.Contains("_kref_") && 0 == args.Length;
}

public class IntIntCtorInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib
        .GetFunc<IntInt_Ptr>(func)
        .Map(f => new KObj(f((int)args[0], (int)args[1]), kLib.Thiz, kLib))
        .IfNoneUnsafe(() => null);

    public override bool IsMatch(KFunc func, object[] args)
    => func.Params.All(x => x.Type.EndsWith("KInt")) && func.RetVal.Type.Contains("_kref_") && 2 == args.Length;
}

public class Ptr_IntInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib
        .GetFunc<Ptr_Int>(func)
        .Map<int?>(d => d(kObj))
        .IfNoneUnsafe(() => null);

    public override bool IsMatch(KFunc func, object[] args)
    => 1 == func.Params.Count && func.RetVal.Type.EndsWith("KInt");
}

public class Ptr_VoidInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib.GetFunc<Ptr_Void>(func)
        .IfSome(d => d(kObj));

    public override bool IsMatch(KFunc func, object[] args)
    => 1 == func.Params.Count && "void" == func.RetVal.Type;
}

public class PtrIntInt_IntInvoker : Invoker
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

