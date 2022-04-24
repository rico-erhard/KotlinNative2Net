using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

public record KFunc(string Name);

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

    const string structStart = @"(?:\s*(typedef )?\s*struct\s+{" + commentPattern + functionsPattern + commentPattern;

    public static Match ParseFunctions(string text)
    {
        Match match = Regex.Match(text, functionsPattern);
        return match;
    }

    static Match ParseFull(string text)
    {
        //const string pattern = @"(\s*(typedef )?\s*struct\s+{(.*)} ([^ ]+);\s+)+";
        const string pattern = structStart + @"(.*)} ([^ ]+);\s+)+";
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match;
    }

    static Match ParseSequence(string text)
    {
        //const string pattern = @"(\s*(typedef )?\s*struct\s+{([^}]*)} ([^ ]+);\s+)+";
        const string pattern = structStart + @"([^}]*)} ([^ ]+);\s+)+";
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match;
    }

    public static Seq<KStruct> Parse(string text)
    {
        const int nameGroup = 9;

        static Seq<KStruct> go(Match match)
        {
            Seq<string> inner = match.Groups[nameGroup - 1].Captures.ToSeq().Map(x => x.ToString());
            Seq<string> names = match.Groups[nameGroup].Captures.ToSeq().Map(x => x.ToString());
            return names.Zip(inner)
                .Map(t => new KStruct(t.Left, Seq<KFunc>(), Parse(t.Right)));
        }

        static Option<Capture> lastName(Match full)
        => full.Groups[nameGroup].Captures.LastOrNone();

        Match sequence = ParseSequence(text);
        Match full = ParseFull(text);

        Option<Capture> fullName = lastName(full);
        Option<Capture> lastInSequenceName = lastName(sequence);

        bool sequenceFound = lastInSequenceName
            .Map(x => x.Value == fullName.Map(x => x.Value))
            .IfNone(false);

        return go(sequenceFound ? sequence : full);
    }
}
