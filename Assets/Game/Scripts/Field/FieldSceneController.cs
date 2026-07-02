using Frontier;
using Frontier.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header( "プレイヤーアイコン" )]
        [SerializeField] private FieldPlayerView _playerView = null;

        [Header( "デバッグ用: 起動時に読み込むフィールドID" )]
        [SerializeField] private string _debugFieldId = "field_01";

        [Header( "ランダム生成" )]
        [SerializeField] private bool                  _useRandomGeneration = true;
        [SerializeField] private FieldGenerationConfig  _generationConfig    = null;

        private FieldData                      _fieldData    = null;
        private Dictionary<int, FieldNodeView> _nodeViews    = new Dictionary<int, FieldNodeView>();
        private Dictionary<int, Vector3>       _nodePositions = new Dictionary<int, Vector3>();

        private FieldProgress Progress => GameSession.Instance?.FieldProgress;

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
                Load( _debugFieldId );
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
                progress.MarkCleared( clearedNodeId );
                progress.CurrentNodeId = clearedNodeId;
                RefreshReachability();
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

            if ( _playerView != null )
            {
                var progress    = Progress;
                int currentId   = progress != null ? progress.CurrentNodeId : _fieldData.StartNodeId;
                if ( _nodePositions.TryGetValue( currentId, out var startPos ) )
                {
                    _playerView.Setup( startPos );
                }
            }
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
            var node = FindNode( nodeId );
            if ( node == null ) return;

            // すべてのノードタイプで先にアイコンを移動させ、到着後に処理する
            if ( _playerView != null && _nodePositions.TryGetValue( nodeId, out var targetPos ) )
            {
                _playerView.MoveTo( targetPos, () => OnPlayerArrived( node ) );
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
