using UnityEngine;

namespace Frontier.UI
{
    public class UISystem : MonoBehaviour, IUiSystem
    {
        private GeneralUISystem _generarlUi     = null;
        private DeploymentUISystem _placementUi  = null;
        private BattleUISystem _battleUi        = null;
#if UNITY_EDITOR
        private DebugUISystem _debugUi          = null;
#endif // UNITY_EDITOR

        public GeneralUISystem GeneralUi => _generarlUi;
        public DeploymentUISystem DeployUi => _placementUi;
        public BattleUISystem BattleUi => _battleUi;
#if UNITY_EDITOR
        public DebugUISystem DebugUi => _debugUi;
#endif // UNITY_EDITOR

        /// <summary>
        /// UIのカテゴリを示すインデックス値です
        /// </summary>
        enum ChildIndex
        {
            General = 0,
            Placement,
            Battle,
#if UNITY_EDITOR
            Debug,
#endif // UNITY_EDITOR
        }

        // Start is called before the first frame update
        void Awake()
        {
            InitializeUiSystem();
        }

        public void InitializeUiSystem()
        {
            LazyInject.GetOrCreate( ref _generarlUi, () => transform.GetChild( ( int ) ChildIndex.General ).GetComponent<GeneralUISystem>() );
            LazyInject.GetOrCreate( ref _placementUi, () => transform.GetChild( ( int ) ChildIndex.Placement ).GetComponent<DeploymentUISystem>() );
            LazyInject.GetOrCreate( ref _battleUi, () => transform.GetChild( ( int ) ChildIndex.Battle ).GetComponent<BattleUISystem>() );

#if UNITY_EDITOR
            LazyInject.GetOrCreate( ref _debugUi, () => transform.GetChild( ( int ) ChildIndex.Debug ).GetComponent<DebugUISystem>() );
#endif // UNITY_EDITOR
        }
    }
}