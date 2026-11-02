using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// カメラと注視点の間にあるタイルを、注視点周りだけ網点状に抜いて透けさせます。
    /// どのタイルが遮っているかはここで判定し、どこを抜くかはタイルのシェーダー(TileOcclusionDither.cginc)が決めます。
    /// </summary>
    public class TileOcclusionFader
    {
        [Inject] private IStageDataProvider _stageDataProvider = null;

        private static readonly int FocusId       = Shader.PropertyToID( "_TileOcclusionFocus" );
        private static readonly int MaxStrengthId = Shader.PropertyToID( "_TileOcclusionMaxStrength" );

        private readonly Dictionary<Tile, float> _fadingTiles = new Dictionary<Tile, float>();  // 抜き具合が0でないタイルと、その抜き具合
        private readonly HashSet<Tile> _occluders             = new HashSet<Tile>();            // 現フレームで注視点を遮っているタイル
        private readonly List<Tile> _workTiles                = new List<Tile>();

        /// <summary>
        /// 遮蔽しているタイルを判定し、各タイルの抜き具合を更新します。カメラの位置が確定した後に毎フレーム呼び出してください。
        /// </summary>
        /// <param name="camera">描画カメラ</param>
        /// <param name="focusPos">注視点(グリッドカーソル等の足元の位置)</param>
        /// <param name="isActive">falseの場合、全てのタイルを元に戻します(攻撃演出中等)</param>
        public void Update( Camera camera, in Vector3 focusPos, bool isActive )
        {
            _occluders.Clear();

            var stageData = _stageDataProvider.CurrentData;
            if( isActive && camera != null && stageData?.Tiles != null )
            {
                CollectOccluders( stageData.Tiles, camera.transform.position, focusPos );

                Vector3 viewportPos = camera.WorldToViewportPoint( focusPos + Vector3.up * TILE_OCCLUSION_FOCUS_HEIGHT );
                Shader.SetGlobalVector( FocusId, new Vector4( viewportPos.x, viewportPos.y, TILE_OCCLUSION_RADIUS, TILE_OCCLUSION_EDGE_SOFTNESS ) );
                Shader.SetGlobalFloat( MaxStrengthId, TILE_OCCLUSION_MAX_STRENGTH );
            }

            foreach( var tile in _occluders )
            {
                if( !_fadingTiles.ContainsKey( tile ) ) { _fadingTiles.Add( tile, 0f ); }
            }

            float step = DeltaTimeProvider.DeltaTime / TILE_OCCLUSION_FADE_DURATION;

            _workTiles.Clear();
            _workTiles.AddRange( _fadingTiles.Keys );
            foreach( var tile in _workTiles )
            {
                // ステージの再読み込み等で破棄されたタイル
                if( tile == null )
                {
                    _fadingTiles.Remove( tile );
                    continue;
                }

                float current = _fadingTiles[tile];
                float goal    = _occluders.Contains( tile ) ? 1f : 0f;
                float next    = Mathf.MoveTowards( current, goal, step );

                if( next != current ) { tile.ApplyOcclusionFade( next ); }

                if( next <= 0f ) { _fadingTiles.Remove( tile ); }
                else             { _fadingTiles[tile] = next; }
            }
        }

        /// <summary>
        /// カメラから注視点付近(足元・胴・頭)への視線を遮っているタイルを収集します
        /// </summary>
        private void CollectOccluders( Tile[] tiles, in Vector3 cameraPos, in Vector3 focusPos )
        {
            foreach( var tile in tiles )
            {
                if( tile == null ) { continue; }

                Bounds bounds = tile.RenderBounds;
                foreach( float height in TILE_OCCLUSION_SAMPLE_HEIGHTS )
                {
                    Vector3 toTarget = focusPos + Vector3.up * height - cameraPos;
                    float distance   = toTarget.magnitude;
                    if( distance <= 0f ) { continue; }

                    if( bounds.IntersectRay( new Ray( cameraPos, toTarget / distance ), out float hitDistance ) && hitDistance < distance )
                    {
                        _occluders.Add( tile );
                        break;
                    }
                }
            }
        }
    }
}
