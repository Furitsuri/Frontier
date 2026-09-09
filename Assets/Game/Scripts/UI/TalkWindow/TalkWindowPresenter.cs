using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 汎用トークウィンドウ(TalkWindowUI)の表示を仲介するPresenterです。
    /// 話者名+セリフ+ポートレートの表示は色々な画面のStateから汎用的に呼ばれるため、
    /// 各StateやHandlerはIUiSystemに直接アクセスせず、必ずこのPresenter経由で表示・非表示を行ってください。
    /// 各シーンのDIInstallerでバインドされている前提です(DIInstaller.cs / FieldDiInstaller.cs /
    /// RecruitDiInstaller.cs / TitleDiInstaller.cs 等、IUiSystemが実体のUISystemを指すシーン)。
    /// </summary>
    public class TalkWindowPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private Dictionary<string, TalkEntryData> _talkEntries = null;

        /// <summary>
        /// 現在のシーンのトークデータ(Assets/Resources/TalkData/{シーン名}.json)から
        /// 指定IDの発言をトークウィンドウに表示します。
        /// </summary>
        public void Show( string entryId )
        {
            _talkEntries ??= TalkWindowDataLoader.Load( SceneManager.GetActiveScene().name );

            if( !_talkEntries.TryGetValue( entryId, out var entry ) )
            {
                Debug.LogWarning( $"[TalkWindowPresenter] トークデータが見つかりません: {entryId}" );
                return;
            }

            Sprite portrait = string.IsNullOrEmpty( entry.PortraitKey ) ? null : Resources.Load<Sprite>( $"Sprites/Portraits/{entry.PortraitKey}" );
            _uiSystem.GeneralUi.TalkWindowView.Show( entry.SpeakerName, entry.Message, portrait );
        }

        public void Hide()
        {
            _uiSystem.GeneralUi.TalkWindowView.Hide();
        }
    }
}
