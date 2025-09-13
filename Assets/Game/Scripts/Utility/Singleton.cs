using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    static public T Instance { get; private set; } = null;

    static public bool IsValid() => Instance != null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            Instance.Init();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        OnStart();
    }

    void Update()
    {
        OnUpdate();
    }

    void LateUpdate()
    {
        OnLateUpdate();
    }

    virtual protected void Init() { }

    virtual protected void OnStart() { }

    virtual protected void OnUpdate() { }

    virtual protected void OnLateUpdate() { }

    private void OnDestroy() { }
}
