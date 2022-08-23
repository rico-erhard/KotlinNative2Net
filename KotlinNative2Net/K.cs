using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

public record KParam(string Type, string Name);

public record KFunc(string FullName, string Name, Seq<KParam> Params, KParam RetVal);

public record KStruct(string FullName, string Name, Seq<KFunc> Funcs, Seq<KStruct> Childs);

public record KHeader(string SymbolsType, string SymbolsFunc, Seq<KStruct> Childs);

public static class KStructEx
{
    static Seq<KStruct> FlattenChilds(this KStruct s)
    {
        static Seq<KStruct> go(KStruct start, Seq<KStruct> acc)
        {
            return start.Childs.Fold(acc.Append(start.Childs), (acc, next) => go(next, acc));
        }
        return go(s, Seq<KStruct>().Add(s));
    }

    static Seq<int> Offsets(this KStruct s)
    {
        static Seq<int> go(KStruct start, Seq<int> acc)
        {
            int nextOffset = acc.LastOrDefault() + start.Funcs.Length;
            return start.Childs.Fold(
                acc.Add(nextOffset),
                (acc, next) => go(next, acc));
        }
        return go(s, Seq<int>().Add(0));
    }

    public static Option<int> FindOffset(this KStruct s, KFunc func)
    {
        Seq<KStruct> childs = FlattenChilds(s);
        Seq<int> offsets = Offsets(s);
        Seq<(KStruct parent, int offset)> childsAndOffsets = childs.Zip(offsets);
        Option<KStruct> parent = childs.Find(x => x.Funcs.Contains(func));
        return childsAndOffsets
            .Find(t => t.parent == parent)
            .Map(t => t.offset + System.Array.IndexOf(t.parent.Funcs.ToArray(), func));
    }

    public static Option<KStruct> FindChild(this KStruct s, string name)
    => s.FlattenChilds().Find(x => x.FullName.EndsWith(name));

    public static Option<KFunc> FindFunc(this KStruct s, string name)
    => s.FlattenChilds().Bind(x => x.Funcs).Find(x => x.FullName.EndsWith(name));
}

public static class Parser
{

    public static Option<string> GetExport(string declaration)
    {
        string pattern = @"^\s*extern [^*]+\* ([a-z_]+)\(void\)";
        return LastGroup(declaration, pattern);
    }

    static Option<string> LastGroup(string declaration, string pattern)
    {
        Match match = Regex.Match(declaration, pattern, RegexOptions.Multiline);
        return LastGroup(match);
    }

    static Option<string> LastGroup(Match match)
    {
        Seq<Group> groups = match.Groups.ToSeq();
        return groups.Map(x => x.ToString()).LastOrNone();
    }

    public static Option<string> GetFuncName(string line)
    {
        string pattern = @"^\s*([^ ]+) \(\*([^)]+)\).*$";
        return LastGroup(line, pattern);
    }

    public static int NumberOfServiceFunctions(string mathSymbols)
    {
        return 11;
    }

    const string commentPattern = @"\s*(?:/\*[^*]*\*/)*\s*";

    const string functionsPattern = "(" + commentPattern + @"\s*(\w+\*?)\s+\(\*(\w+)\)\((\s*(\w+\*?)\s*(\w+)?,?)*\);\s*" + ")*";

    const string structStart = @"(\s*(typedef )?\s*struct\s+{" + commentPattern + functionsPattern + commentPattern;

    public static Match ParseFunctions(string text)
    {
        Match match = Regex.Match(text, functionsPattern);
        return match;
    }

    public static Option<KFunc> ParseSignature(string funcSignature)
    {
        Match match = Regex.Match(funcSignature, functionsPattern);
        Option<string> retValTypes = match.Groups[2].Captures.ToSeq().Map(x => x.ToString()).HeadOrNone();
        Option<string> funcName = match.Groups[3].Captures.ToSeq().Map(x => x.ToString()).HeadOrNone();
        Seq<string> paramTypes = match.Groups[5].Captures.ToSeq().Map(x => x.ToString());
        Seq<string> paramNames = match.Groups[6].Captures.ToSeq().Map(x => x.ToString());
        return from name in funcName
               from retValType in retValTypes
               select new KFunc(name, name,
                   paramTypes.Zip(paramNames).Map(t => new KParam(t.Left, t.Right)),
                   new KParam(retValType, "RetVal"));
    }

