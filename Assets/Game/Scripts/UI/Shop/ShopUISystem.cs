using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// ショップUIシステムです。View実装は未着手のため、現時点では骨組みのみです。
    /// </summary>
    public class ShopUISystem : MonoBehaviour
    {
        public void Init()
        {
            gameObject.SetActive( true );
        }

        public void Exit()
        {
            gameObject.SetActive( false );
        }

        public void Setup()
        {
        }
    }
}
