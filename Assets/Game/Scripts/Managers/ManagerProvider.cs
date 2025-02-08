using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class ManagerProvider : Singleton<ManagerProvider>, IServiceProvider
    {
        override protected void Init()
        {
        }

        public T GetService<T>()
        {
            return default(T);
        }
    }
}