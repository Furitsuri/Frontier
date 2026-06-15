using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// ステージ上に存在するすべてのエンティティ（キャラクター・ステージオブジェクト等）の基底クラスです。
    /// Construct（Zenject による自動呼び出し）→ Init の順で初期化し、不要になったら Dispose を呼んでください。
    /// </summary>
    public abstract class Entity : MonoBehaviour, IDisposer
    {
        protected HierarchyBuilderBase _hierarchyBld    = null;
        protected TransformHandler     _transformHdlr   = null;
        protected int _size                             = Constants.GRID_SIZE_MIN; // グリッドサイズ(Constants.GRID_SIZE_MIN～Constants.GRID_SIZE_MAX)

        private List<int> _occupiedTileIndices = new List<int>();

        protected TransformHandler GetTransformHandler => _transformHdlr;

        /// <summary>
        /// このエンティティが現在占有しているタイルインデックスの一覧（読み取り専用）。
        /// RefreshOccupiedTileIndices を呼ぶまで更新されません。
        /// </summary>
        public IReadOnlyList<int> OccupiedTileIndices => _occupiedTileIndices;

        public int Size => _size;

        public void SetPosition( in Vector3 position )                                          => _transformHdlr.SetPosition( position );
        public void SetPositionXZ( in Vector3 position )                                        => _transformHdlr.SetPositionXZ( position );
        public void SetRotation( in Quaternion rotation )                                       => _transformHdlr.SetRotation( rotation );
        public void SetRotation( Direction direction )                                          => _transformHdlr.SetRotation( direction );
        public void SetVelocityAndAcceleration( in Vector3 velocity, in Vector3 accel )        => _transformHdlr.SetVelocityAndAcceleration( velocity, accel );
        public void ResetVelocityAcceleration()                                                 => _transformHdlr.ResetVelocityAcceleration();
        public void ResetRotationOrder()                                                        => _transformHdlr.ResetRotationOrder();
        public void OrderRotate( in Quaternion rotation )                                       => _transformHdlr.OrderRotate( rotation );
        public void RotateToPosition( in Vector3 targetPos )                                    => _transformHdlr.RotateToPosition( targetPos );
        public void EstablishBaseRotation()                                                     => _transformHdlr.EstablishBaseRotation();
        public void AddPosition( in Vector3 position )                                          => _transformHdlr.AddPosition( position );
        public Vector3 GetPosition()                                                            => _transformHdlr.GetPosition();
        public Vector3 GetPreviousPosition()                                                    => _transformHdlr.GetPreviousPosition();
        public Quaternion GetRotation()                                                         => _transformHdlr.GetRotation();
        public Vector3 GetOrderedForward()                                                      => _transformHdlr.GetOrderedForward();
        public Direction GetDirection()                                                         => _transformHdlr.GetDirection();

        /// <summary>
        /// Zenject のコンストラクタ相当です。MonoBehaviour は Unity が生成するため通常の C# コンストラクタが
        /// 使えないため、[Inject] メソッドで代替しています。Zenject が GameObject 生成後に自動で呼び出します。
        /// サブクラスで追加の注入が必要な場合はオーバーライドし、必ず base.Construct() を呼んでください。
        /// </summary>
        [Inject]
        public virtual void Construct( HierarchyBuilderBase hierarchyBld )
        {
            _hierarchyBld = hierarchyBld;
            LazyInject.GetOrCreate( ref _transformHdlr, () => _hierarchyBld.InstantiateWithDiContainer<TransformHandler>( false ) );
            _transformHdlr.Regist( this.transform );
        }

        protected virtual void Update()
        {
            _transformHdlr?.Update( DeltaTimeProvider.DeltaTime );
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// エンティティのサイズを設定します。値域は Constants.GRID_SIZE_MIN ～ GRID_SIZE_MAX に制限されます。
        /// </summary>
        public void SetSize( int size )
        {
            _size = Mathf.Clamp( size, Constants.GRID_SIZE_MIN, Constants.GRID_SIZE_MAX );

            _transformHdlr.SetScale( ( float ) _size );
        }

        /// <summary>
        /// 占有タイルインデックスリストを再計算します。
        /// _size × _size の矩形フットプリントで baseTileIndex を左上として計算します。
        /// GridCursorController.SetGridCursorSize / GridCursor.SetCursorSize と同じ座標系を想定しています。
        /// </summary>
        /// <param name="baseTileIndex">エンティティの基準タイルインデックス（左上隅）</param>
        /// <param name="columnCount">ステージ1行あたりのタイル数</param>
        public void RefreshOccupiedTileIndices( int baseTileIndex, int columnCount )
        {
            _occupiedTileIndices.Clear();

            for( int row = 0; row < _size; ++row )
            {
                for( int col = 0; col < _size; ++col )
                {
                    _occupiedTileIndices.Add( baseTileIndex + col + row * columnCount );
                }
            }
        }

        /// <summary>
        /// 状態を初期値に戻します。バトル開始など再利用時にも呼ばれます。
        /// </summary>
        public virtual void Init()
        {
            _transformHdlr.Init();
        }

        /// <summary>
        /// 保持リソースを解放し、ゲームオブジェクトを破棄します。
        /// </summary>
        public virtual void Dispose()
        {
            Destroy( gameObject );
            Destroy( this );
        }
    }
}
