using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ServiceLocator<T> where T : class
{
    static public T Instance
    {
        get;
        private set;
    }

    static public bool IsValid()
    {
        return Instance != null;
    }

    static public void Bind( T instance )
    {
        Instance = instance;
    }

    static public void Unbind( T instance )
    {
        if( Instance == instance )
        {
            Instance = null;
        }
    }

    static public void Clear()
    {
        Instance = null;
    }
}
