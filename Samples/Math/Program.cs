using System.Runtime.InteropServices;
using KotlinNative2Net;

string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
string apiPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math_api.h";
string sharedLibPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math.dll";

string headerText = File.ReadAllText(apiPath);

KHeader header  = (KHeader)Parser.ParseHeader(headerText);

KStruct symbols = Parser.ParseStructs(headerText).First();
KStruct kotlin = symbols.Childs.Head;
KStruct root = kotlin.Childs.Head;
KStruct arithmetic = root.Childs.Head;
KStruct minus = arithmetic.Childs[0];
KStruct plus = arithmetic.Childs[1];

IntPtr mathLib = NativeLibrary.Load(sharedLibPath);

KFunc plusCtor = (KFunc)plus.Funcs.Find(x => "Plus" == x.Name);
int plusCtorOffset = (int)symbols.FindOffset(plusCtor);

Console.WriteLine(plusCtorOffset);
//Marshal.GetDelegateForFunctionPointer()