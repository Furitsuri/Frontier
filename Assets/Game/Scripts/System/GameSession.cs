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
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public UserDomain    UserDomain    { get; private set; } = new UserDomain();
        public FieldProgress FieldProgress { get; set; }         = null;

        private void Awake()
        {
            if ( Instance == null )
            {
                Instance = this;
                DontDestroyOnLoad( gameObject );
            }
            else if ( Instance != this )
            {
                Destroy( gameObject );
            }
        }

        private void OnDestroy()
        {
            if ( Instance == this ) Instance = null;
        }
    }
}