    static Seq<KStruct> ParseStructsWithShortNames(string text)
    {
        static Match ParseFull(string text)
        {
            const string pattern = structStart + @"(.*)} ([^ ]+);\s*)+";
            Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
            return match;
        }

        static Match ParseSequence(string text)
        {
            //const string pattern = structStart + @"([^}]*)} ([^ ]+);\s+)+";
            const string pattern = structStart + @"(.*?)} ([^ ]+);\s*)+";
            Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
            return match;
        }

        const int nameGroup = 10;

        static Seq<KStruct> go(Match match)
        {
            Seq<string> inner = match.Groups[nameGroup - 1].Captures.ToSeq().Map(x => x.ToString());
            Seq<string> names = match.Groups[nameGroup].Captures.ToSeq().Map(x => x.ToString());
            Seq<KFunc> fs = match.Groups[3].Captures.ToSeq().Map(x => x.ToString())
                .Bind(x => ParseSignature(x).ToSeq());
            return names.Zip(inner)
                .Map(t => new KStruct(t.Left, t.Left, fs, ParseStructs(t.Right)));
        }

        static Seq<KStruct> goSequence(Match match)
        {
            return Range(0, match.Groups[1].Captures.Count)
                .Bind(k => ParseStructs(match.Groups[1].Captures[k].Value))
                .ToSeq();
        }

        static Option<Capture> lastName(Match full)
        => full.Groups[nameGroup].Captures.LastOrNone();

        Match sequence = ParseSequence(text);
        Match full = ParseFull(text);

        Option<Capture> fullName = lastName(full);
        Option<Capture> lastInSequenceName = lastName(sequence);

        bool multiple = 1 < sequence.Groups[1].Captures.Count;
        bool notNested = lastInSequenceName
            .Map(x => x.Value == fullName.Map(x => x.Value))
            .IfNone(false);
        bool isSequence = multiple && notNested;

        return isSequence ? goSequence(sequence) : go(full);
    }


    public static KStruct FixFullNames(KStruct s)
    {
        static KFunc PrefixFunc(String prefix, KFunc f)
        => new KFunc(prefix + "." + f.Name, f.Name, f.Params, f.RetVal);

        static KStruct go(String prefix, KStruct s)
        {
            string next = prefix + (string.IsNullOrEmpty(prefix) ? "" : ".") + s.Name;
            return new KStruct(next, s.Name, s.Funcs.Map(f => PrefixFunc(next, f)), s.Childs.Map(c => go(next, c)));
        }
        return go("", s);
    }

    public static Seq<KStruct> ParseStructs(string text)
    => ParseStructsWithShortNames(text).Map(FixFullNames);

    public static Option<KHeader> ParseHeader(string header)
    {
        static Option<(string, string)> GetInitFunc(string header)
        {
            const string externPattern = @"^extern (\w+)\*\s+(\w+)\s*\(void\);\s*$";
            Match match = Regex.Match(header, externPattern, RegexOptions.Multiline);
            return match.Success ? (match.Groups[1].Value, match.Groups[2].Value) : None;
        }
        static Option<string> GetSymbolsSection(string header, string symbolsName)
        {
            string pattern = @"\s*typedef\s+struct\s*{.*?}\s*" + symbolsName + @"\s*;";
            Match match = Regex.Match(header, pattern, RegexOptions.RightToLeft | RegexOptions.Singleline);
            string symbolsSection = match.Groups[0].Value;
            MatchCollection typedefs = Regex.Matches(symbolsSection, "typedef");
            return match.Success && 1 <= typedefs.Count
                ? symbolsSection
                : None;
        }
        Option<(string ret, string func)> initFunc = GetInitFunc(header);
        Option<string> symbolsPart = initFunc.Bind(t => GetSymbolsSection(header, t.ret));
        Seq<KStruct> childs = symbolsPart.ToSeq().Bind(ParseStructs);

        return GetInitFunc(header)
            .Map(x => new KHeader(x.Item1, x.Item2, childs));
    }
}