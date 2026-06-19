using Frontier.Field;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// プレイセッション全体を通じて維持すべきデータを保持するシングルトン。
    /// GameMain.unity に配置し DontDestroyOnLoad で全シーンをまたいで生存する。
    /// 保持するのは「自軍データ」と「フィールド進行状態」のみ。
    /// 戦闘・UI などシーンごとに初期化すべきものはここに置かない。
    /// </summary>
    [DefaultExecutionOrder( -100 )]
    public class GameSession : MonoBehaviour
    {
        private static GameSession _instance = null;

        /// <summary>
        /// インスタンスが存在しない場合は実行時に生成します。
        /// MonoBehaviourのAwake()実行順に依存せず、DIコンテナの初期化など
        /// シーンロード直後の早いタイミングから参照しても必ず存在することを保証します。
        /// </summary>
        public static GameSession Instance
        {
            get
            {
                if ( _instance == null )
                {
                    var go = new GameObject( nameof( GameSession ) );
                    _instance = go.AddComponent<GameSession>();
                    DontDestroyOnLoad( go );
                }
                return _instance;
            }
        }

        public UserDomain    UserDomain    { get; private set; } = new UserDomain();
        public FieldProgress FieldProgress { get; set; }         = null;

        private void Awake()
        {
            if ( _instance == null )
            {
                _instance = this;
                DontDestroyOnLoad( gameObject );
            }
            else if ( _instance != this )
            {
                Destroy( gameObject );
            }
        }

        private void OnDestroy()
        {
            if ( _instance == this ) _instance = null;
        }
    }
}
