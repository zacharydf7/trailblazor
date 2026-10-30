namespace Trailblazor;

// Option values come as objects from TbOption, so a field's comparer, if it has one, is applied to them here.
internal static class OptionValues
{
    public static bool Same<T>(IEqualityComparer<T>? comparer, object? first, object? second) =>
        comparer is not null && first is T one && second is T other
            ? comparer.Equals(one, other)
            : Equals(first, second);
}
