using Xunit;
using static Xunit.Assert;
using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;
using System.Collections.Generic;

namespace KotlinNative2Net.Tests;

public class ParseTest
{
    const string mathSymbols = @"
typedef struct {
  /* Service functions. */
  void (*DisposeStablePointer)(math_KNativePtr ptr);
  void (*DisposeString)(const char* string);
  math_KBoolean (*IsInstance)(math_KNativePtr ref, const math_KType* type);
  math_kref_kotlin_Byte (*createNullableByte)(math_KByte);
  math_kref_kotlin_Short (*createNullableShort)(math_KShort);
  math_kref_kotlin_Int (*createNullableInt)(math_KInt);
  math_kref_kotlin_Long (*createNullableLong)(math_KLong);
  math_kref_kotlin_Float (*createNullableFloat)(math_KFloat);
  math_kref_kotlin_Double (*createNullableDouble)(math_KDouble);
  math_kref_kotlin_Char (*createNullableChar)(math_KChar);
  math_kref_kotlin_Boolean (*createNullableBoolean)(math_KBoolean);
  math_kref_kotlin_Unit (*createNullableUnit)(void);

  /* User functions. */
  struct {
    struct {
      struct {
        struct {
          math_KType* (*_type)(void);
          math_kref_arithmetic_Minus (*Minus)(math_KInt a, math_KInt b);
          math_KInt (*subtract)(math_kref_arithmetic_Minus thiz);
        } Minus;
        struct {
          math_KType* (*_type)(void);
          math_kref_arithmetic_Plus (*Plus)(math_KInt a, math_KInt b);
          math_KInt (*add)(math_kref_arithmetic_Plus thiz);
        } Plus;
      } arithmetic;
    } root;
  } kotlin;
} math_ExportedSymbols;
";

    const string minusStruct = @"
struct {
  math_KType* (*_type)(void);
  math_kref_arithmetic_Minus (*Minus)(math_KInt a, math_KInt b);
  math_KInt (*subtract)(math_kref_arithmetic_Minus thiz);
} Minus;
";

    const string minusFunctions = @"
math_KType* (*_type)(void);
math_kref_arithmetic_Minus (*Minus)(math_KInt a, math_KInt b);
math_KInt (*subtract)(math_kref_arithmetic_Minus thiz);
";

    const string twoFuncStructs = @"
struct {
  math_KType* (*_type)(void);
  math_kref_arithmetic_Minus (*Minus)(math_KInt a, math_KInt b);
  math_KInt (*subtract)(math_kref_arithmetic_Minus thiz);
} Minus;
struct {
  math_KType* (*_type)(void);
  math_kref_arithmetic_Plus (*Plus)(math_KInt a, math_KInt b);
  math_KInt (*add)(math_kref_arithmetic_Plus thiz);
} Plus;
";

    const string serviceFunctions = @"
/* Service functions. */
void (*DisposeStablePointer)(math_KNativePtr ptr);
void (*DisposeString)(const char* string);
math_KBoolean (*IsInstance)(math_KNativePtr ref, const math_KType* type);
math_kref_kotlin_Byte (*createNullableByte)(math_KByte);
math_kref_kotlin_Short (*createNullableShort)(math_KShort);
math_kref_kotlin_Int (*createNullableInt)(math_KInt);
math_kref_kotlin_Long (*createNullableLong)(math_KLong);
math_kref_kotlin_Float (*createNullableFloat)(math_KFloat);
math_kref_kotlin_Double (*createNullableDouble)(math_KDouble);
math_kref_kotlin_Char (*createNullableChar)(math_KChar);
math_kref_kotlin_Boolean (*createNullableBoolean)(math_KBoolean);
math_kref_kotlin_Unit (*createNullableUnit)(void);
";

    const string externLine = @"extern math_ExportedSymbols* math_symbols(void);";

    [Fact]
    public void FindExport()
    {
        string export = (string)Declaration.GetExport(externLine);
        Equal("math_symbols", export);
    }

    [Fact]
    public void CountNumberOfServiceFunctions()
    {
        int count = Declaration.NumberOfServiceFunctions(mathSymbols);
        Equal(11, count);
    }

    [Fact]
    public void GetFuncNameTest()
    {
        string funcDeclaration = @"void (*DisposeStablePointer)(math_KNativePtr ptr);";
        string funcName = (string)Declaration.GetFuncName(funcDeclaration);
        Equal("DisposeStablePointer", funcName);
    }

