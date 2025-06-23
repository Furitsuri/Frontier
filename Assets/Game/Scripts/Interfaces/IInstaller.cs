using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInstaller
{
    public void InstallBindings();

    public void InstallBindings<T>(T instance);
}