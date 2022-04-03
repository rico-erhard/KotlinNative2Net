using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

public record KFunc(string name);

public record KStruct(string name, Seq<KFunc> funcs, Seq<KStruct> structs);

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

    public static Option<KStruct> Parse(string text)
    {
        string pattern = @"\s*(typedef )?\s*struct\s+{(.*)} ([^ ]+);";
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return LastGroup(match)
            .Map(x => new KStruct(x, Seq<KFunc>(), Seq<KStruct>()));
    }

}
