using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

public record KParam(string Type, string Name);
public record KFunc(string Name, Seq<KParam> Params);

public record KStruct(string Name, Seq<KFunc> Funcs, Seq<KStruct> Childs);

public static class Declaration
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

    const string functionsPattern = "(" + commentPattern + @"\s*(\w+\*?)\s+\(\*(\w+)\)\((\s*(\w+\*?)\s*(\w+)?,?)+\);\s*" + ")*";

    const string structStart = @"(\s*(typedef )?\s*struct\s+{" + commentPattern + functionsPattern + commentPattern;

    public static Match ParseFunctions(string text)
    {
        Match match = Regex.Match(text, functionsPattern);
        return match;
    }

    public static Option<KFunc> ParseSignature(string funcSignature)
    {
        Match match = Regex.Match(funcSignature, functionsPattern);
        Option<string> funcName = match.Groups[3].Captures.ToSeq().Map(x => x.ToString()).HeadOrNone();
        Seq<string> paramTypes = match.Groups[5].Captures.ToSeq().Map(x => x.ToString());
        Seq<string> paramNames = match.Groups[6].Captures.ToSeq().Map(x => x.ToString());
        return funcName.Map(x => new KFunc(x, paramTypes.Zip(paramNames)
            .Map(t => new KParam(t.Left, t.Right))));
    }

    public static Seq<KStruct> Parse(string text)
    {
        static Match ParseFull(string text)
        {
            const string pattern = structStart + @"(.*)} ([^ ]+);\s+)+";
            Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
            return match;
        }

        static Match ParseSequence(string text)
        {
            //const string pattern = structStart + @"([^}]*)} ([^ ]+);\s+)+";
            const string pattern = structStart + @"(.*?)} ([^ ]+);\s+)+";
            Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
            return match;
        }

        const int nameGroup = 10;

        static Seq<KStruct> go(Match match)
        {
            Seq<string> inner = match.Groups[nameGroup - 1].Captures.ToSeq().Map(x => x.ToString());
            Seq<string> names = match.Groups[nameGroup].Captures.ToSeq().Map(x => x.ToString());
            Seq<KFunc> fs = match.Groups[5].Captures.ToSeq().Map(x => new KFunc(x.ToString(), Seq<KParam>()));
            return names.Zip(inner)
                .Map(t => new KStruct(t.Left, fs, Parse(t.Right)));
        }

        static Seq<KStruct> goSequence(Match match)
        {
            return Range(0, match.Groups[1].Captures.Count)
                .Bind(k => Parse(match.Groups[1].Captures[k].Value))
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
}
