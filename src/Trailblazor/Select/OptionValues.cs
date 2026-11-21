namespace Trailblazor;

// Option values come as objects from TbOption, so a field's comparer, if it has one, is applied to them here.
internal static class OptionValues
{
    public static bool Same<T>(IEqualityComparer<T>? comparer, object? first, object? second) =>
        comparer is not null && first is T one && second is T other
            ? comparer.Equals(one, other)
            : Equals(first, second);

    // A multiple choice's value after smart paste: what was chosen and wasn't offered, such as a disabled option,
    // which keeps its state, and then the options the model named.
    public static IReadOnlyList<T> Pasted<T>(IEqualityComparer<T>? comparer, IReadOnlyList<T>? value,
        IReadOnlyList<object?> named, IReadOnlyList<object?> offered) =>
        [.. (value ?? []).Where(chosen => !offered.Any(item => Same(comparer, chosen, item))), .. named.Cast<T>()];
}
