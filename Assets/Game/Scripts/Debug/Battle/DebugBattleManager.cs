﻿using Frontier.Entities;
using Frontier.Battle;
using UnityEngine;

namespace Frontier
{
    public class DebugBattleRoutineController : MonoBehaviour
    {
        [SerializeField]
        private Character.Parameter GenerateUnitParamSetting;

        [SerializeField]
        private BattleRoutineController _btlRtnCtrl;

        public static DebugBattleRoutineController instance = null;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);

            if (_btlRtnCtrl == null)
            {
                Instantiate(_btlRtnCtrl);
            }
        }

        private void Start()
        {
            StartCoroutine(_btlRtnCtrl.Battle());
        }
    }
}