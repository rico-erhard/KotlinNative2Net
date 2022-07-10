using System.Dynamic;
using KotlinNative2Net;
using LanguageExt;
using static LanguageExt.Prelude;
using static System.Console;

static string Hex(IntPtr ptr)
=> BitConverter.ToString(BitConverter.GetBytes(ptr.ToInt64()).Reverse().ToArray());

static void PrintHexed(IntPtr ptr)
=> WriteLine(Hex(ptr));

string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
string apiPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math_api.h";
string sharedLibPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math.dll";

using KLib kLib = (KLib)KLib.Of(apiPath, sharedLibPath);

string mathHeader = File.ReadAllText(apiPath);

KHeader header = kLib.Header;
KStruct symbolsDecl = kLib.Symbols;
KStruct minusDecl = (KStruct)symbolsDecl.FindChild("root.arithmetic.Minus");
KStruct plusDecl = (KStruct)symbolsDecl.FindChild("Plus");

KFunc plusTypeDecl = (KFunc)plusDecl.FindFunc("_type");
KFunc plusCtorDecl = (KFunc)plusDecl.FindFunc("Plus");
KFunc minusTypeDecl = (KFunc)minusDecl.FindFunc("_type");
KFunc isInstanceDecl = (KFunc)symbolsDecl.FindFunc("IsInstance");
KFunc createNullableUnitDecl = (KFunc)symbolsDecl.FindFunc("createNullableUnit");
KFunc disposeStablePointerDecl = (KFunc)symbolsDecl.FindFunc("DisposeStablePointer");

Void_IntPtr plusType = (Void_IntPtr)kLib.GetFunc<Void_IntPtr>(plusTypeDecl);
Void_IntPtr minusType = (Void_IntPtr)kLib.GetFunc<Void_IntPtr>(minusTypeDecl);
PlusCtor plusCtor = (PlusCtor)kLib.GetFunc<PlusCtor>(plusCtorDecl);
MinusCtor minusCtor = (MinusCtor)kLib.GetFunc<MinusCtor>("arithmetic.Minus.Minus");
Ptr_Int addMethod = (Ptr_Int)kLib.GetFunc<Ptr_Int>("arithmetic.Plus.add");
Ptr_Int subtractMethod = (Ptr_Int)kLib.GetFunc<Ptr_Int>("arithmetic.Minus.subtract");

Void_IntPtr createNullableUnit = (Void_IntPtr)kLib.GetFunc<Void_IntPtr>(createNullableUnitDecl);
PtrPtr_Int isInstance = (PtrPtr_Int)kLib.GetFunc<PtrPtr_Int>(isInstanceDecl);
Ptr_Void disposeStablePointer = (Ptr_Void)kLib.GetFunc<Ptr_Void>(disposeStablePointerDecl);

void Dispose(IntPtr kObj)
=> disposeStablePointer(kObj);

bool IsInstance(IntPtr kObj, IntPtr type)
=> 0 != isInstance(kObj, type);


KObj NewKObj(IntPtr handle)
=> new KObj(handle, Dispose);

using KObj plus5 = NewKObj(plusCtor(2, 3));
using KObj plus7 = NewKObj(plusCtor(3, 4));
using KObj plusTypeInst = NewKObj(plusType());
using KObj minusTypeInst = NewKObj(minusType());
using KObj plusTypeInst2 = NewKObj(plusType());
using KObj minus = NewKObj(minusCtor(2, 3));
using KObj unit = NewKObj(createNullableUnit());
using KObj unit2 = NewKObj(createNullableUnit());

PrintHexed(unit);
PrintHexed(unit2);

PrintHexed(plusTypeInst);
PrintHexed(plusTypeInst2);

PrintHexed(plus5);
PrintHexed(plus7);

WriteLine($"plus is plusType = {IsInstance(plus5, plusTypeInst)}");
WriteLine($"plus is minusType = {IsInstance(plus5, minusTypeInst)}");

WriteLine($"minus is minusType = {IsInstance(minus, minusTypeInst)}");
WriteLine($"minus is plusType = {IsInstance(minus, plusTypeInst)}");

WriteLine($"unit is plusType = {IsInstance(unit, plusTypeInst)}");

WriteLine($"2 + 3 = {addMethod(plus5)}");
WriteLine($"3 + 4 = {addMethod(plus7)}");
WriteLine($"2 - 3 = {subtractMethod(minus)}");

dynamic dynPlus7 = new DynKObj(plus7, plusDecl, kLib);
int result = dynPlus7.add<int>();
WriteLine($"Result = {result}");


class DynKObj : DynamicObject
{
    readonly KObj kObj;

    readonly KStruct kStruct;
    readonly KLib kLib;

    public DynKObj(KObj kObj, KStruct kStruct, KLib kLib)
    {
        this.kObj = kObj;
        this.kStruct = kStruct;
        this.kLib = kLib;
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
                kLib.GetFunc<Ptr_Int>(f).Do(d =>
                {
                    localResult = d(kObj);
                    success = true;
                });
            }
        });

        result = localResult;
        return success;
    }

}


delegate IntPtr SymbolsFunc();

delegate IntPtr PlusCtor(int a, int b);

delegate IntPtr MinusCtor(int a, int b);

delegate IntPtr Void_IntPtr();

delegate IntPtr Ptr_Void(IntPtr ptr);

delegate int PtrPtr_Int(IntPtr inst, IntPtr type);

delegate int PtrPtr_Byte(IntPtr inst, IntPtr type);

delegate int Ptr_Int(IntPtr inst);

// https://docs.microsoft.com/en-us/dotnet/api/system.dynamic.dynamicobject?view=net-6.0