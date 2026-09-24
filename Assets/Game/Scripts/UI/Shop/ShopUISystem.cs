using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// ショップUIシステムです。BattleUi/RecruitUiと同格の、IUiSystem直下のUIシステムとして扱います。
    /// </summary>
    public class ShopUISystem : MonoBehaviour
    {
        [Header( "商品一覧+所持アニマ" )]
        [SerializeField] private ShopView _shopView;

        public ShopView ShopView => _shopView;

        public void Init()
        {
            gameObject.SetActive( true );
            _shopView.Show();
        }

        public void Exit()
        {
            _shopView.Hide();
            gameObject.SetActive( false );
        }

        public void Setup()
        {
            _shopView.Setup();
        }
    }
}
