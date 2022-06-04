using System.Runtime.InteropServices;
using KotlinNative2Net;
using LanguageExt;
using static System.Console;

static string Hex(IntPtr ptr)
=> BitConverter.ToString(BitConverter.GetBytes(ptr.ToInt64()).Reverse().ToArray());

static void PrintHexed(IntPtr ptr)
=> WriteLine(Hex(ptr));

static T GetFunc<T>(IntPtr symbols, int offset)
{
    IntPtr addr = Marshal.ReadIntPtr(symbols + IntPtr.Size * offset);
    return Marshal.GetDelegateForFunctionPointer<T>(addr);
}

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
int plusTypeOffset = (int)findOffset(plusTypeDecl);

KFunc plusCtorDecl = (KFunc)plusDecl.Funcs.Find(x => "Plus" == x.Name);
int plusCtorOffset = (int)findOffset(plusCtorDecl);

KFunc addDecl = (KFunc)plusDecl.Funcs.Find(x => "add" == x.Name);
int addOffset = (int)findOffset(addDecl);

KFunc minusCtorDecl = (KFunc)minusDecl.Funcs.Find(x => "Minus" == x.Name);
int minusCtorOffset = (int)findOffset(minusCtorDecl);

KFunc subtractMethodDecl = (KFunc)minusDecl.Funcs.Find(x => "subtract" == x.Name);
int substractOffset = (int)findOffset(subtractMethodDecl);

KFunc isInstanceDecl = (KFunc)symbolsDecl.Funcs.Find(x => "IsInstance" == x.Name);
int isInstanceOffset = (int)findOffset(isInstanceDecl);

KFunc createNullableUnitDecl = (KFunc)symbolsDecl.Funcs.Find(x => "createNullableUnit" == x.Name);
int createNullableUnitOffset = (int)findOffset(createNullableUnitDecl);

KFunc disposeStablePointerDecl = (KFunc)symbolsDecl.Funcs.Find(x => "DisposeStablePointer" == x.Name);
int disposeStablePointerOffset = (int)findOffset(createNullableUnitDecl);

IntPtr mathLib = NativeLibrary.Load(sharedLibPath);
IntPtr symbolsFuncAddr = NativeLibrary.GetExport(mathLib, header.SymbolsFunc);
SymbolsFunc symbolsFunc = Marshal.GetDelegateForFunctionPointer<SymbolsFunc>(symbolsFuncAddr);
IntPtr symbols = symbolsFunc();

int step = IntPtr.Size;

Void_IntPtr plusType = GetFunc<Void_IntPtr>(symbols, plusTypeOffset);
PlusCtor plusCtor = GetFunc<PlusCtor>(symbols, plusCtorOffset);
MinusCtor minusCtor = GetFunc<MinusCtor>(symbols, minusCtorOffset);
Ptr_Int addMethod = GetFunc<Ptr_Int>(symbols, addOffset);
Ptr_Int subtractMethod = GetFunc<Ptr_Int>(symbols, substractOffset);

Void_IntPtr createNullableUnit = GetFunc<Void_IntPtr>(symbols, createNullableUnitOffset);
PtrPtr_Int isInstance = GetFunc<PtrPtr_Int>(symbols, isInstanceOffset);
Ptr_Void disposeStablePointer = GetFunc<Ptr_Void>(symbols, disposeStablePointerOffset);

PrintHexed(symbols);

IntPtr plus = plusCtor(2, 3);
IntPtr plusTypeInst = plusType();
IntPtr minus = minusCtor(2, 3);

WriteLine(plus);
WriteLine($"plus is plusType = {0 == isInstance(plus, plusTypeInst)}");
WriteLine($"minus is plusType = {0 == isInstance(minus, plusTypeInst)}");

WriteLine($"2 + 3 = {addMethod(plus)}");
WriteLine($"2 - 3 = {subtractMethod(minus)}");

IntPtr plusPinnedAddr = Marshal.ReadIntPtr(plus);
disposeStablePointer(plusPinnedAddr);

IntPtr unit = createNullableUnit();
WriteLine(unit);


delegate IntPtr SymbolsFunc();

delegate IntPtr PlusCtor(int a, int b);

delegate IntPtr MinusCtor(int a, int b);

delegate IntPtr Void_IntPtr();

delegate IntPtr Ptr_Void(IntPtr ptr);

delegate int PtrPtr_Int(IntPtr inst, IntPtr type);

delegate int Ptr_Int(IntPtr inst);

