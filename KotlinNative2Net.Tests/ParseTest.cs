using Xunit;
using static Xunit.Assert;
using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;
using System.Collections.Generic;

namespace KotlinNative2Net.Tests;

public class ParseTest
{
    const string mathHeader = @"
#ifndef KONAN_MATH_H
#define KONAN_MATH_H
#ifdef __cplusplus
extern ""C"" {
#endif
#ifdef __cplusplus
typedef bool            math_KBoolean;
#else
typedef _Bool           math_KBoolean;
#endif
typedef unsigned short     math_KChar;
typedef signed char        math_KByte;
typedef short              math_KShort;
typedef int                math_KInt;
typedef long long          math_KLong;
typedef unsigned char      math_KUByte;
typedef unsigned short     math_KUShort;
typedef unsigned int       math_KUInt;
typedef unsigned long long math_KULong;
typedef float              math_KFloat;
typedef double             math_KDouble;
#ifndef _MSC_VER
typedef float __attribute__ ((__vector_size__ (16))) math_KVector128;
#else
#include <xmmintrin.h>
typedef __m128 math_KVector128;
#endif
typedef void*              math_KNativePtr;
struct math_KType;
typedef struct math_KType math_KType;

typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Byte;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Short;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Int;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Long;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Float;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Double;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Char;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Boolean;
typedef struct {
  math_KNativePtr pinned;
} math_kref_kotlin_Unit;
typedef struct {
  math_KNativePtr pinned;
} math_kref_arithmetic_Minus;
typedef struct {
  math_KNativePtr pinned;
} math_kref_arithmetic_Plus;


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
extern math_ExportedSymbols* math_symbols(void);
#ifdef __cplusplus
}  /* extern ""C"" */
#endif
#endif  /* KONAN_MATH_H */
";

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
} math_ExportedSymbols;";

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
        string export = (string)Parser.GetExport(externLine);
        Equal("math_symbols", export);
    }

    [Fact]
    public void CountNumberOfServiceFunctions()
    {
        int count = Parser.NumberOfServiceFunctions(mathSymbols);
        Equal(11, count);
    }

    [Fact]
    public void GetFuncNameTest()
    {
        string funcDeclaration = @"void (*DisposeStablePointer)(math_KNativePtr ptr);";
        string funcName = (string)Parser.GetFuncName(funcDeclaration);
        Equal("DisposeStablePointer", funcName);
    }

    [Fact]
    public void GetFuncNameTestOfLineWithPtrReturnType()
    {
        string funcDeclaration = @"math_KType* (*_type)(void);";
        string funcName = (string)Parser.GetFuncName(funcDeclaration);
        Equal("_type", funcName);
    }


    [Fact]
    public void ParseKStructsName()
    {
        KStruct symbols = Parser.ParseStructs(mathSymbols).First();
        Equal("math_ExportedSymbols", symbols.Name);
    }

    [Fact]
    public void ParseTwoStructs()
    {
        Seq<KStruct> symbols = Parser.ParseStructs(twoFuncStructs);
        Equal(2, symbols.Count);
    }

    [Fact]
    public void ParseKStructs()
    {

        Seq<KStruct> declaration = Parser.ParseStructs(mathSymbols);
        KStruct symbols = declaration.First();
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
        Match match = Parser.ParseFunctions(oneFunc);
        Equal(2, match.Groups[6].Captures.Count);
    }

    [Fact]
    public void ParseOneFunctionSignatureParams()
    {
        string oneFunc = @"math_kref_arithmetic_Plus (*Plus)(math_KInt a, math_KInt b);";
        KFunc func = (KFunc)Parser.ParseSignature(oneFunc);
        Equal("Plus", func.Name);
        Equal(2, func.Params.Count);
        Equal("a", func.Params[0].Name);
        Equal("math_KInt", func.Params[0].Type);
        Equal("b", func.Params[1].Name);
    }

    [Fact]
    public void ParseOneFunctionSignatureRetVal()
    {
        string oneFunc = @"math_kref_arithmetic_Plus (*Plus)(math_KInt a, math_KInt b);";
        KFunc func = (KFunc)Parser.ParseSignature(oneFunc);
        Equal("math_kref_arithmetic_Plus", func.RetVal.Type);
    }

    [Fact]
    public void ParseFunctionWithoutParameters()
    {
        string oneFunc = @"math_KType* (*_type)(void);";
        Match match = Parser.ParseFunctions(oneFunc);
        Single(match.Captures);
    }

    [Fact]
    public void ParseThreeFunctions()
    {
        Match match = Parser.ParseFunctions(minusFunctions);
        Equal(3, match.Groups[3].Captures.Count);
    }

    [Fact]
    public void ParseServiceFunctions()
    {
        Match match = Parser.ParseFunctions(serviceFunctions);
        Equal(12, match.Groups[3].Captures.Count);
    }

    (KStruct, KStruct, KStruct, KStruct, KStruct, KStruct)
        ParseMathSymbols()
    {
        KStruct symbols = Parser.ParseStructs(mathSymbols).First();
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
            KStruct minus, KStruct plus) = ParseMathSymbols();

        Equal(12, symbols.Funcs.Count);
        Equal(3, minus.Funcs.Count);
        Equal(3, plus.Funcs.Count);
        True(plus.Funcs.Map(x => x.Name).Contains("add"));
    }

    [Fact]
    public void ParseKStructsWithFunctionNames2()
    {
        KHeader header = (KHeader)Parser.ParseHeader(mathHeader);
        KStruct symbols = (KStruct)header.Childs.Find(x => x.Name == header.SymbolsType);
        Equal(12, symbols.Funcs.Count);

        KStruct minus = (KStruct)symbols.FindChild("Minus");
        Equal(3, minus.Funcs.Count);

        KStruct plus = (KStruct)symbols.FindChild("Plus");
        Equal(3, plus.Funcs.Count);
        Equal(3, plus.Funcs.Count);
        True(plus.Funcs.Map(x => x.Name).Contains("add"));
    }

    [Fact]
    public void ParseAndCheckParameters()
    {
        (KStruct symbols, KStruct kotlin, KStruct root, KStruct arithmetic,
            KStruct minus, KStruct plus) = ParseMathSymbols();

        Equal(3, plus.Funcs.Count);
        KFunc ctor = (KFunc)plus.Funcs.Find(x => "Plus" == x.Name);
        Equal(2, ctor.Params.Count);

        // math_KType* (*_type)(void);
        KFunc getType = (KFunc)plus.Funcs.Find(x => "_type" == x.Name);
        Equal(0, getType.Params.Count);

        //void (*DisposeString)(const char* string);
        KFunc disposeString = (KFunc)symbols.Funcs.Find(x => "DisposeString" == x.Name);
        Equal("void", disposeString.RetVal.Type);
    }

    [Fact]
    public void ParseAndCheckOffset()
    {
        (KStruct symbols, KStruct kotlin, KStruct root, KStruct arithmetic,
            KStruct minus, KStruct plus) = ParseMathSymbols();

        KFunc disposeString = (KFunc)symbols.Funcs.Find(x => "DisposeString" == x.Name);
        Equal("void", disposeString.RetVal.Type);

        Equal(1, (int)symbols.FindOffset(disposeString));

        KFunc plusCtor = (KFunc)plus.Funcs.Find(x => "Plus" == x.Name);
        Equal(16, (int)symbols.FindOffset(plusCtor));

        KFunc add = (KFunc)plus.Funcs.Find(x => "add" == x.Name);
        Equal(17, (int)symbols.FindOffset(add));

        KFunc plusType = (KFunc)plus.Funcs.Find(x => "_type" == x.Name);
        Equal(15, (int)symbols.FindOffset(plusType));

        KFunc minusType = (KFunc)minus.Funcs.Find(x => "_type" == x.Name);
        Equal(12, (int)symbols.FindOffset(minusType));
    }

    [Fact]
    public void ParseHeader()
    {
        //Equal(1, typedefMatches.Count);
        //KHeader decl = (KHeader)Parser.ParseHeader(mathHeader);
        //Equal("math_symbols", decl.Init);
    }
}