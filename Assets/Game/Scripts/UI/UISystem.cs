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
            Transform childGeneralUI = transform.GetChild( ( int ) ChildIndex.General );
            if( childGeneralUI != null )
            {
                _generarlUi = childGeneralUI.GetComponent<GeneralUISystem>();
                NullCheck.AssertNotNull( _generarlUi, nameof( _generarlUi ) );
            }

            Transform childPlacementUI = transform.GetChild( ( int ) ChildIndex.Placement );
            if( childPlacementUI != null )
            {
                _placementUi = childPlacementUI.GetComponent<DeploymentUISystem>();
                NullCheck.AssertNotNull( _placementUi, nameof( _placementUi ) );
            }

            Transform childBattleUI = transform.GetChild( ( int ) ChildIndex.Battle );
            if( childBattleUI != null )
            {
                _battleUi = childBattleUI.GetComponent<BattleUISystem>();
                NullCheck.AssertNotNull( _battleUi, nameof( _battleUi ) );
            }

#if UNITY_EDITOR
            Transform childDebugUI = transform.GetChild( ( int ) ChildIndex.Debug );
            if( childDebugUI != null )
            {
                _debugUi = childDebugUI.GetComponent<DebugUISystem>();
                NullCheck.AssertNotNull( _debugUi, nameof( _debugUi ) );
            }
#endif // UNITY_EDITOR
        }
    }
}