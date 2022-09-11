using LanguageExt;
using static LanguageExt.Prelude;

namespace KotlinNative2Net;

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

    public static Option<KStruct> FindChild(this KStruct s , Func<KStruct, bool> pred)
    => s.FlattenChilds().Find(pred);

    public static Option<KStruct> FindChild(this KStruct s, string name)
    => FindChild(s, x => x.FullName.EndsWith('.' + name));

    public static Option<KFunc> FindFunc(this KStruct s, Func<KFunc, bool> pred)
    => s.FlattenChilds().Bind(x => x.Funcs).Find(pred);

    public static Option<KFunc> FindFunc(this KStruct s, string name)
    => FindFunc(s, x => x.FullName.EndsWith('.' + name));
}
