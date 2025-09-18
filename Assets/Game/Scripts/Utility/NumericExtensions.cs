using System;

static public class NumericExtensions
{
    static public bool IsBetween<T>(this T value, T min, T max) where T : IComparable<T>
    {
        return 0 <= value.CompareTo(min) && value.CompareTo(max) <= 0;
    }
}