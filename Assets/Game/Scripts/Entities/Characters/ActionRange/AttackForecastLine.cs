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
        private Vector3[] _points = new Vector3[0];     // 線を構成する各点のワールド座標(始点=攻撃側、終点=対象タイルの手前)

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
            if( _points.Length != pointCount ) { _points = new Vector3[pointCount]; }
            for( int i = 0; i < pointCount; ++i )
            {
                float t     = endRate * i / ( pointCount - 1 );
                float invT  = 1f - t;

                _points[i] = invT * invT * start + 2f * invT * t * control + t * t * target;
            }
            _lineRenderer.positionCount = pointCount;
            _lineRenderer.SetPositions( _points );

            gameObject.SetActive( true );
        }

        /// <summary>
        /// 線の始点(攻撃側のキャラクター)が画面から見切れている場合に、線のうち画面内に収まっている部分の、始点側の端の位置を取得します。
        /// 「線を辿っていくと画面の外へ出てしまう、その直前の位置」であり、見切れている攻撃側のキャラクターを示すアイコンの表示位置に用います。
        /// </summary>
        /// <param name="camera">判定に用いるカメラ</param>
        /// <param name="screenMargin">画面端から内側へ空ける余白(ピクセル)。取得される位置は、画面端からこの距離以上内側になります</param>
        /// <param name="screenPos">取得した画面上の位置(ピクセル)</param>
        /// <returns>位置を取得出来たか(始点が画面内にある場合、線の終点が画面内に収まっていない場合、線が表示されていない場合はfalse)</returns>
        public bool TryGetScreenEdgePoint( Camera camera, float screenMargin, out Vector2 screenPos )
        {
            screenPos = Vector2.zero;

            if( !gameObject.activeSelf || _points.Length < 2 ) { return false; }

            Rect screenRect = camera.pixelRect;
            Rect insetRect  = new Rect( screenRect.x + screenMargin, screenRect.y + screenMargin, screenRect.width - screenMargin * 2f, screenRect.height - screenMargin * 2f );

            // 始点(攻撃側のキャラクター)が画面内にあれば、見切れていない
            if( IsInsideScreen( camera, _points[0], screenRect, out _ ) ) { return false; }
            // 終点(対象タイル側)が余白の内側に収まっていなければ、表示出来る位置がない
            if( !IsInsideScreen( camera, _points[_points.Length - 1], insetRect, out _ ) ) { return false; }

            // 終点側から始点側へ辿り、最初に余白の外へ出る区間を探す
            for( int i = _points.Length - 2; 0 <= i; --i )
            {
                if( IsInsideScreen( camera, _points[i], insetRect, out _ ) ) { continue; }

                // 区間内で、余白の内側に留まる限界の位置を二分探索で求める
                Vector3 inside  = _points[i + 1];
                Vector3 outside = _points[i];
                for( int step = 0; step < 8; ++step )
                {
                    Vector3 middle = ( inside + outside ) * 0.5f;
                    if( IsInsideScreen( camera, middle, insetRect, out _ ) ) { inside = middle; }
                    else { outside = middle; }
                }

                IsInsideScreen( camera, inside, insetRect, out screenPos );
                return true;
            }

            return false;
        }

        /// <summary>
        /// 指定のワールド座標が、カメラの前方にあり、かつ画面上の指定の範囲内に映っているかを取得します
        /// </summary>
        private static bool IsInsideScreen( Camera camera, in Vector3 worldPos, in Rect rect, out Vector2 screenPos )
        {
            Vector3 pos = camera.WorldToScreenPoint( worldPos );
            screenPos   = pos;

            return 0f < pos.z && rect.Contains( screenPos );
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
