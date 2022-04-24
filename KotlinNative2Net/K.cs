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

    public static Seq<KStruct> ParseStructs(string text)
    {
        Match match = ParseFull(text);
        Seq<string> groups = match.Groups.ToSeq().Map(x => x.ToString());
        return groups.Count switch
        {
            4 => Some(new KStruct(groups[3].ToString(), Seq<KFunc>(), ParseStructs(groups[2].ToString()))).ToSeq(),
            _ => Seq<KStruct>(),
        };
    }

    static Match ParseFull(string text)
    {
        string pattern = @"(\s*(typedef )?\s*struct\s+{(.*)} ([^ ]+);\s+)+";
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match;
    }

    private static Match ParseSequence(string text)
    {
        string pattern = @"(\s*(typedef )?\s*struct\s+{([^}]*)} ([^ ]+);\s+)+";
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match;
    }

    public static Seq<KStruct> ParseSequential(string text)
    {
        static Seq<KStruct> go(Match match)
        {
            Seq<string> funcs = match.Groups[3].Captures.ToSeq().Map(x => x.ToString());
            Seq<string> names = match.Groups[4].Captures.ToSeq().Map(x => x.ToString());
            return names.Zip(funcs).Map(t => new KStruct(t.Left, Seq<KFunc>(), ParseSequential(t.Right)));
        }
        Match match = ParseSequence(text);
        return go(match);
    }


    public static Seq<KStruct> Parse(string text)
    {
        static Seq<KStruct> go(Match match)
        {
            Seq<string> funcs = match.Groups[3].Captures.ToSeq().Map(x => x.ToString());
            Seq<string> names = match.Groups[4].Captures.ToSeq().Map(x => x.ToString());
            return names.Zip(funcs)
                .Map(t => new KStruct(t.Left, Seq<KFunc>(), Parse(t.Right)));
        }

        static Option<Capture> getLastName(Match full)
        => full.Groups[4].Captures.LastOrNone();

        Match sequence = ParseSequence(text);
        Match full = ParseFull(text);

        Option<Capture> name = getLastName(full);
        Option<Capture> lastNameInSequence = getLastName(sequence);
        bool sequenceFound = lastNameInSequence.Map(x => x.Value == name.Map(x => x.Value)).IfNone(false);

        return go(sequenceFound ? sequence : full);
    }

}
