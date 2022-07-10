using KotlinNative2Net;
using static System.Console;

string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
string apiPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math_api.h";
string sharedLibPath = Path.GetDirectoryName(assemblyLocation) + Path.DirectorySeparatorChar + "math.dll";

KLib kLib = (KLib)KLib.Of(apiPath, sharedLibPath);
dynamic dynKLib = kLib;

dynamic dynPlus5 = dynKLib.root.arithmetic.Plus.Plus(2, 3);
int dynPlus5Add = dynPlus5.add<int>();
WriteLine($"DynPlus5Add = {dynPlus5Add}");

//dynPlus5.Dispose(); // not implemented
WriteLine("Bye.");