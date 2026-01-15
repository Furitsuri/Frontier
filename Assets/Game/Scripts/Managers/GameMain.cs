using Frontier.Battle;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using Frontier.Tutorial;
using Frontier.DebugTools.DebugMenu;
using Zenject;
using static Constants;

namespace Frontier
{
    public class GameMain : FocusRoutineController
    {
        [Header( "UIカメラのオブジェクト" )]
        [SerializeField] private GameObject _UICameraObject;

        [Header( "各種マネージャのプロバイダオブジェクト" )]
        [SerializeField] private GameObject _managerProvider;

        [Header( "ステージ開始時に表示する時間(秒)" )]
        [SerializeField] private float stageStartDelay = 2f;

        [Inject] private DiContainer _diContainer = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private InputFacade _inputFcd = null;
        [Inject] private TutorialFacade _tutorialFcd = null;

        private GameObject _stageImage;
#if UNITY_EDITOR
        private DebugMenuFacade _debugMenuFcd;
        private DebugEditorMonoDriver _debugEditorMonoDrv;
#endif // UNITY_EDITOR

        static public GameMain instance = null;

        void Awake()
        {
            if( instance == null )
            {
                instance = this;
            }
            else if( instance != this )
            {
                Destroy( gameObject );
            }

            if( transform.parent != null )
            {
                transform.SetParent( null ); // ルートに移動
            }

            DontDestroyOnLoad( gameObject );

            Debug.Assert( _hierarchyBld != null, "Error : インスタンスの生成管理を行うオブジェクトが設定されていません。" );
            Debug.Assert( _inputFcd != null, "Error : 入力窓口のオブジェクトが設定されていません。" );

            if( ManagerProvider.Instance == null )
            {
                _hierarchyBld.CreateComponentAndOrganize<ManagerProvider>( _managerProvider, true );
            }

#if UNITY_EDITOR
            LazyInject.GetOrCreate( ref _debugMenuFcd, () => _hierarchyBld.InstantiateWithDiContainer<DebugMenuFacade>( false ) );
            LazyInject.GetOrCreate( ref _debugEditorMonoDrv, () => _diContainer.Resolve<DebugEditorMonoDriver>() );
#endif // UNITY_EDITOR
        }

        // Start is called before the first frame update
        void Start()
        {
            // 入力関連の初期化
            _inputFcd.Init();
            // チュートリアル関連の初期化
            _tutorialFcd.Init();

            base.Init();
            InitGame();
        }

        void Update()
        {
            base.UpdateRoutine();
        }

        void LateUpdate()
        {
            base.LateUpdateRoutine();
        }

        void FixedUpdate()
        {
            base.FixedUpdateRoutine();
        }

        /// <summary>
        /// ゲームを初期化します
        /// </summary>
        private void InitGame()
        {
            // アニメーションデータの初期化
            AnimDatas.Init();

#if UNITY_EDITOR
            // デバッグモードの初期化
            _debugMenuFcd.Init( CanAcceptDebugTransition, AcceptDebugTransition );
            _debugEditorMonoDrv.Init();
            // デバッグモードへ移行するための入力コードを登録
            ResgiterInputCodes();
#endif // UNITY_EDITOR

            _stageImage = GameObject.Find( "StageLevelImage" );
            if( _stageImage != null )
            {
                Invoke( "StageLevelImage", stageStartDelay );
            }
        }

        /// <summary>
        /// ステージレベルの画像表示を取りやめます
        /// Invoke関数で参照されます
        /// </summary>
        private void StageLevelImage()
        {
            _stageImage.SetActive( false );
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバッグメニューを開くための入力コードを登録します。
        /// ※この入力コードはUnity Editor上ではデバッグ状態以外の全ての状態で有効です。
        /// </summary>
        private void ResgiterInputCodes()
        {
            int hashCode = Hash.GetStableHash( Constants.DEBUG_TRANSION_INPUT_HASH_STRING );

            _inputFcd.RegisterInputCodes( (GuideIcon.DEBUG_MENU, "DEBUG", CanAcceptDebugTransition, new AcceptBooleanInput( AcceptDebugTransition ), 0.0f, hashCode) );
        }

        /// <summary>
        /// デバッグメニューを開くための入力を受け付けるかどうかを判定します。
        /// </summary>
        /// <returns>デバッグメニューへの遷移の可否</returns>
        private bool CanAcceptDebugTransition()
        {
            return true;
        }

        /// <summary>
        /// デバッグメニューへの遷移入力を受け付けた際の処理を行います。
        /// </summary>
        /// <param name="isDebugTranstion">デバッグメニューへの遷移入力</param>
        /// <returns>入力実行の有無</returns>
        private bool AcceptDebugTransition( bool isDebugTranstion )
        {
            if( !isDebugTranstion ) return false;

            _debugMenuFcd.OpenDebugMenu();

            return true;
        }
#endif // UNITY_EDITOR
    }
}