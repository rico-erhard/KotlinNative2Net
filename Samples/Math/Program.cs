using System.Runtime.InteropServices;
using KotlinNative2Net;
using LanguageExt;
using static System.Console;

static string Hex(IntPtr ptr)
=> BitConverter.ToString(BitConverter.GetBytes(ptr.ToInt64()).Reverse().ToArray());

static void PrintHexed(IntPtr ptr)
=> WriteLine(Hex(ptr));

static T GetFuncAtOffset<T>(IntPtr symbols, int offset)
{
    IntPtr addr = Marshal.ReadIntPtr(symbols + IntPtr.Size * offset);
    return Marshal.GetDelegateForFunctionPointer<T>(addr);
}

static Option<T> GetFuncFromDecl<T>(IntPtr symbols, KStruct symbolsDecl, KFunc f)
=> symbolsDecl.FindOffset(f)
    .Map(x => GetFuncAtOffset<T>(symbols, x));

string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
string apiPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math_api.h";
string sharedLibPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math.dll";

string mathHeader = File.ReadAllText(apiPath);

KHeader header = (KHeader)Parser.ParseHeader(mathHeader);
KStruct symbolsDecl = (KStruct)header.Childs.Find(x => x.Name == header.SymbolsType);
KStruct minusDecl = (KStruct)symbolsDecl.FindChild("Minus");
KStruct plusDecl = (KStruct)symbolsDecl.FindChild("Plus");


Func<KFunc, Option<int>> findOffset =
f => symbolsDecl.FindOffset(f);

KFunc plusTypeDecl = (KFunc)plusDecl.Funcs.Find(x => "_type" == x.Name);
KFunc plusCtorDecl = (KFunc)plusDecl.Funcs.Find(x => "Plus" == x.Name);
KFunc addDecl = (KFunc)plusDecl.Funcs.Find(x => "add" == x.Name);
KFunc minusCtorDecl = (KFunc)minusDecl.Funcs.Find(x => "Minus" == x.Name);
KFunc minusTypeDecl = (KFunc)minusDecl.Funcs.Find(x => "_type" == x.Name);
KFunc subtractMethodDecl = (KFunc)minusDecl.Funcs.Find(x => "subtract" == x.Name);
KFunc isInstanceDecl = (KFunc)symbolsDecl.Funcs.Find(x => "IsInstance" == x.Name);
KFunc createNullableUnitDecl = (KFunc)symbolsDecl.Funcs.Find(x => "createNullableUnit" == x.Name);
KFunc disposeStablePointerDecl = (KFunc)symbolsDecl.Funcs.Find(x => "DisposeStablePointer" == x.Name);


IntPtr mathLib = NativeLibrary.Load(sharedLibPath);
IntPtr symbolsFuncAddr = NativeLibrary.GetExport(mathLib, header.SymbolsFunc);
SymbolsFunc symbolsFunc = Marshal.GetDelegateForFunctionPointer<SymbolsFunc>(symbolsFuncAddr);
IntPtr symbols = symbolsFunc();

int step = IntPtr.Size;

T GetFunc<T>(KFunc f)
=> (T)GetFuncFromDecl<T>(symbols, symbolsDecl, f);

Void_IntPtr plusType = GetFunc<Void_IntPtr>(plusTypeDecl);
PlusCtor plusCtor = GetFunc<PlusCtor>(plusCtorDecl);
MinusCtor minusCtor = GetFunc<MinusCtor>(minusCtorDecl);
Ptr_Int addMethod = GetFunc<Ptr_Int>(addDecl);
Ptr_Int subtractMethod = GetFunc<Ptr_Int>(subtractMethodDecl);

Void_IntPtr createNullableUnit = GetFunc<Void_IntPtr>(createNullableUnitDecl);
PtrPtr_Int isInstance = GetFunc<PtrPtr_Int>(isInstanceDecl);
Ptr_Void disposeStablePointer = GetFunc<Ptr_Void>(disposeStablePointerDecl);

void Dispose(IntPtr kObj)
=> disposeStablePointer(kObj);

bool IsInstance(IntPtr kObj, IntPtr type)
=> 0 != isInstance(kObj, type);

PrintHexed(symbols);

IntPtr plus5 = plusCtor(2, 3);
IntPtr plus7 = plusCtor(3, 4);
IntPtr plusTypeInst = plusType();
IntPtr plusTypeInst2 = plusType();
IntPtr minus = minusCtor(2, 3);
IntPtr unit = createNullableUnit();
IntPtr unit2 = createNullableUnit();

PrintHexed(unit);
PrintHexed(unit2);

PrintHexed(plusTypeInst);
PrintHexed(plusTypeInst2);

PrintHexed(plus5);
PrintHexed(plus7);

WriteLine($"plus is plusType = {IsInstance(plus5, plusTypeInst)}");
WriteLine($"minus is plusType = {IsInstance(minus, plusTypeInst)}");
WriteLine($"unit is plusType = {IsInstance(unit, plusTypeInst)}");

WriteLine($"2 + 3 = {addMethod(plus5)}");
WriteLine($"2 - 3 = {subtractMethod(minus)}");

Dispose(plus5);
Dispose(plus7);
Dispose(plusTypeInst);
Dispose(plusTypeInst2);
Dispose(minus);
Dispose(unit);
Dispose(unit2);


delegate IntPtr SymbolsFunc();

delegate IntPtr PlusCtor(int a, int b);

delegate IntPtr MinusCtor(int a, int b);

delegate IntPtr Void_IntPtr();

delegate IntPtr Ptr_Void(IntPtr ptr);

delegate int PtrPtr_Int(IntPtr inst, IntPtr type);

delegate int PtrPtr_Byte(IntPtr inst, IntPtr type);

delegate int Ptr_Int(IntPtr inst);

