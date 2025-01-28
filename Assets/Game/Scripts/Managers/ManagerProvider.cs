using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class ManagerProvider : Singleton<ManagerProvider>, IServiceProvider
    {
        [SerializeField]
        private GameObject _inputFacadeObject;

        private InputFacade _inputFacade;

        override protected void Init()
        {
            GameObject inputFcd = Instantiate(_inputFacadeObject);
            if (inputFcd != null)
            {
                _inputFacade = inputFcd.GetComponent<InputFacade>();
            }
        }

        public T GetService<T>()
        {
            if (typeof(T) == typeof(InputFacade))
            {
                return (T)(object)_inputFacade;
            }

            return default(T);
        }
    }
}