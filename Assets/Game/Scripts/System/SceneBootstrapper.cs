using Frontier.UI;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// シーン開始時に必ず呼ばれ、ローディング画面などシーンを跨いで必要な
    /// シングルトンの存在を保証するブートストラップコンポーネント。
    /// 各シーンのルートに1つ配置してください。
    /// </summary>
    [DefaultExecutionOrder( -1000 )]
    public class SceneBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            LoadingScreenController.EnsureInstance();
        }
    }
}
