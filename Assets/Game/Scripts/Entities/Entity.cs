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
        protected HierarchyBuilderBase _hierarchyBld = null;
        protected TransformHandler     _transformHdlr = null;

        public TransformHandler GetTransformHandler => _transformHdlr;

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
