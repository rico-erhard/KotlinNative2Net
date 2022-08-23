using System.Runtime.InteropServices;
using KotlinNative2Net;
using static System.Console;

unsafe
{
    string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
    string apiPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math_api.h";
    string sharedLibPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math.dll";

    using KLib kLib = (KLib)KLib.Of(apiPath, sharedLibPath);
    dynamic dynKLib = kLib;

    dynamic dynPlus = dynKLib.root.arithmetic.Plus.Plus(2, 3);
    int five = dynPlus.add<int>();
    WriteLine($"2 + 3 = {five}");

    dynamic dynMinus = dynKLib.root.arithmetic.Minus.Minus(2, 3);
    int minusOne = dynMinus.subtract<int>();
    WriteLine($"2 - 3 = {minusOne}");

    dynamic dynCallback = dynKLib.root.arithmetic.Callback.Callback();
    delegate* unmanaged<int, int, int> netAdd = &NetAdd;
    int result = dynCallback.call<int>((IntPtr)netAdd, 2, 3);

    dynPlus.Dispose();
    dynMinus.Dispose();
    dynCallback.Dispose();
    WriteLine("Bye.");

    [UnmanagedCallersOnly]
    static int NetAdd(int a, int b)
    {
        WriteLine($"Add {a} and {b} in .NET.");
        return a + b;
    }
}