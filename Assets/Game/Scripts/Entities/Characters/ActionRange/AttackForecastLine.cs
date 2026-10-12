using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// 攻撃して来る可能性のあるキャラクターから、対象のタイルへ向けて伸びる攻撃予告線1本分です。
    /// 線は対象のタイルまで到達させず、その手前で止めます。
    /// 見た目(色・太さ・マテリアル)はプレハブ(AttackForecastLine.prefab)のLineRenderer側で、
    /// 線の形状(山なりの高さ・手前で止める距離・滑らかさ)はこのコンポーネントのInspector上の値で設定し、
    /// このクラスは指定された2点に合わせた線の配置と、表示・非表示の切り替えのみを担います。
    /// 生成はPrefabRegistry.AttackForecastLinePrefabを基に、HierarchyBuilderBase経由で行ってください。
    /// </summary>
    [RequireComponent( typeof( LineRenderer ) )]
    public class AttackForecastLine : MonoBehaviour
    {
        [Header( "線の両端の高さ" )]
        [SerializeField] private float _startHeight = 1.0f;             // 攻撃側キャラクターの足元から、線の始点までの高さ
        [SerializeField] private float _targetHeight = 0.5f;            // 対象タイルの表面から、線が向かう先までの高さ

        [Header( "線の形状" )]
        [SerializeField] private float _arcHeight = 0.6f;               // 山なりの高さ(距離に依らない分)
        [SerializeField] private float _arcHeightPerDistance = 0.15f;   // 山なりの高さ(両端の水平距離1あたりの加算分)
        [SerializeField] private float _endGap = 0.6f;                  // 対象タイルの中心から、どれだけ手前で線を止めるか(水平距離)
        [SerializeField, Range( 0f, 1f )] private float _maxGapRate = 0.5f;    // 両端が近い場合に、手前で止める距離が全体に占める割合の上限
        [SerializeField, Min( 2 )] private int _segmentCount = 24;      // 線を構成する点の数(多いほど滑らかになる)

        private LineRenderer _lineRenderer = null;

        /// <summary>
        /// 攻撃側キャラクターの足元から対象タイルの表面へ向かう線を表示します
        /// </summary>
        /// <param name="attackerFootPos">攻撃側キャラクターの足元の座標</param>
        /// <param name="targetTileSurfacePos">対象タイルの表面座標</param>
        public void Show( Vector3 attackerFootPos, Vector3 targetTileSurfacePos )
        {
            // 非アクティブ状態で生成された場合Awakeが呼ばれないため、使用時に遅延して取得する
            if( null == _lineRenderer ) { _lineRenderer = GetComponent<LineRenderer>(); }

            Vector3 start   = attackerFootPos + Vector3.up * _startHeight;
            Vector3 target  = targetTileSurfacePos + Vector3.up * _targetHeight;

            float horizontalDistance = Vector2.Distance( new Vector2( start.x, start.z ), new Vector2( target.x, target.z ) );

            // 対象まで結ぶ山なりの曲線(2次ベジェ曲線)を求め、その途中までを線として配置することで、対象の手前で止める。
            // 制御点は水平方向の中間に置いているため、曲線上の水平方向の進み具合は媒介変数に比例する
            Vector3 control = ( start + target ) * 0.5f + Vector3.up * ( _arcHeight + _arcHeightPerDistance * horizontalDistance );
            float gap       = Mathf.Min( _endGap, horizontalDistance * _maxGapRate );
            float endRate   = ( 0f < horizontalDistance ) ? 1f - gap / horizontalDistance : 1f;

            int pointCount = Mathf.Max( 2, _segmentCount );
            _lineRenderer.positionCount = pointCount;
            for( int i = 0; i < pointCount; ++i )
            {
                float t         = endRate * i / ( pointCount - 1 );
                float invT      = 1f - t;
                Vector3 point   = invT * invT * start + 2f * invT * t * control + t * t * target;

                _lineRenderer.SetPosition( i, point );
            }

            gameObject.SetActive( true );
        }

        /// <summary>
        /// 線を非表示にします
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
