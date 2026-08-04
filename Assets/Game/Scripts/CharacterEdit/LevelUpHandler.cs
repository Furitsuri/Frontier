using Frontier.Entities;
using Frontier.UI;
using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// キャラクター編集画面の「レベルアップ」項目から遷移するレベルアップ画面の入力受付・
    /// ライフサイクルを担当するハンドラ。CharacterEditHandlerと同様、特定のシーンに紐づかない
    /// 独立したクラスとしている。割り振りの実データ(LevelUpContext)はここが保持し、
    /// 決定(OK)操作が行われた際にのみCharacter.Status/UserDomain.Expへ反映する
    /// (それまでは全て仮の値であり、キャンセルすれば破棄される)。
    /// </summary>
    public class LevelUpHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain = null;

        private LevelUpPresenter _presenter = null;
        private LevelUpContext _context = null;
        private Action _onClosed = null;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        /// <param name="view">キャラクター編集画面のヒエラルキー内に配置済みのLevelUpUIへの参照</param>
        public void Setup( LevelUpUI view )
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<LevelUpPresenter>( new object[] { view }, false );
        }

        /// <summary>
        /// レベルアップ画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="character">レベルアップ対象のキャラクター</param>
        /// <param name="onClosed">画面を閉じた際(決定・キャンセルいずれも)に呼ばれるコールバック</param>
        public void Show( Character character, Action onClosed )
        {
            _context = new LevelUpContext( character, _userDomain.Exp );
            _onClosed = onClosed;

            _presenter.Show( _context );

            RegisterNavInputCodes();
        }

        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( LevelUpHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                new InputCode( GuideIcon.ALL_CURSOR, "SELECT", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ) { RepeatDelay = DIRECTION_INPUT_REPEAT_DELAY },
                ( GuideIcon.CONFIRM, "OK",   InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ), 0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,  "BACK", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),  0.0f, _navHashCode )
            );
        }

        /// <summary>
        /// 上下でメニューカーソル(MaxHP/Atk/Def/OK)を移動し、左右で選択中の能力値の仮割り振りを
        /// 増減します(OK選択中は左右とも無効)。
        /// </summary>
        private bool AcceptDirection( InputContext context )
        {
            switch ( context.Cursor )
            {
                case Direction.FORWARD:
                case Direction.BACK:
                    return _presenter.MoveSelection( context.Cursor );

                case Direction.RIGHT:
                    return _presenter.Increase();

                case Direction.LEFT:
                    return _presenter.Decrease();

                default:
                    return false;
            }
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;
            if ( !_presenter.IsOkSelected() ) return false;

            Commit();
            Close();

            return true;
        }

        /// <summary>
        /// 仮の割り振り結果をCharacter.Status/UserDomain.Expへ反映します。
        /// MaxHPを上げた分だけCurHPも同時に回復させます(不足分をそのまま維持する形)。
        /// レベルが実際に上がった場合は、Status.Exp(現在のレベル内での取得経験値)を0にリセットします。
        /// </summary>
        private void Commit()
        {
            int addMaxHp = _context.GetAllocated( LevelUpContext.StatKind.MaxHP ) * LevelUpContext.GetGrowthPerPoint( LevelUpContext.StatKind.MaxHP );
            int addAtk   = _context.GetAllocated( LevelUpContext.StatKind.Atk ) * LevelUpContext.GetGrowthPerPoint( LevelUpContext.StatKind.Atk );
            int addDef   = _context.GetAllocated( LevelUpContext.StatKind.Def ) * LevelUpContext.GetGrowthPerPoint( LevelUpContext.StatKind.Def );

            ref var status = ref _context.Character.GetStatusRef;
            status.Level  = _context.TentativeLevel;
            status.MaxHP += addMaxHp;
            status.CurHP += addMaxHp;
            status.Atk   += addAtk;
            status.Def   += addDef;

            // レベルが実際に上がった場合のみ、そのレベル内での取得経験値を0にリセットする
            // (割り振らずにOKを押した場合、既存の取得経験値はそのまま維持する)
            if ( _context.TentativeLevel != _context.OriginalLevel )
            {
                status.Exp = 0;
            }

            _userDomain.AddExp( _context.TentativeExp - _context.OriginalExp );
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        /// <summary>
        /// 画面を閉じます(決定・キャンセルいずれも、仮の割り振り状態を破棄してこのメソッドへ合流します。
        /// 決定の場合はCommit()が先に実データへ反映済みです)。
        /// </summary>
        private void Close()
        {
            _presenter.Hide();
            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();

            var callback = _onClosed;
            _onClosed = null;
            _context = null;

            callback?.Invoke();
        }
    }
}
