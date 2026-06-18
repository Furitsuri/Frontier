using Frontier;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドシーンのエントリポイント。
    /// FieldData をロードしてノードを配置し、プレイヤーの選択を管理します。
    /// </summary>
    public class FieldSceneController : MonoBehaviour
    {
        private const string BattleSceneName = "GameMain";

        [Header( "ノードのプレハブ" )]
        [SerializeField] private FieldNodeView _nodePrefab = null;

        [Header( "ノードを配置する親トランスフォーム" )]
        [SerializeField] private Transform _nodeContainer = null;

        [Header( "デバッグ用: 起動時に読み込むフィールドID" )]
        [SerializeField] private string _debugFieldId = "field_01";

        private FieldData                      _fieldData  = null;
        private Dictionary<int, FieldNodeView> _nodeViews  = new Dictionary<int, FieldNodeView>();

        private FieldProgress Progress => GameSession.Instance?.FieldProgress;

        private void Start()
        {
            // 戦闘から帰還した場合はクリア済みノードを反映してから進行状態を復元
            if ( FieldTransitionContext.IsFromField )
            {
                RestoreAfterBattle();
            }
            else
            {
                Load( _debugFieldId );
            }
        }

        public void Load( string fieldId )
        {
            _fieldData = FieldDataSerializer.Load( fieldId );
            if ( _fieldData == null )
            {
                Debug.LogWarning( $"[FieldSceneController] フィールドデータの読み込みに失敗しました: {fieldId}" );
                return;
            }

            // GameSession に FieldProgress がなければ新規作成
            if ( GameSession.Instance != null && GameSession.Instance.FieldProgress == null )
            {
                GameSession.Instance.FieldProgress = new FieldProgress
                {
                    FieldId       = fieldId,
                    CurrentNodeId = _fieldData.StartNodeId,
                };
            }

            BuildNodes();
            RefreshReachability();
        }

        // ── 戦闘帰還 ────────────────────────────────────────────────────────

        private void RestoreAfterBattle()
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

            foreach ( var nodeData in _fieldData.Nodes )
            {
                var view = Instantiate( _nodePrefab, _nodeContainer );
                view.transform.position = new Vector3( nodeData.PosX, nodeData.PosY, 0f );
                view.Setup( nodeData, isReachable: false, onSelected: OnNodeSelected );
                _nodeViews[nodeData.Id] = view;
            }
        }

        private void RefreshReachability()
        {
            if ( _fieldData == null ) return;

            var progress     = Progress;
            var currentNode  = progress != null ? FindNode( progress.CurrentNodeId ) : null;
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

            var nodeType = ( FieldNodeType ) node.Type;
            Debug.Log( $"[FieldSceneController] ノード選択: Id={nodeId} Type={nodeType}" );

            switch ( nodeType )
            {
                case FieldNodeType.Battle:
                case FieldNodeType.Boss:
                    TransitionToBattle( node );
                    break;

                case FieldNodeType.Recruit:
                    // TODO: 雇用シーンが分離されたら遷移を実装
                    Debug.Log( "[FieldSceneController] Recruit は未実装です。" );
                    break;

                case FieldNodeType.Rest:
                    // TODO: 休憩処理（回復等）を実装
                    Debug.Log( "[FieldSceneController] Rest は未実装です。" );
                    break;

                default:
                    break;
            }
        }

        private void TransitionToBattle( FieldNodeData node )
        {
            var progress = Progress;
            if ( progress != null ) progress.CurrentNodeId = node.Id;
            FieldTransitionContext.SetupBattleTransition( node.StageIndex, node.Id );
            SceneManager.LoadScene( BattleSceneName );
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
