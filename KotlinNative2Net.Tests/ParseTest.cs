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
}