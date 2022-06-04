
using System.Runtime.InteropServices;
using KotlinNative2Net;

static string Hex(IntPtr ptr)
=> BitConverter.ToString(BitConverter.GetBytes(ptr.ToInt64()).Reverse().ToArray());

static void PrintHexed(IntPtr ptr)
=> Console.WriteLine(Hex(ptr));

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

KFunc plusTypeDecl = (KFunc)plusDecl.Funcs.Find(x => "_type" == x.Name);
int plusTypeOffset = (int)symbolsDecl.FindOffset(plusTypeDecl);

KFunc plusCtorDecl = (KFunc)plusDecl.Funcs.Find(x => "Plus" == x.Name);
int plusCtorOffset = (int)symbolsDecl.FindOffset(plusCtorDecl);

KFunc isInstanceDecl = (KFunc)symbolsDecl.Funcs.Find(x => "IsInstance" == x.Name);
int isInstanceOffset = (int)symbolsDecl.FindOffset(isInstanceDecl);

KFunc createNullableUnitDecl = (KFunc)symbolsDecl.Funcs.Find(x => "createNullableUnit" == x.Name);
int createNullableUnitOffset = (int)symbolsDecl.FindOffset(createNullableUnitDecl);

KFunc disposeStablePointerDecl = (KFunc)symbolsDecl.Funcs.Find(x => "DisposeStablePointer" == x.Name);
int disposeStablePointerOffset = (int)symbolsDecl.FindOffset(createNullableUnitDecl);

IntPtr mathLib = NativeLibrary.Load(sharedLibPath);
IntPtr symbolsFuncAddr = NativeLibrary.GetExport(mathLib, header.SymbolsFunc);
SymbolsFunc symbolsFunc = Marshal.GetDelegateForFunctionPointer<SymbolsFunc>(symbolsFuncAddr);
IntPtr symbols = symbolsFunc();

int step = IntPtr.Size;

IntPtr plusTypeAddr = Marshal.ReadIntPtr(symbols + step * plusTypeOffset);
Void_IntPtr plusType = Marshal.GetDelegateForFunctionPointer<Void_IntPtr>(plusTypeAddr);

IntPtr plusAddr = Marshal.ReadIntPtr(symbols + step * plusCtorOffset);
PlusCtor plusCtor = Marshal.GetDelegateForFunctionPointer<PlusCtor>(plusAddr);

IntPtr createNullableUnitAddr = Marshal.ReadIntPtr(symbols + step * createNullableUnitOffset);
Void_IntPtr createNullableUnit = Marshal.GetDelegateForFunctionPointer<Void_IntPtr>(createNullableUnitAddr);

IntPtr isInstanceAddr = Marshal.ReadIntPtr(symbols + step * isInstanceOffset);
PtrPtr_Int isInstance = Marshal.GetDelegateForFunctionPointer<PtrPtr_Int>(isInstanceAddr);

IntPtr disposeStablePointerAddr = Marshal.ReadIntPtr(symbols + step * disposeStablePointerOffset);
Ptr_Void disposeStablePointer = Marshal.GetDelegateForFunctionPointer<Ptr_Void>(disposeStablePointerAddr);

PrintHexed(symbols);
PrintHexed(plusAddr);
PrintHexed(createNullableUnitAddr);
PrintHexed(disposeStablePointerAddr);

IntPtr plus = plusCtor(2, 3);
IntPtr plusTypeInst = plusType();

int x = isInstance(plus, plusTypeInst);
Console.WriteLine(x);
Console.WriteLine(plus);

IntPtr plusPinnedAddr = Marshal.ReadIntPtr(plus);
disposeStablePointer(plusPinnedAddr);
IntPtr unit = createNullableUnit();
Console.WriteLine(unit);


delegate IntPtr SymbolsFunc();

delegate IntPtr PlusCtor(int a, int b);

delegate IntPtr Void_IntPtr();

delegate IntPtr Ptr_Void(IntPtr ptr);

delegate int PtrPtr_Int(IntPtr inst, IntPtr type);

