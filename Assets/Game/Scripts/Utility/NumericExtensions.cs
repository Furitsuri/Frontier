using System;
using System.Numerics;
using UnityEngine;

static public class NumericExtensions
{
    static public bool IsBetween<T>(this T value, T min, T max) where T : IComparable<T>
    {
        return 0 <= value.CompareTo(min) && value.CompareTo(max) <= 0;
    }

    static public UnityEngine.Vector3 XZ(this UnityEngine.Vector3 vec)
    {
        return new UnityEngine.Vector3(vec.x, 0f, vec.z);
    }
}