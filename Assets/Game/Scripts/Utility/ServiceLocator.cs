using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ServiceLocator<T> where T : class
{
    public static T Instance
    {
        get;
        private set;
    }

    public static bool IsValid()
    {
        return Instance != null;
    }

    public static void Bind( T instance )
    {
        Instance = instance;
    }

    public static void Unbind( T instance )
    {
        if( Instance == instance )
        {
            Instance = null;
        }
    }

    public static void Clear()
    {
        Instance = null;
    }
}
