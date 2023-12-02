using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class ManagerProvider : Singleton<ManagerProvider>, IServiceProvider
    {
        // [SerializeField]
        // private GameObject _battleManagerObject;
        // [SerializeField]
        // private GameObject _soundManagerObject;
        [SerializeField]
        private GameObject _stageControllerObject;
        private StageController _stageController;

        override protected void Init()
        {
            GameObject stgCtrl = Instantiate(_stageControllerObject);
            if (stgCtrl != null)
            {
                _stageController = stgCtrl.GetComponent<StageController>();
            }
        }

        public T GetService<T>()
        {
            if (typeof(T) == typeof(StageController))
            {
                return (T)(object)_stageController;
            }

            return default(T);
        }
    }
}