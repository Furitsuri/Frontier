using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; } = null;

    public static bool IsValid() => Instance != null;

    private void Awake()
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

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        OnStart();
    }

    private void Update()
    {
        OnUpdate();
    }

    private void LateUpdate()
    {
        OnLateUpdate();
    }

    virtual protected void Init() { }

    virtual protected void OnStart() { }

    virtual protected void OnUpdate() { }

    virtual protected void OnLateUpdate() { }

    private void OnDestroy() { }
}
