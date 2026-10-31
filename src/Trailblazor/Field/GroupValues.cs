namespace Trailblazor;

// Razor types a group item by its literal, so Value="2" is an int even in a group of int?. The group takes the value
// when it is its own type or that type's nullable, as a select or toggle group does with its options.
internal static class GroupValues
{
    public static bool TryFit<T>(object? value, out T fitted)
    {
        switch (value)
        {
            case T same:
                fitted = same;
                return true;
            case null when default(T) is null:
                fitted = default!;
                return true;
            default:
                fitted = default!;
                return false;
        }
    }

    public static T Fit<T>(object? value) => TryFit<T>(value, out var fitted)
        ? fitted
        : throw new InvalidCastException($"{value} doesn't fit a group of {typeof(T).Name}.");
}
