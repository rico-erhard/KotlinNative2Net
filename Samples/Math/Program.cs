using System.Runtime.InteropServices;
using KotlinNative2Net;
using static System.Console;
using static System.Math;

unsafe
{
    string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
    string apiPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math_api.h";
    string sharedLibPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math.dll";

    using KLib kLib = (KLib)KLib.Of(apiPath, sharedLibPath, Invokers.Default.Add(new PtrDoubleDouble_DoubleInvoker()));
    dynamic dynKLib = kLib;

    dynamic dynPlus = dynKLib.root.arithmetic.Plus.Plus(2, 3);
    int five = dynPlus.add<int>();
    WriteLine($"2 + 3 = {five}");

    dynamic dynMinus = dynKLib.root.arithmetic.Minus.Minus(2, 3);
    int minusOne = dynMinus.subtract<int>();
    WriteLine($"2 - 3 = {minusOne}");

    dynamic dynCallback = dynKLib.root.arithmetic.Callback.Callback();
    delegate* unmanaged<int, int, int> netAdd = &NetAdd;
    int netAddResult = dynCallback.call<int>((IntPtr)netAdd, 2, 3);
    WriteLine($"2 + 3 = {netAddResult}");

    dynamic dynFloatPlus = dynKLib.root.arithmetic.FloatPlus.FloatPlus();
    double pi = dynFloatPlus.add(PI - 1, 1d);
    WriteLine($"(π - 1) + 1 = {pi}");

    dynPlus.Dispose();
    dynMinus.Dispose();
    dynCallback.Dispose();
    dynFloatPlus.Dispose();
    WriteLine("Bye.");

    [UnmanagedCallersOnly]
    static int NetAdd(int a, int b)
    {
        WriteLine($"Add {a} and {b} in .NET.");
        return a + b;
    }
}


delegate double PtrDoubleDouble_Double(IntPtr kObj, double a, double b);

class PtrDoubleDouble_DoubleInvoker : Invoker
{
    public override object? Invoke(KLib kLib, KFunc func, IntPtr kObj, object?[] args)
    => kLib
        .GetFunc<PtrDoubleDouble_Double>(func)
        .Map<double?>(d => d(kObj, (double)args[0], (double)args[1]))
        .IfNoneUnsafe(() => null);

    public override bool IsMatch(KFunc func, object[] args)
    => 2 == args.Length
        && func.Params.Skip(1).All(x => x.Type.EndsWith("KDouble"))
        && func.RetVal.Type.EndsWith("KDouble");
}