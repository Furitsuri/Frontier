using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class DebugBattleManager : MonoBehaviour
    {
        [SerializeField]
        private Character.Parameter GenerateUnitParamSetting;

        [SerializeField]
        private BattleManager _btlMgr;

        public static DebugBattleManager instance = null;

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

            if (_btlMgr == null)
            {
                Instantiate(_btlMgr);
            }
        }

        private void Start()
        {
            StartCoroutine(_btlMgr.Battle());
        }
    }
}