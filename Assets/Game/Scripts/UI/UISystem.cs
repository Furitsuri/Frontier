using UnityEngine;

namespace Frontier.UI
{
    public class UISystem : MonoBehaviour, IUiSystem
    {
        [SerializeField] private GeneralUISystem _generalUi         = null;
        [SerializeField] private RecruitUISystem _recruitmentUi = null;
        [SerializeField] private DeploymentUISystem _deploymentUi   = null;
        [SerializeField] private BattleUISystem _battleUi           = null;
#if UNITY_EDITOR
        private DebugUISystem _debugUi          = null;
#endif // UNITY_EDITOR

        public GeneralUISystem GeneralUi => _generalUi;
        public RecruitUISystem RecruitUi => _recruitmentUi;
        public DeploymentUISystem DeployUi => _deploymentUi;
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
            Recruit,
            Deployment,
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
#if UNITY_EDITOR
            LazyInject.GetOrCreate( ref _debugUi, () => transform.GetChild( ( int ) ChildIndex.Debug ).GetComponent<DebugUISystem>() );
#endif // UNITY_EDITOR

            _generalUi?.Setup();
            _recruitmentUi?.Setup();
            _deploymentUi?.Setup();
            _battleUi?.Setup();

            _recruitmentUi?.gameObject.SetActive( false );
            _deploymentUi?.gameObject.SetActive( false );
            _battleUi?.gameObject.SetActive( false );
        }
    }
}