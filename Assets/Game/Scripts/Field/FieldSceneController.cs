using Frontier;
using Frontier.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドのロジックを担当します。
    /// FieldData をロードしてノードを配置し、プレイヤーの選択を管理します。
    /// シーンのエントリポイントは FieldMain が担当します。
    /// </summary>
    public class FieldSceneController : FocusRoutineBase
    {
        private const string BattleSceneName  = "BattleScene";
        private const string RecruitSceneName = "RecruitScene";

        [Header( "ノードのプレハブ" )]
        [SerializeField] private FieldNodeView _nodePrefab = null;

        [Header( "ノードを配置する親トランスフォーム" )]
        [SerializeField] private Transform _nodeContainer = null;

        [Header( "経路描画コンポーネント" )]
        [SerializeField] private FieldPathRenderer _pathRenderer = null;

        [Header( "プレイヤーアイコン(3Dモデル導入に伴い非表示化)" )]
        [SerializeField] private FieldPlayerView _playerView = null;

        [Header( "デバッグ用: 起動時に読み込むフィールドID" )]
        [SerializeField] private string _debugFieldId = "field_01";

        [Header( "ランダム生成" )]
        [SerializeField] private bool                  _useRandomGeneration = true;
        [SerializeField] private FieldGenerationConfig  _generationConfig    = null;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain             = null;

        private FieldData                      _fieldData    = null;
        private Dictionary<int, FieldNodeView> _nodeViews    = new Dictionary<int, FieldNodeView>();
        private Dictionary<int, Vector3>       _nodePositions = new Dictionary<int, Vector3>();
        private FieldPlayerCharacterView       _playerCharacterView = null;
        private FieldMenuHandler               _fieldMenuHandler = null;
        private FieldCameraController          _cameraController = null;

        private FieldProgress Progress => GameSession.Instance?.FieldProgress;

        /// <summary>
        /// FocusRoutine優先度制御用。MAIN_FLOWとして登録することで、OptionHandler等の
        /// 優先度の高いルーチンが実行される際に自動的に中断(Pause)されるようにする。
        /// </summary>
        public override int GetPriority() => ( int ) FocusRoutinePriority.MAIN_FLOW;

        private void Start()
        {
            // 戦闘シーンからの遷移時に暗転したままになっている場合に解除する
            LoadingScreenController.Instance?.Hide();

            // 戦闘・雇用などから帰還した場合はクリア済みノードを反映してから進行状態を復元
            if ( FieldTransitionContext.IsFromField )
            {
                RestoreAfterFieldExit();
            }
            else
            {
                // セーブデータのロード等で既にFieldProgressが存在する場合は、そのFieldIdを優先する
                // (存在しない場合はデバッグ用の既定フィールドで新規開始する)
                string fieldId = GameSession.Instance?.FieldProgress?.FieldId ?? _debugFieldId;
                Load( fieldId );
            }
        }

        public void Load( string fieldId )
        {
            var template = FieldDataSerializer.Load( fieldId );
            if ( template == null )
            {
                Debug.LogWarning( $"[FieldSceneController] フィールドデータの読み込みに失敗しました: {fieldId}" );
                return;
            }

            // GameSession に FieldProgress がなければ新規作成。既存の場合は生成シードを引き継いで同じマップを再現する
            var progress     = GameSession.Instance?.FieldProgress;
            bool isNewProgress = progress == null;
            if ( isNewProgress && GameSession.Instance != null )
            {
                progress = new FieldProgress { FieldId = fieldId };
                GameSession.Instance.FieldProgress = progress;
            }

            if ( _useRandomGeneration && _generationConfig != null )
            {
                int seed = isNewProgress ? Guid.NewGuid().GetHashCode() : progress.GenerationSeed;
                if ( progress != null ) progress.GenerationSeed = seed;
                _fieldData = FieldGenerator.Generate( template, _generationConfig, seed );
            }
            else
            {
                _fieldData = template;
            }

            if ( isNewProgress && progress != null )
            {
                progress.CurrentNodeId = _fieldData.StartNodeId;
            }

            BuildNodes();
            RefreshReachability();
        }

        // ── 戦闘・雇用帰還 ────────────────────────────────────────────────────

        private void RestoreAfterFieldExit()
        {
            int clearedNodeId = FieldTransitionContext.ClearedNodeId;
            FieldTransitionContext.Clear();

            Load( _debugFieldId );

            var progress = Progress;
            if ( progress != null && clearedNodeId >= 0 )
            {
                // ステージレベルは「Battle/Bossノードの初回クリア」でのみ進行させる(Recruit等の帰還や再クリアではカウントしない)
                bool isFirstClear = !progress.IsNodeCleared( clearedNodeId );
                var  clearedNode  = FindNode( clearedNodeId );

                progress.MarkCleared( clearedNodeId );
                progress.CurrentNodeId = clearedNodeId;
                RefreshReachability();

                if ( isFirstClear && clearedNode != null )
                {
                    var nodeType = ( FieldNodeType ) clearedNode.Type;
                    if ( nodeType == FieldNodeType.Battle || nodeType == FieldNodeType.Boss )
                    {
                        _userDomain?.IncreaseStageLevel();
                    }
                }
            }
        }

        // ── ノード配置 ───────────────────────────────────────────────────────

        private void BuildNodes()
        {
            foreach ( Transform child in _nodeContainer )
            {
                Destroy( child.gameObject );
            }
            _nodeViews.Clear();
            _nodePositions.Clear();

            foreach ( var nodeData in _fieldData.Nodes )
            {
                var pos  = new Vector3( nodeData.PosX, nodeData.PosY, 0f );
                var view = Instantiate( _nodePrefab, _nodeContainer );
                view.transform.position = pos;
                view.Setup( nodeData, isReachable: false, onSelected: OnNodeSelected );
                _nodeViews[nodeData.Id]     = view;
                _nodePositions[nodeData.Id] = pos;
            }

            if ( _pathRenderer != null )
            {
                _pathRenderer.Build( _fieldData, _nodePositions );
            }

            {
                var progress    = Progress;
                int currentId   = progress != null ? progress.CurrentNodeId : _fieldData.StartNodeId;
                if ( _nodePositions.TryGetValue( currentId, out var startPos ) )
                {
                    // 3Dモデル表示に置き換えたため、旧アイコンは非表示にする
                    if ( _playerView != null ) { _playerView.gameObject.SetActive( false ); }

                    EnsurePlayerCharacterView();
                    _playerCharacterView?.Setup( startPos );

                    EnsureFieldMenuHandler();

                    // シーン遷移(初回配置・戦闘/雇用からの帰還)直後は、カメラの焦点をキャラクターに合わせる
                    EnsureCameraController();
                    _cameraController?.SetFollowTarget( _playerCharacterView );
                }
            }
        }

        /// <summary>
        /// フィールド上に自身を表す3Dキャラクターモデルのビューを生成します(一度だけ)。
        /// </summary>
        private void EnsurePlayerCharacterView()
        {
            if ( _playerCharacterView != null || _hierarchyBld == null ) return;

            _playerCharacterView = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<FieldPlayerCharacterView>( true, false, nameof( FieldPlayerCharacterView ) );
        }

        /// <summary>
        /// シーン内のフィールドカメラ制御コンポーネントを取得します(一度だけ)。
        /// </summary>
        private void EnsureCameraController()
        {
            if ( _cameraController != null ) return;

            _cameraController = FindFirstObjectByType<FieldCameraController>();
        }

        /// <summary>
        /// OPT2入力で開くフィールドメニューのハンドラを生成します(一度だけ)。
        /// 入力コードの登録・Presenter呼び出し等はすべてFieldMenuHandlerが担う。
        /// </summary>
        private void EnsureFieldMenuHandler()
        {
            if ( _fieldMenuHandler != null || _hierarchyBld == null || _playerCharacterView == null ) return;

            _fieldMenuHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<FieldMenuHandler>( true, false, nameof( FieldMenuHandler ) );
            _fieldMenuHandler.Setup( _playerCharacterView );
        }

        /// <summary>
        /// オプション画面等のサブ画面から復帰した際に呼ばれます。
        /// フィールドメニューが開いたままであれば、FieldMenuHandler側で同じ選択状態のまま再表示します。
        /// </summary>
        public override void Restart()
        {
            base.Restart();

            _fieldMenuHandler?.NotifySceneResumed();
        }

        private void RefreshReachability()
        {
            if ( _fieldData == null ) return;

            var progress     = Progress;
            int currentId    = progress != null ? progress.CurrentNodeId : _fieldData.StartNodeId;
            var currentNode  = FindNode( currentId );
            var reachableIds = currentNode?.NextIds ?? new int[0];

            foreach ( var (id, view) in _nodeViews )
            {
                view.SetReachable( reachableIds.Contains( id ) );
            }
        }

        // ── ノード選択 ───────────────────────────────────────────────────────

        private void OnNodeSelected( int nodeId )
        {
            // オプション画面表示中等、自身が中断されている場合はノードクリックを受け付けない
            // (FieldNodeViewは別GameObjectのため、Pause()によるgameObject.SetActive(false)だけでは
            //  このコールバック自体の発火は止められないことに注意)
            if ( !gameObject.activeInHierarchy ) return;

            // メニュー表示中はノード選択を受け付けない
            if ( _fieldMenuHandler != null && _fieldMenuHandler.IsOpen ) return;

            var node = FindNode( nodeId );
            if ( node == null ) return;

            // すべてのノードタイプで先にモデルを移動させ、到着後に処理する
            if ( _playerCharacterView != null && _nodePositions.TryGetValue( nodeId, out var targetPos ) )
            {
                _playerCharacterView.MoveTo( targetPos, () => OnPlayerArrived( node ) );
            }
            else
            {
                OnPlayerArrived( node );
            }
        }

        private void OnPlayerArrived( FieldNodeData node )
        {
            // 進行状態を更新してから到達可能ノードを再評価
            var progress = Progress;
            if ( progress != null ) progress.CurrentNodeId = node.Id;
            RefreshReachability();

            var nodeType = ( FieldNodeType ) node.Type;
            Debug.Log( $"[FieldSceneController] ノード到達: Id={node.Id} Type={nodeType}" );

            switch ( nodeType )
            {
                case FieldNodeType.Battle:
                case FieldNodeType.Boss:
                    FieldTransitionContext.SetupFieldExitTransition( node.Id, node.StageIndex );
                    TransitionToScene( BattleSceneName );
                    break;

                case FieldNodeType.Recruit:
                    FieldTransitionContext.SetupFieldExitTransition( node.Id );
                    TransitionToScene( RecruitSceneName );
                    break;

                case FieldNodeType.Rest:
                    // TODO: 休憩処理（回復等）を実装
                    Debug.Log( "[FieldSceneController] Rest は未実装です。" );
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// ローディング画面を表示してから指定シーンへ遷移します。
        /// 暗転が完了するまで旧シーンを破棄しないことで、初期化前のGame画面が一瞬映る問題を防ぎます。
        /// </summary>
        private void TransitionToScene( string sceneName )
        {
            // 遷移開始時刻を記録（遷移先シーン側で遷移完了時に所要時間をログ出力する）
            FieldTransitionContext.MarkBattleTransitionStart();

            var loadingScreen = LoadingScreenController.EnsureInstance();
            if ( loadingScreen != null )
            {
                // 暗転フェードと非同期ロードを「並行」させ、両方完了後にシーンを活性化する。
                // （allowSceneActivation=false のため暗転中の裏読込でチラつきは起きない。フェード時間を遷移時間から実質ゼロにする狙い）
                bool fadeDone = false;
                loadingScreen.Show( onComplete: () => fadeDone = true );
                StartCoroutine( LoadSceneAsyncRoutine( sceneName, () => fadeDone ) );
            }
            else
            {
                StartCoroutine( LoadSceneAsyncRoutine( sceneName, () => true ) );
            }
        }

        private IEnumerator LoadSceneAsyncRoutine( string sceneName, System.Func<bool> isFadeComplete )
        {
            var op = SceneManager.LoadSceneAsync( sceneName );
            op.allowSceneActivation = false;

            // 「ロード完了(0.9)」かつ「暗転完了」の両方が揃ってから活性化する
            while ( op.progress < 0.9f || !isFadeComplete() )
            {
                yield return null;
            }

            op.allowSceneActivation = true;
        }

        private FieldNodeData FindNode( int nodeId )
        {
            foreach ( var node in _fieldData.Nodes )
            {
                if ( node.Id == nodeId ) return node;
            }
            return null;
        }
    }
}
