using System.Globalization;

namespace Trailblazor;

// What the number field and the sliders need to know about the number types they hold.
internal static class NumberTypes
{
    public static bool IsNumber(Type type) =>
        !type.IsEnum && Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;

    // Convert.ToDecimal throws for NaN, the infinities and doubles past decimal's range, so those return false.
    public static bool TryToDecimal(object? value, out decimal number)
    {
        number = 0;
        if (value is null || !IsNumber(value.GetType())
            || !(Math.Abs(Convert.ToDouble(value, CultureInfo.InvariantCulture)) < (double)decimal.MaxValue))
        {
            return false;
        }

        number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return true;
    }

    public static bool IsWhole(Type type) => Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64;

    // The range the type itself can hold; float and double are stepped within decimal's range.
    public static (decimal Min, decimal Max) Limits(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.SByte => (sbyte.MinValue, sbyte.MaxValue),
        TypeCode.Byte => (byte.MinValue, byte.MaxValue),
        TypeCode.Int16 => (short.MinValue, short.MaxValue),
        TypeCode.UInt16 => (ushort.MinValue, ushort.MaxValue),
        TypeCode.Int32 => (int.MinValue, int.MaxValue),
        TypeCode.UInt32 => (uint.MinValue, uint.MaxValue),
        TypeCode.Int64 => (long.MinValue, long.MaxValue),
        TypeCode.UInt64 => (ulong.MinValue, ulong.MaxValue),
        _ => (decimal.MinValue, decimal.MaxValue),
    };
}
