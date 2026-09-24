using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 画面全体を「ぼかし+単色の覆い」で覆う汎用オーバーレイ(ScreenBlurOverlayView)の表示を仲介するPresenterです。
    /// 確認画面など、奥の画面の内容から注意を逸らしたい場面から汎用的に呼ばれるため、各StateやHandlerは
    /// IUiSystemに直接アクセスせず、必ずこのPresenter経由で表示・非表示を行ってください。
    /// 各シーンのDIInstallerでバインドされている前提です(DIInstaller.cs / FieldDiInstaller.cs /
    /// RecruitDiInstaller.cs / TitleDiInstaller.cs / ShopDiInstaller.cs)。
    /// </summary>
    public class ScreenBlurOverlayPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        /// <summary>
        /// 既定の見た目(ぼかし+灰色の覆い)で、ヘッダー等の常設UIより奥の全ての表示を覆います。
        /// </summary>
        public void Show()
        {
            var view = View;
            if ( view != null ) { view.Show(); }
        }

        /// <summary>
        /// 見た目を指定して覆います。
        /// </summary>
        /// <param name="blurSize">ぼかしの強さ(0でぼかし無しの覆いのみ)</param>
        /// <param name="coverColor">覆いの色(rgb=色、a=覆いの濃さ)</param>
        public void Show( float blurSize, Color coverColor )
        {
            var view = View;
            if ( view != null ) { view.Show( blurSize, coverColor ); }
        }

        public void Hide()
        {
            var view = View;
            if ( view != null ) { view.Hide(); }
        }

        private ScreenBlurOverlayView View
        {
            get
            {
                var view = _uiSystem.GeneralUi.BlurOverlayView;
                if ( view == null ) { Debug.LogWarning( "[ScreenBlurOverlayPresenter] GeneralUISystem.BlurOverlayViewが未設定です(GeneralUI.prefabのBlurOverlayを確認してください)" ); }

                return view;
            }
        }
    }
}
