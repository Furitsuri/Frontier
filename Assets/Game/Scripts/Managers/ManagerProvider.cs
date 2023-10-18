using Frontier.Stage;
using UnityEngine;

namespace Frontier
{
    public class ManagerProvider : Singleton<ManagerProvider>, IServiceProvider
    {
        [SerializeField]
        private GameObject _battleManagerObject;
        [SerializeField]
        private GameObject _soundManagerObject;
        [SerializeField]
        private GameObject _stageControllerObject;

        private BattleManager _battleManager;
        private SoundManager _soundManager;
        private StageController _stageController;

        override protected void Init()
        {
            GameObject btlMgr = Instantiate(_battleManagerObject);
            if (btlMgr != null)
            {
                _battleManager = btlMgr.GetComponent<BattleManager>();
            }

            GameObject sndMgr = Instantiate(_soundManagerObject);
            if (sndMgr != null)
            {
                _soundManager = sndMgr.GetComponent<SoundManager>();
            }

            GameObject stgCtrl = Instantiate(_stageControllerObject);
            if (stgCtrl != null)
            {
                _stageController = stgCtrl.GetComponent<StageController>();
            }
        }

        public T GetService<T>()
        {
            if (typeof(T) == typeof(BattleManager))
            {
                return (T)(object)_battleManager;
            }
            if (typeof(T) == typeof(SoundManager))
            {
                return (T)(object)_soundManager;
            }
            if (typeof(T) == typeof(StageController))
            {
                return (T)(object)_stageController;
            }

            return default(T);
        }
    }
}