    [Fact]
    public void GetFuncNameTestOfLineWithPtrReturnType()
    {
        string funcDeclaration = @"math_KType* (*_type)(void);";
        string funcName = (string)Declaration.GetFuncName(funcDeclaration);
        Equal("_type", funcName);
    }


    [Fact]
    public void ParseKStructsName()
    {
        KStruct symbols = Declaration.Parse(mathSymbols).First();
        Equal("math_ExportedSymbols", symbols.Name);
    }

    [Fact]
    public void ParseTwoStructs()
    {
        Seq<KStruct> symbols = Declaration.Parse(twoFuncStructs);
        Equal(2, symbols.Count);
    }

    [Fact]
    public void ParseKStructs()
    {
        KStruct symbols = Declaration.Parse(mathSymbols).First();
        KStruct kotlin = symbols.Childs.Head;
        KStruct root = kotlin.Childs.Head;
        KStruct arithmetic = root.Childs.Head;
        KStruct minus = arithmetic.Childs[0];
        KStruct plus = arithmetic.Childs[1];

        Equal("math_ExportedSymbols", symbols.Name);
        Equal("kotlin", kotlin.Name);
        Equal("root", root.Name);
        Equal("arithmetic", arithmetic.Name);
        Equal("Plus", plus.Name);
        Equal("Minus", minus.Name);

        Equal(1, symbols.Childs.Count);
        Equal(2, arithmetic.Childs.Count);
    }

    [Fact]
    public void ParseOneFunction()
    {
        string oneFunc = @"math_kref_arithmetic_Plus (*Plus)(math_KInt a, math_KInt b);";
        Match match = Declaration.ParseFunctions(oneFunc);
        Equal(2, match.Groups[6].Captures.Count);
    }

    [Fact]
    public void ParseOneFunctionSignature()
    {
        string oneFunc = @"math_kref_arithmetic_Plus (*Plus)(math_KInt a, math_KInt b);";
        KFunc func = (KFunc)Declaration.ParseSignature(oneFunc);
        Equal("Plus", func.Name);
        Equal(2, func.Params.Count);
        Equal("a", func.Params[0].Name);
    }

    [Fact]
    public void ParseFunctionWithoutParameters()
    {
        string oneFunc = @"math_KType* (*_type)(void);";
        Match match = Declaration.ParseFunctions(oneFunc);
        Single(match.Captures);
    }

    [Fact]
    public void ParseThreeFunctions()
    {
        Match match = Declaration.ParseFunctions(minusFunctions);
        Equal(3, match.Groups[3].Captures.Count);
    }

    [Fact]
    public void ParseServiceFunctions()
    {
        Match match = Declaration.ParseFunctions(serviceFunctions);
        Equal(12, match.Groups[3].Captures.Count);
    }

    (KStruct, KStruct, KStruct, KStruct, KStruct, KStruct)
        ParseMathSymbols()
    {
        KStruct symbols = Declaration.Parse(mathSymbols).First();
        KStruct kotlin = symbols.Childs.Head;
        KStruct root = kotlin.Childs.Head;
        KStruct arithmetic = root.Childs.Head;
        KStruct minus = arithmetic.Childs[0];
        KStruct plus = arithmetic.Childs[1];
        return (symbols, kotlin, root, arithmetic, minus, plus);
    }

    [Fact]
    public void ParseKStructsWithFunctionNames()
    {
        (KStruct symbols, KStruct kotlin, KStruct root, KStruct arithmetic,
            KStruct minus, KStruct plus)= ParseMathSymbols();

        Equal(12, symbols.Funcs.Count);
        Equal(3, minus.Funcs.Count);
        Equal(3, plus.Funcs.Count);
        True(plus.Funcs.Map(x => x.Name).Contains("add"));
    }

    //[Fact]
    public void ParseAndCheckParameters()
    {
        (KStruct symbols, KStruct kotlin, KStruct root, KStruct arithmetic,
            KStruct minus, KStruct plus)= ParseMathSymbols();

        Equal(3, plus.Funcs.Count);
        KFunc ctor = (KFunc)plus.Funcs.Find(x => "Plus" == x.Name);
        Equal(2, ctor.Params.Count);

    }
}