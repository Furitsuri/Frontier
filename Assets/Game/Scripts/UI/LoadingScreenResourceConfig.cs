using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// Resources.Load から LoadingScreenController プレハブを解決するためのポインタ用アセット。
    /// プレハブ本体は Assets/Game/Prefabs 以下に置き、Resources 配下にはこの参照アセットのみを置く。
    /// </summary>
    [CreateAssetMenu( fileName = "LoadingScreenResourceConfig", menuName = "Frontier/UI/LoadingScreenResourceConfig" )]
    public class LoadingScreenResourceConfig : ScriptableObject
    {
        [SerializeField] private LoadingScreenController _prefab = null;

        public LoadingScreenController Prefab => _prefab;
    }
}
